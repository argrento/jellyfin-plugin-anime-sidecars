using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>Preview of one planned sidecar.</summary>
/// <param name="Episode">Item name.</param>
/// <param name="SourcePath">Source path.</param>
/// <param name="TargetFileName">Target file name.</param>
/// <param name="Kind">Subtitle or Audio.</param>
/// <param name="Title">Title.</param>
/// <param name="Language">Language.</param>
/// <param name="Forced">Forced flag.</param>
/// <param name="IsDefault">Default flag.</param>
/// <param name="Status"><c>linked</c> or <c>pending</c>.</param>
public sealed record PreviewRow(string Episode, string SourcePath, string TargetFileName, string Kind, string? Title, string? Language, bool Forced, bool IsDefault, string Status);

/// <summary>Synchronizes sidecars of library videos into their metadata folders.</summary>
public class SidecarSyncService
{
    private readonly ILibraryManager _libraryManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SidecarSyncService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>Initializes a new instance of the <see cref="SidecarSyncService"/> class.</summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="providerManager">Provider manager.</param>
    /// <param name="fileSystem">File system.</param>
    /// <param name="logger">Logger.</param>
    public SidecarSyncService(ILibraryManager libraryManager, IProviderManager providerManager, IFileSystem fileSystem, ILogger<SidecarSyncService> logger)
    {
        _libraryManager = libraryManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    private static PluginConfiguration Config => Plugin.Instance?.Configuration ?? new PluginConfiguration();

    /// <summary>Synchronizes all videos in selected libraries.</summary>
    /// <param name="progress">Progress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of changed items.</returns>
    public Task<int> SyncAllAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => SyncAsync(GetVideos(Config.LibraryIds), progress, cancellationToken);

    /// <summary>Synchronizes one series (or movie folder item).</summary>
    /// <param name="seriesId">Series id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of changed items.</returns>
    public Task<int> SyncSeriesAsync(Guid seriesId, CancellationToken cancellationToken)
        => SyncAsync(GetVideos(new[] { seriesId }), new Progress<double>(), cancellationToken);

    /// <summary>Previews what would be linked for a series.</summary>
    /// <param name="seriesId">Series id.</param>
    /// <returns>Rows.</returns>
    public IReadOnlyList<PreviewRow> PreviewSeries(Guid seriesId)
    {
        var config = Config;
        var rules = SidecarRules.FromConfiguration(config);
        var lister = new CachingFileLister();
        var rows = new List<PreviewRow>();
        foreach (var video in GetVideos(new[] { seriesId }))
        {
            var plan = BuildPlan(video, rules, lister);
            if (plan.Count == 0)
            {
                continue;
            }

            var target = video.GetInternalMetadataPath();
            var owned = SidecarSynchronizer.ReadManifest(target);
            foreach (var p in plan)
            {
                var linked = owned.Contains(p.TargetFileName) && File.Exists(Path.Combine(target, p.TargetFileName));
                rows.Add(new PreviewRow(DisplayName(video), p.SourcePath, p.TargetFileName, p.Kind.ToString(), p.Title, p.Language, p.Forced, p.IsDefault, linked ? "linked" : "pending"));
            }
        }

        return rows;
    }

    private static string DisplayName(Video video)
        => video is Episode e && e.IndexNumber is int n
            ? $"{(e.ParentIndexNumber is int s ? $"S{s:00}" : string.Empty)}E{n:00} {video.Name}"
            : video.Name;

    private static IReadOnlyList<PlannedSidecar> BuildPlan(Video video, SidecarRules rules, IFileLister lister)
    {
        var dir = Path.GetDirectoryName(video.Path);
        if (string.IsNullOrEmpty(dir) || lister.GetDirectories(dir).Count == 0)
        {
            return Array.Empty<PlannedSidecar>();
        }

        var candidates = SidecarMatcher.FindCandidates(video.Path, rules, lister);
        return SidecarNamer.Plan(Path.GetFileNameWithoutExtension(video.Path), candidates, rules);
    }

    private async Task<EpisodeNumberFix?> TryFixEpisodeNumberAsync(Video video, CancellationToken cancellationToken)
    {
        if (video is not Episode episode || episode.IsLocked)
        {
            return null;
        }

        var fix = EpisodeNumberFallback.Plan(episode.IndexNumber, Path.GetFileNameWithoutExtension(episode.Path));
        if (fix is null)
        {
            return null;
        }

        var season = EpisodeNumberFallback.ResolveSeason(
            episode.ParentIndexNumber,
            LibraryPaths.IsDirectlyIn(episode.Path, episode.Series?.Path));
        if (season is null)
        {
            return null;
        }

        var previous = episode.IndexNumber;
        if (fix.ReplacesWrongNumber)
        {
            // Name, overview, dates and ids were fetched for the wrong number.
            EpisodeMetadataReset.ClearProviderData(episode);
        }

        episode.IndexNumber = fix.Number;
        episode.IndexNumberEnd = null;
        episode.ParentIndexNumber = season;

        await episode.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Anime Sidecars: episode number {Number} set for {Item} (was {Previous})",
            fix.Number,
            episode.Path,
            previous);
        return fix;
    }

