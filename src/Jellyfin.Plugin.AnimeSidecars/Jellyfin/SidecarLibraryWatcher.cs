using Jellyfin.Plugin.AnimeSidecars.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>
/// Syncs videos Jellyfin adds outside a full library scan (real-time monitoring picks up new
/// downloads without running post-scan tasks).
/// </summary>
public sealed class SidecarLibraryWatcher : IHostedService, IDisposable
{
    /// <summary>Quiet time after the last added video; a download adds its episodes one by one.</summary>
    private static readonly TimeSpan Delay = TimeSpan.FromMinutes(2);

    private readonly ILibraryManager _libraryManager;
    private readonly DebouncedBatch<Guid> _batch;

    /// <summary>Initializes a new instance of the <see cref="SidecarLibraryWatcher"/> class.</summary>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="service">Sync service.</param>
    /// <param name="logger">Logger.</param>
    public SidecarLibraryWatcher(ILibraryManager libraryManager, SidecarSyncService service, ILogger<SidecarLibraryWatcher> logger)
    {
        _libraryManager = libraryManager;
        _batch = new DebouncedBatch<Guid>(
            Delay,
            ids => service.SyncVideosAsync(ids, CancellationToken.None),
            e => logger.LogError(e, "Anime Sidecars: sync of added videos failed"));
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded += OnItemAdded;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _libraryManager.ItemAdded -= OnItemAdded;
        _batch.Dispose();
    }

    private void OnItemAdded(object? sender, ItemChangeEventArgs e)
    {
        if (e.Item is (Episode or Movie) and Video { IsVirtualItem: false } video && !string.IsNullOrEmpty(video.Path))
        {
            _batch.Add(video.Id);
        }
    }
}
