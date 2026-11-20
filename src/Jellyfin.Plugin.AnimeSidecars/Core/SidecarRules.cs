using Jellyfin.Plugin.AnimeSidecars.Configuration;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>Parsed, normalized matching rules.</summary>
/// <param name="SubtitleExtensions">Subtitle extensions, lowercase with leading dot.</param>
/// <param name="AudioExtensions">Audio extensions, lowercase with leading dot.</param>
/// <param name="IncludeAudio">Whether audio sidecars are matched.</param>
/// <param name="LanguageTokens">Uppercase token to language code.</param>
/// <param name="SignsWords">Folder names that mark forced (signs) subtitles.</param>
/// <param name="SubtitlePriority">Subtitle group priority.</param>
/// <param name="MaxDepth">Max folder depth below the video folder.</param>
public sealed record SidecarRules(
    IReadOnlySet<string> SubtitleExtensions,
    IReadOnlySet<string> AudioExtensions,
    bool IncludeAudio,
    IReadOnlyDictionary<string, string> LanguageTokens,
    IReadOnlyList<string> SignsWords,
    IReadOnlyList<string> SubtitlePriority,
    int MaxDepth)
{
    private static readonly string[] SubtitleExts = { ".ass", ".ssa", ".srt", ".sup", ".vtt" };
    private static readonly string[] AudioExts = { ".mka", ".ac3", ".eac3", ".dts", ".flac", ".aac", ".mp3", ".opus" };

    /// <summary>Builds rules from plugin configuration.</summary>
    /// <param name="config">Configuration.</param>
    /// <returns>Rules.</returns>
    public static SidecarRules FromConfiguration(PluginConfiguration config)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in config.LanguageTokens ?? Array.Empty<string>())
        {
            var eq = line?.IndexOf('=') ?? -1;
            if (eq <= 0)
            {
                continue;
            }

            var code = line![(eq + 1)..].Trim();
            if (code.Length == 0)
            {
                continue;
            }

            foreach (var token in line[..eq].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                tokens[token.ToUpperInvariant()] = code;
            }
        }

        return new SidecarRules(
            new HashSet<string>(SubtitleExts, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(AudioExts, StringComparer.OrdinalIgnoreCase),
            config.IncludeAudio,
            tokens,
            Clean(config.SignsWords),
            Clean(config.SubtitlePriority),
            3);
    }

    private static string[] Clean(string[]? values)
        => (values ?? Array.Empty<string>()).Select(v => v?.Trim() ?? string.Empty).Where(v => v.Length > 0).ToArray();
}
