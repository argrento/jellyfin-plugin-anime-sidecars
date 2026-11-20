using System.Text;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>A sidecar with its final file name in the metadata folder.</summary>
/// <param name="SourcePath">Full source path.</param>
/// <param name="TargetFileName">File name to create in the metadata folder.</param>
/// <param name="Kind">Kind.</param>
/// <param name="Title">Track title (may carry a collision suffix).</param>
/// <param name="Language">Language code.</param>
/// <param name="Forced">Forced flag.</param>
/// <param name="IsDefault">Default flag.</param>
public sealed record PlannedSidecar(string SourcePath, string TargetFileName, SidecarKind Kind, string? Title, string? Language, bool Forced, bool IsDefault);

/// <summary>Builds Jellyfin-friendly names for sidecar candidates.</summary>
public static class SidecarNamer
{
    /// <summary>Plans target names, default flags and collision suffixes.</summary>
    /// <param name="videoBaseName">Video file name without extension.</param>
    /// <param name="candidates">Candidates.</param>
    /// <param name="rules">Rules.</param>
    /// <returns>Planned sidecars in candidate order.</returns>
    public static IReadOnlyList<PlannedSidecar> Plan(string videoBaseName, IReadOnlyList<SidecarCandidate> candidates, SidecarRules rules)
    {
        var defaults = new HashSet<SidecarCandidate>(ReferenceEqualityComparer.Instance);
        foreach (var group in candidates
                     .Where(c => c.Kind == SidecarKind.Subtitle && !c.Forced && c.Title is not null)
                     .GroupBy(c => c.Language ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            SidecarCandidate? best = null;
            var bestIndex = int.MaxValue;
            foreach (var c in group)
            {
                var index = IndexOf(rules.SubtitlePriority, c.Title!);
                if (index < bestIndex)
                {
                    best = c;
                    bestIndex = index;
                }
            }

            if (best is not null)
            {
                defaults.Add(best);
            }
        }

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<PlannedSidecar>(candidates.Count);
        foreach (var c in candidates)
        {
            var isDefault = defaults.Contains(c);
            var title = c.Title;
            var name = BuildName(videoBaseName, title, c, isDefault);
            for (var n = 2; !used.Add(name); n++)
            {
                title = c.Title is null ? n.ToString(System.Globalization.CultureInfo.InvariantCulture) : $"{c.Title} {n}";
                name = BuildName(videoBaseName, title, c, isDefault);
            }

            result.Add(new PlannedSidecar(c.SourcePath, name, c.Kind, title, c.Language, c.Forced, isDefault));
        }

        return result;
    }

    private static int IndexOf(IReadOnlyList<string> priority, string title)
    {
        for (var i = 0; i < priority.Count; i++)
        {
            if (string.Equals(priority[i], title, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }

    private static string BuildName(string baseName, string? title, SidecarCandidate c, bool isDefault)
    {
        var sb = new StringBuilder(baseName);
        if (!string.IsNullOrEmpty(title))
        {
            sb.Append('.').Append(title);
        }

        if (!string.IsNullOrEmpty(c.Language))
        {
            sb.Append('.').Append(c.Language);
        }

        if (isDefault)
        {
            sb.Append(".default");
        }

        if (c.Forced)
        {
            sb.Append(".forced");
        }

        return sb.Append(c.Extension).ToString();
    }
}
