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
    /// <returns>True when the directory should be ignored.</returns>
    public static bool ShouldIgnoreDirectory(string folderName, bool parentIsLibraryRoot, bool inSelectedLibrary, bool parentHasVideo, IReadOnlyList<string> patterns)
        => !parentIsLibraryRoot && inSelectedLibrary && parentHasVideo && IsMatch(folderName, patterns);

    /// <summary>Whether a directory directly contains at least one video file.</summary>
    /// <param name="path">Directory.</param>
    /// <returns>True when a video file is present; false when none or unreadable.</returns>
    public static bool DirectoryHasVideo(string path)
    {
        try
        {
            return Directory.EnumerateFiles(path).Any(f => VideoExtensions.Contains(Path.GetExtension(f)));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
