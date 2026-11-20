using System.Collections.Concurrent;
using Jellyfin.Plugin.AnimeSidecars.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>Hides release service folders (subs, sound, bonus…) from library resolution.</summary>
public class SidecarIgnoreRule : IResolverIgnoreRule
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private static readonly ConcurrentDictionary<string, (bool HasVideo, DateTime At)> VideoDirCache = new(StringComparer.Ordinal);
    private readonly ILibraryManager _libraryManager;

    /// <summary>Initializes a new instance of the <see cref="SidecarIgnoreRule"/> class.</summary>
    /// <param name="libraryManager">Library manager.</param>
    public SidecarIgnoreRule(ILibraryManager libraryManager)
    {
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public bool ShouldIgnore(FileSystemMetadata fileInfo, BaseItem? parent)
    {
        var config = Plugin.Instance?.Configuration;
        if (config is null || !fileInfo.IsDirectory)
        {
            return false;
        }

        if (!FolderIgnoreMatcher.IsMatch(fileInfo.Name, config.IgnoreFolderPatterns))
        {
            return false;
        }

        // Same root test as Jellyfin's CoreResolutionIgnoreRule: library locations are top parents.
        var parentIsLibraryRoot = parent is null or AggregateFolder or UserRootFolder or CollectionFolder || parent.IsTopParent;
        if (parentIsLibraryRoot)
        {
            return false;
        }

        var inSelectedLibrary = config.LibraryIds.Length == 0
            || _libraryManager.GetCollectionFolders(parent!).Any(f => config.LibraryIds.Contains(f.Id));
        var parentDir = Path.GetDirectoryName(fileInfo.FullName);
        var parentHasVideo = !string.IsNullOrEmpty(parentDir) && ParentHasVideo(parentDir);
        return FolderIgnoreMatcher.ShouldIgnoreDirectory(fileInfo.Name, parentIsLibraryRoot, inSelectedLibrary, parentHasVideo, config.IgnoreFolderPatterns);
    }

    private static bool ParentHasVideo(string dir)
    {
        var now = DateTime.UtcNow;
        if (VideoDirCache.TryGetValue(dir, out var cached) && now - cached.At < CacheTtl)
        {
            return cached.HasVideo;
        }

        var hasVideo = FolderIgnoreMatcher.DirectoryHasVideo(dir);
        VideoDirCache[dir] = (hasVideo, now);
        return hasVideo;
    }
}
