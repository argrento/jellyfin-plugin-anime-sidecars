using System.Globalization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>
/// Episode number parsing for names Jellyfin's own parser rejects,
/// e.g. <c>Show 01 [BDRemux]</c> or <c>Show (01 сер.)</c>.
/// </summary>
public static partial class EpisodeNumberFallback
{
    /// <summary>Tries to read an episode number from a file name.</summary>
    /// <param name="fileNameWithoutExtension">File name without extension.</param>
    /// <returns>Episode number 1–999, or null when not confident.</returns>
    public static int? TryParse(string fileNameWithoutExtension)
    {
        var russian = RussianRegex().Match(fileNameWithoutExtension);
        if (russian.Success)
        {
            return Valid(russian.Groups[1].Value);
        }

        var stripped = GroupsRegex().Replace(fileNameWithoutExtension, " ").Trim().TrimEnd(' ', '-', '_', '.');
        var trailing = TrailingNumberRegex().Match(stripped);
        return trailing.Success ? Valid(trailing.Groups[1].Value) : null;
    }

    /// <summary>
    /// Decides which season a newly numbered episode belongs to.
    /// </summary>
    /// <param name="currentSeason">Season number Jellyfin already knows, if any.</param>
    /// <param name="directlyInSeriesFolder">Whether the file sits directly in the series folder.</param>
    /// <returns>
    /// The season number, or null when the episode must stay unnumbered: in a season folder without
    /// a number (e.g. <c>Specials [SP]</c>) metadata providers assume season 1 and would overwrite
    /// the episode with the main season's data.
    /// </returns>
    public static int? ResolveSeason(int? currentSeason, bool directlyInSeriesFolder)
        => currentSeason ?? (directlyInSeriesFolder ? 1 : null);

    private static int? Valid(string digits)
    {
        var n = int.Parse(digits, CultureInfo.InvariantCulture);
        return n is >= 1 and <= 999 ? n : null;
    }

    [GeneratedRegex(@"(?<!\d)(\d{1,3})\s*-?\s*сер", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RussianRegex();

    [GeneratedRegex(@"\[[^\]]*\]|\([^)]*\)")]
    private static partial Regex GroupsRegex();

    [GeneratedRegex(@"(?:^|[\s_\-])(\d{1,3})(?:v\d)?$")]
    private static partial Regex TrailingNumberRegex();
}
