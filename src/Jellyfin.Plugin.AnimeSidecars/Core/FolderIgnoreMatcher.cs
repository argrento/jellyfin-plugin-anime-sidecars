using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Glob matching of folder names for the library ignore rule.</summary>
public static class FolderIgnoreMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> Cache = new(StringComparer.Ordinal);

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".m4v", ".avi", ".m2ts", ".ts", ".webm", ".mov", ".wmv", ".mpg", ".mpeg", ".ogm", ".flv",
    };

    /// <summary>Whether a folder name matches any glob pattern (<c>*</c>, <c>?</c>; whole name; case-insensitive).</summary>
    /// <param name="folderName">Folder name.</param>
    /// <param name="patterns">Patterns.</param>
    /// <returns>True when matched.</returns>
    public static bool IsMatch(string folderName, IReadOnlyList<string> patterns)
    {
        foreach (var raw in patterns)
        {
            var pattern = raw?.Trim();
            if (string.IsNullOrEmpty(pattern))
            {
                continue;
            }

            var regex = Cache.GetOrAdd(pattern, p => new Regex(
                "^" + Regex.Escape(p).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline));
            if (regex.IsMatch(folderName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Full ignore decision for a directory.</summary>
    /// <param name="folderName">Folder name.</param>
    /// <param name="parentIsLibraryRoot">Whether the folder sits directly in a library root.</param>
    /// <param name="inSelectedLibrary">Whether the folder belongs to a selected library.</param>
    /// <param name="parentHasVideo">Whether the parent directory directly contains a video file (a release folder).</param>
    /// <param name="patterns">Patterns.</param>
    /// <param name="sidecarOnly">Whether the folder holds only tracks of the videos next to it (see <see cref="IsSidecarOnlyFolder"/>).</param>
    /// <returns>True when the directory should be ignored.</returns>
    public static bool ShouldIgnoreDirectory(string folderName, bool parentIsLibraryRoot, bool inSelectedLibrary, bool parentHasVideo, IReadOnlyList<string> patterns, bool sidecarOnly = false)
        => !parentIsLibraryRoot && inSelectedLibrary && parentHasVideo && (sidecarOnly || IsMatch(folderName, patterns));

    /// <summary>
    /// Whether a folder holds only tracks of the videos next to it, whatever it is called
    /// (e.g. a subtitle folder named after the translator, <c>RikuSempai/</c>).
    /// </summary>
    /// <param name="path">Folder.</param>
    /// <param name="lister">Directory lister.</param>
    /// <param name="maxDepth">How deep to look; a folder with content deeper than this is kept.</param>
    /// <returns>
    /// True when the parent folder has videos, the folder has no video at any depth, and at least one
    /// file in it is named after one of the parent's videos.
    /// </returns>
    public static bool IsSidecarOnlyFolder(string path, IFileLister lister, int maxDepth = 3)
    {
        var parent = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(parent))
        {
            return false;
        }

        var videos = lister.GetFiles(parent).Where(IsVideo).Select(Path.GetFileNameWithoutExtension).OfType<string>().ToArray();
        if (videos.Length == 0)
        {
            return false;
        }

        var hasSidecar = false;
        return Walk(path, 1) && hasSidecar;

        bool Walk(string dir, int depth)
        {
            foreach (var file in lister.GetFiles(dir))
            {
                if (IsVideo(file))
                {
                    return false;
                }

                if (!hasSidecar)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    hasSidecar = videos.Any(v => SidecarMatcher.NameMatches(name, v));
                }
            }

            foreach (var sub in lister.GetDirectories(dir))
            {
                if (depth >= maxDepth || !Walk(sub, depth + 1))
                {
                    return false;
                }
            }

            return true;
        }
    }

    private static bool IsVideo(string file) => VideoExtensions.Contains(Path.GetExtension(file));

    /// <summary>Whether a directory directly contains at least one video file.</summary>
    /// <param name="path">Directory.</param>
    /// <returns>True when a video file is present; false when none or unreadable.</returns>
    public static bool DirectoryHasVideo(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path).Any(IsVideo);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
