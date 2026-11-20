using System.Globalization;
using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.AnimeSidecars.Core;

/// <summary>An episode number to set.</summary>
/// <param name="Number">The episode number.</param>
/// <param name="ReplacesWrongNumber">
/// True when Jellyfin had a number that did not come from the file name. Jellyfin re-parses the path
/// on a "replace all metadata" refresh and would bring the wrong number back, so such a refresh must be avoided.
/// </param>
public sealed record EpisodeNumberFix(int Number, bool ReplacesWrongNumber);

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
        if (trailing.Success)
        {
            return Valid(trailing.Groups[1].Value);
        }

        // "Show 001 TV JAP 1080p": the only bare number in the name is the episode.
        var bare = BareNumberRegex().Matches(stripped);
        return bare.Count == 1 ? Valid(bare[0].Groups[1].Value) : null;
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

    /// <summary>
    /// Checks whether an episode number Jellyfin assigned actually occurs in the file name.
    /// When it does not, Jellyfin took it from a parent folder (e.g. "001-366" in a release
    /// folder name) and it is wrong for every episode in that folder.
    /// </summary>
    /// <param name="fileNameWithoutExtension">File name without extension.</param>
    /// <param name="number">Episode number Jellyfin assigned.</param>
    /// <returns>True when some digit run in the name equals the number.</returns>
    public static bool AppearsInFileName(string fileNameWithoutExtension, int number)
    {
        foreach (Match m in DigitRunRegex().Matches(fileNameWithoutExtension))
        {
            if (int.TryParse(m.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) && n == number)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Decides whether an episode's number should be set or corrected.</summary>
    /// <param name="currentNumber">Episode number Jellyfin assigned, if any.</param>
    /// <param name="fileNameWithoutExtension">File name without extension.</param>
    /// <returns>The number to set, or null to leave the episode as it is.</returns>
    public static EpisodeNumberFix? Plan(int? currentNumber, string fileNameWithoutExtension)
    {
        if (currentNumber is int current && AppearsInFileName(fileNameWithoutExtension, current))
        {
            return null;
        }

        var parsed = TryParse(fileNameWithoutExtension);
        if (parsed is null || parsed == currentNumber)
        {
            return null;
        }

        return new EpisodeNumberFix(parsed.Value, ReplacesWrongNumber: currentNumber.HasValue);
    }

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

    [GeneratedRegex(@"(?<![^\s_])(\d{1,3})(?:v\d)?(?![^\s_])")]
    private static partial Regex BareNumberRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitRunRegex();
}
