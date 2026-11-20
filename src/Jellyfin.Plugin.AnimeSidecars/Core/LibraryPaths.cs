namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Path helpers for library scoping.</summary>
public static class LibraryPaths
{
    /// <summary>Whether a file sits directly in a folder (not in a sub-folder).</summary>
    /// <param name="filePath">File path.</param>
    /// <param name="folder">Folder path.</param>
    /// <returns>True when the file's directory is the folder.</returns>
    public static bool IsDirectlyIn(string filePath, string? folder)
        => !string.IsNullOrEmpty(folder)
           && string.Equals(Path.GetDirectoryName(filePath), Normalize(folder), StringComparison.Ordinal);

    /// <summary>Whether a path is inside any of the given root folders.</summary>
    /// <param name="path">Path.</param>
    /// <param name="roots">Root folders.</param>
    /// <returns>True when under a root.</returns>
    public static bool IsUnderAny(string path, IEnumerable<string> roots)
        => roots.Any(r => path.StartsWith(Normalize(r) + Path.DirectorySeparatorChar, StringComparison.Ordinal));

    private static string Normalize(string folder) => folder.TrimEnd(Path.DirectorySeparatorChar);
}
