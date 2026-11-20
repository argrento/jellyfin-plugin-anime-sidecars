using System.Text.Json;
using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Result of one synchronization.</summary>
/// <param name="Changed">Whether any file was created or removed.</param>
/// <param name="Created">Files created or replaced.</param>
/// <param name="Removed">Stale owned files removed.</param>
/// <param name="Errors">Non-fatal problems.</param>
public sealed record SyncResult(bool Changed, int Created, int Removed, IReadOnlyList<string> Errors);

/// <summary>Applies a sidecar plan to a metadata folder, owning only files it lists in its manifest.</summary>
public static class SidecarSynchronizer
{
    /// <summary>Manifest file name.</summary>
    public const string ManifestFileName = ".anime-sidecars.json";

    /// <summary>Reads owned file names from the manifest.</summary>
    /// <param name="targetDir">Metadata folder.</param>
    /// <returns>Owned file names; empty when no manifest.</returns>
    public static IReadOnlySet<string> ReadManifest(string targetDir)
    {
        var path = Path.Combine(targetDir, ManifestFileName);
        try
        {
            if (File.Exists(path))
            {
                var manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path));
                return new HashSet<string>(manifest?.Files ?? Array.Empty<string>(), StringComparer.Ordinal);
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
        }

        return new HashSet<string>(StringComparer.Ordinal);
    }

    /// <summary>Makes the folder contain exactly the planned owned files.</summary>
    /// <param name="targetDir">Metadata folder.</param>
    /// <param name="plan">Plan.</param>
    /// <param name="mode">Link mode.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>Result.</returns>
    public static SyncResult Apply(string targetDir, IReadOnlyList<PlannedSidecar> plan, LinkMode mode, ILogger? logger = null)
    {
        var manifestPath = Path.Combine(targetDir, ManifestFileName);
        if (plan.Count == 0 && !File.Exists(manifestPath))
        {
            return new SyncResult(false, 0, 0, Array.Empty<string>());
        }

        Directory.CreateDirectory(targetDir);
        var owned = ReadManifest(targetDir);
        var wanted = new HashSet<string>(plan.Select(p => p.TargetFileName), StringComparer.Ordinal);
        var errors = new List<string>();
        int created = 0, removed = 0;

        foreach (var name in owned.Where(n => !wanted.Contains(n)))
        {
            var path = Path.Combine(targetDir, name);
            if (EntryExists(path))
            {
                File.Delete(path);
                removed++;
            }
        }

        var nowOwned = new List<string>();
        try
        {
            foreach (var p in plan)
            {
                var path = Path.Combine(targetDir, p.TargetFileName);
                try
                {
                    if (EntryExists(path))
                    {
                        if (!owned.Contains(p.TargetFileName))
                        {
                            errors.Add($"Not overwriting foreign file {path}");
                            logger?.LogWarning("Anime Sidecars: not overwriting foreign file {Path}", path);
                            continue;
                        }

                        if (IsUpToDate(path, p.SourcePath, mode))
                        {
                            nowOwned.Add(p.TargetFileName);
                            continue;
                        }

                        File.Delete(path);
                    }

                    Place(path, p.SourcePath, mode, logger);
                    created++;
                    nowOwned.Add(p.TargetFileName);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"{path}: {e.Message}");
                    logger?.LogWarning(e, "Anime Sidecars: failed to place {Path}", path);
                    if (EntryExists(path) && owned.Contains(p.TargetFileName))
                    {
                        nowOwned.Add(p.TargetFileName);
                    }
                }
            }
        }
        finally
        {
            File.WriteAllText(manifestPath, JsonSerializer.Serialize(new Manifest { Files = nowOwned.ToArray() }));
        }

        return new SyncResult(created + removed > 0, created, removed, errors);
    }

    private static bool EntryExists(string path)
    {
        var fi = new FileInfo(path);
        return fi.Exists || fi.LinkTarget is not null;
    }

    private static bool IsUpToDate(string path, string source, LinkMode mode)
    {
        var fi = new FileInfo(path);
        if (fi.LinkTarget is not null)
        {
            return mode == LinkMode.Symlink && string.Equals(fi.LinkTarget, source, StringComparison.Ordinal);
        }

        // A regular file is accepted in both modes (Symlink mode may have fallen back to copying).
        if (!fi.Exists)
        {
            return false;
        }

        var src = new FileInfo(source);
        return src.Exists && src.Length == fi.Length;
    }

    private static void Place(string path, string source, LinkMode mode, ILogger? logger)
    {
        if (mode == LinkMode.Symlink)
        {
            try
            {
                File.CreateSymbolicLink(path, source);
                return;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                logger?.LogWarning(e, "Anime Sidecars: symlink failed for {Path}, copying instead", path);
            }
        }

        File.Copy(source, path, overwrite: false);
    }

    private sealed class Manifest
    {
        public string[] Files { get; set; } = Array.Empty<string>();
    }
}
