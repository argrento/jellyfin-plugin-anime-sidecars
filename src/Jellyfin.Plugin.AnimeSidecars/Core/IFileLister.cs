using System.Collections.Concurrent;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Directory listing abstraction.</summary>
public interface IFileLister
{
    /// <summary>Lists sub-directories (full paths); missing directory gives an empty list.</summary>
    /// <param name="path">Directory.</param>
    /// <returns>Directories.</returns>
    IReadOnlyList<string> GetDirectories(string path);

    /// <summary>Lists files (full paths); missing directory gives an empty list.</summary>
    /// <param name="path">Directory.</param>
    /// <returns>Files.</returns>
    IReadOnlyList<string> GetFiles(string path);

    /// <summary>Whether any listing at or below <paramref name="root"/> failed (e.g. mount unavailable).</summary>
    /// <param name="root">Directory.</param>
    /// <returns>True when a listing failed.</returns>
    bool HasFailuresUnder(string root);
}

/// <summary>Physical file system lister caching results for its lifetime (one sync run).</summary>
public sealed class CachingFileLister : IFileLister
{
    private readonly ConcurrentDictionary<string, string[]> _dirs = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string[]> _files = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, bool> _failed = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<string> GetDirectories(string path) => _dirs.GetOrAdd(path, p => Safe(p, () => Directory.GetDirectories(p)));

    /// <inheritdoc />
    public IReadOnlyList<string> GetFiles(string path) => _files.GetOrAdd(path, p => Safe(p, () => Directory.GetFiles(p)));

    /// <inheritdoc />
    public bool HasFailuresUnder(string root)
    {
        var prefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return _failed.Keys.Any(f => f.Equals(root, StringComparison.Ordinal) || f.StartsWith(prefix, StringComparison.Ordinal));
    }

    private string[] Safe(string path, Func<string[]> list)
    {
        try
        {
            var result = list();
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            _failed[path] = true;
            return Array.Empty<string>();
        }
    }
}