    /// <summary>
    /// Names an episode whose number was corrected. Without "replace all metadata" a refresh keeps the
    /// title embedded in the file (or leaves the name empty), so the provider's name is fetched here.
    /// </summary>
    private async Task ApplyNameAsync(Episode episode, CancellationToken cancellationToken)
    {
        if (episode.LockedFields.Contains(MetadataField.Name))
        {
            return;
        }

        IEnumerable<RemoteSearchResult> results;
        try
        {
            results = await _providerManager.GetRemoteSearchResults<Episode, EpisodeInfo>(
                new RemoteSearchQuery<EpisodeInfo> { ItemId = episode.Id, SearchInfo = episode.GetLookupInfo() },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _logger.LogWarning(e, "Anime Sidecars: provider lookup failed for {Item}", episode.Path);
            results = Array.Empty<RemoteSearchResult>();
        }

        var name = EpisodeMetadataReset.ChooseName(
            results.Select(r => r.Name),
            episode.Name,
            _libraryManager.ParseName(Path.GetFileNameWithoutExtension(episode.Path)).Name);
        if (!string.Equals(name, episode.Name, StringComparison.Ordinal))
        {
            episode.Name = name;
            await episode.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
        }
    }

    private List<Video> GetVideos(Guid[] rootIds)
    {
        // Scope by folder paths: Jellyfin's AncestorIds can miss older items, so it is not used.
        var roots = rootIds
            .Select(id => _libraryManager.GetItemById(id))
            .SelectMany(item => item switch
            {
                CollectionFolder library => library.PhysicalLocations,
                null => Array.Empty<string>(),
                _ => new[] { item.Path },
            })
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = new[] { BaseItemKind.Episode, BaseItemKind.Movie },
            Recursive = true,
            IsVirtualItem = false,
        };

        return _libraryManager.GetItemList(query)
            .OfType<Video>()
            .Where(v => v.VideoType == VideoType.VideoFile && !string.IsNullOrEmpty(v.Path))
            .Where(v => rootIds.Length == 0 || LibraryPaths.IsUnderAny(v.Path, roots))
            .ToList();
    }

    private async Task<int> SyncAsync(List<Video> videos, IProgress<double> progress, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var config = Config;
            var rules = SidecarRules.FromConfiguration(config);
            var lister = new CachingFileLister();
            var changed = 0;
            for (var i = 0; i < videos.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var video = videos[i];
                try
                {
                    var fix = config.FixEpisodeNumbers
                        ? await TryFixEpisodeNumberAsync(video, cancellationToken).ConfigureAwait(false)
                        : null;
                    var numbered = fix is not null;
                    var corrected = fix is { ReplacesWrongNumber: true };

                    var plan = BuildPlan(video, rules, lister);
                    var dir = Path.GetDirectoryName(video.Path);
                    var sidecarsChanged = false;
                    if (string.IsNullOrEmpty(dir) || lister.HasFailuresUnder(dir))
                    {
                        _logger.LogWarning("Anime Sidecars: cannot list {Dir}, leaving sidecars unchanged", dir);
                    }
                    else
                    {
                        var result = SidecarSynchronizer.Apply(video.GetInternalMetadataPath(), plan, config.LinkMode, _logger);
                        sidecarsChanged = result.Changed;
                        if (sidecarsChanged)
                        {
                            _logger.LogInformation(
                                "Anime Sidecars: {Item} +{Created} -{Removed}",
                                video.Path,
                                result.Created,
                                result.Removed);
                        }
                    }

                    if (numbered || sidecarsChanged)
                    {
                        changed++;

                        // A newly numbered episode only has its file name as metadata: replace it with provider data.
                        // Not when a wrong number was corrected: a "replace all" refresh makes Jellyfin re-parse
                        // the path and bring the wrong number back; providers then only fill empty fields.
                        await video.RefreshMetadata(
                            new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                            {
                                MetadataRefreshMode = MetadataRefreshMode.FullRefresh,
                                ImageRefreshMode = !numbered
                                    ? MetadataRefreshMode.ValidationOnly
                                    : corrected ? MetadataRefreshMode.FullRefresh : MetadataRefreshMode.Default,
                                ReplaceAllMetadata = numbered && !corrected,
                                ReplaceAllImages = corrected,
                            },
                            cancellationToken).ConfigureAwait(false);

                        if (corrected && video is Episode correctedEpisode)
                        {
                            await ApplyNameAsync(correctedEpisode, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogError(e, "Anime Sidecars: failed for {Item}", video.Path);
                }

                progress.Report(100.0 * (i + 1) / Math.Max(1, videos.Count));
            }

            _logger.LogInformation("Anime Sidecars: sync done, {Changed} of {Total} items changed", changed, videos.Count);
            return changed;
        }
        finally
        {
            _lock.Release();
        }
    }
}
