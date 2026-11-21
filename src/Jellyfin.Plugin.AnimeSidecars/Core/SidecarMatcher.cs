using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Kind of sidecar file.</summary>
public enum SidecarKind
{
    /// <summary>Subtitle.</summary>
    Subtitle,

    /// <summary>Audio.</summary>
    Audio,
}

/// <summary>A sidecar file found for a video.</summary>
/// <param name="SourcePath">Full source path.</param>
/// <param name="Kind">Kind.</param>
/// <param name="Title">Track title (group), if any.</param>
/// <param name="Language">Language code, if any.</param>
/// <param name="Forced">Whether this is a signs/forced subtitle.</param>
/// <param name="Extension">Lowercase extension with dot.</param>
public sealed record SidecarCandidate(string SourcePath, SidecarKind Kind, string? Title, string? Language, bool Forced, string Extension);

/// <summary>Finds sidecar files for a video in its sub-folders.</summary>
public static partial class SidecarMatcher
{
    /// <summary>Finds sidecar candidates for a video.</summary>
    /// <param name="videoPath">Full video path.</param>
    /// <param name="rules">Rules.</param>
    /// <param name="lister">Directory lister.</param>
    /// <returns>Candidates ordered by source path (ordinal).</returns>
    public static IReadOnlyList<SidecarCandidate> FindCandidates(string videoPath, SidecarRules rules, IFileLister lister)
    {
        var videoDir = Path.GetDirectoryName(videoPath);
        if (string.IsNullOrEmpty(videoDir))
        {
            return Array.Empty<SidecarCandidate>();
        }

        var baseName = Path.GetFileNameWithoutExtension(videoPath);
        var result = new List<SidecarCandidate>();
        Walk(videoDir, new List<string>(), 1);
        result.Sort((a, b) => string.CompareOrdinal(a.SourcePath, b.SourcePath));
        return result;

        void Walk(string dir, List<string> folders, int depth)
        {
            foreach (var sub in lister.GetDirectories(dir))
            {
                folders.Add(Path.GetFileName(sub));
                foreach (var file in lister.GetFiles(sub))
                {
                    var candidate = TryMatch(file, baseName, folders, rules);
                    if (candidate is not null)
                    {
                        result.Add(candidate);
                    }
                }

                if (depth < rules.MaxDepth)
                {
                    Walk(sub, folders, depth + 1);
                }

                folders.RemoveAt(folders.Count - 1);
            }
        }
    }

    /// <summary>Whether a file name (without extension) belongs to a video: equal to its name, or its name + <c>.something</c>.</summary>
    /// <param name="name">File name without extension.</param>
    /// <param name="videoBaseName">Video file name without extension.</param>
    /// <returns>True when the file is named after the video.</returns>
    public static bool NameMatches(string name, string videoBaseName)
        => name.Equals(videoBaseName, StringComparison.OrdinalIgnoreCase)
            || (name.Length > videoBaseName.Length
                && name.StartsWith(videoBaseName, StringComparison.OrdinalIgnoreCase)
                && name[videoBaseName.Length] == '.');

    private static SidecarCandidate? TryMatch(string file, string baseName, IReadOnlyList<string> folders, SidecarRules rules)
    {
        var ext = Path.GetExtension(file).ToLowerInvariant();
        SidecarKind kind;
        if (rules.SubtitleExtensions.Contains(ext))
        {
            kind = SidecarKind.Subtitle;
        }
        else if (rules.IncludeAudio && rules.AudioExtensions.Contains(ext))
        {
            kind = SidecarKind.Audio;
        }
        else
        {
            return null;
        }

        if (!NameMatches(Path.GetFileNameWithoutExtension(file), baseName))
        {
            return null;
        }

        string? language = null;
        for (var i = folders.Count - 1; i >= 0 && language is null; i--)
        {
            language = FindLanguage(folders[i], rules);
        }

        var forced = folders.Any(f => rules.SignsWords.Any(w => string.Equals(f.Trim(), w, StringComparison.OrdinalIgnoreCase)));
        return new SidecarCandidate(file, kind, FindTitle(folders[^1], rules), language, forced, ext);
    }

    private static string? FindTitle(string folder, SidecarRules rules)
    {
        var brackets = BracketRegex().Matches(folder);
        string? title;
        if (brackets.Count > 0)
        {
            title = brackets[^1].Groups[1].Value;
        }
        else if (FindLanguage(folder, rules) is not null)
        {
            return null;
        }
        else
        {
            title = folder;
        }

        title = WhitespaceRegex().Replace(title.Replace('.', ' '), " ").Trim();
        return title.Length == 0 ? null : title;
    }

    private static string? FindLanguage(string folder, SidecarRules rules)
    {
        var outside = BracketRegex().Replace(folder, " ");
        foreach (Match token in TokenRegex().Matches(outside))
        {
            if (rules.LanguageTokens.TryGetValue(token.Value.ToUpperInvariant(), out var code))
            {
                return code;
            }
        }

        return null;
    }

    [GeneratedRegex(@"\[([^\]]*)\]")]
    private static partial Regex BracketRegex();

    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
