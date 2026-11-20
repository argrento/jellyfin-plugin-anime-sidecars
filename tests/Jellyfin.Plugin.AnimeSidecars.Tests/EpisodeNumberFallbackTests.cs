using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class EpisodeNumberFallbackTests
{
    [Theory]
    [InlineData("Jujutsu Kaisen 01 [BDRemux]", 1)]
    [InlineData("Jujutsu Kaisen 24 [BDRemux]", 24)]
    [InlineData("Juju Stroll 20 [BDRemux]", 20)]
    [InlineData("[AniPlague] Bleach Sennen Kessen-hen 01 [WEB-DL 1080p AVC AAC]", 1)]
    [InlineData("[Yousei-raws] Fullmetal Alchemist Brotherhood 64 [BDrip 1920x1080 x264 FLAC]", 64)]
    [InlineData("Jujutsu Kaisen 05v2 [BDRemux]", 5)]
    [InlineData("Show 102 (BD 1080p)", 102)]
    [InlineData("Ликвидация (01 сер.) - web-dl1080p", 1)]
    [InlineData("Мастер и Маргарита (10 сер.) - web-dl1080p", 10)]
    [InlineData("01 серия_Апостол_rip by_Aleksan55", 1)]
    [InlineData("[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC", 1)]
    [InlineData("[DBD-Raws] Bleach 167 TV JAP 1080p BDRip HEVC 10bit AAC", 167)]
    public void Parses_Number(string name, int expected) => Assert.Equal(expected, EpisodeNumberFallback.TryParse(name));

    [Theory]
    [InlineData("[Yousei-raws] Fullmetal Alchemist Brotherhood (Creditless ED 1) [BDrip 1920x1080 x264 FLAC]")]
    [InlineData("Show 13.5 [BDRemux]")]
    [InlineData("Movie 2019 [BDRip]")]
    [InlineData("Some Show [BDRemux]")]
    [InlineData("Show 0 [x]")]
    [InlineData("Show 2 Part 3 Extra")] // several bare numbers, none trailing: ambiguous
    public void Returns_Null_When_Unsure(string name) => Assert.Null(EpisodeNumberFallback.TryParse(name));

    [Theory]
    [InlineData(null, true, 1)] // no season folder: season 1, as Jellyfin assumes
    [InlineData(2, false, 2)] // inside "Season 2"
    [InlineData(1, true, 1)]
    public void Season_Resolved_When_Known(int? current, bool inSeriesRoot, int expected)
        => Assert.Equal(expected, EpisodeNumberFallback.ResolveSeason(current, inSeriesRoot));

    [Fact]
    public void Unnumbered_Season_Folder_Is_Not_Numbered()
    {
        // e.g. "Juju Stroll [SP]/": metadata providers treat an unknown season as season 1
        // and would overwrite the specials with the main season's episodes.
        Assert.Null(EpisodeNumberFallback.ResolveSeason(null, directlyInSeriesFolder: false));
    }

    [Theory]
    [InlineData("[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC", 1, true)]
    [InlineData("Show S01E05 1080p", 5, true)]
    [InlineData("Show - 12 [1080p]", 12, true)]
    // Jellyfin's "1-12" rule reads "001-366" in the release folder name as episode 366.
    [InlineData("[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC", 366, false)]
    [InlineData("Show S01E05 1080p", 108, false)]
    public void Detects_Whether_Number_Comes_From_File_Name(string name, int number, bool expected)
        => Assert.Equal(expected, EpisodeNumberFallback.AppearsInFileName(name, number));

    [Fact]
    public void Missing_Number_Is_Filled()
        => Assert.Equal(new EpisodeNumberFix(1, ReplacesWrongNumber: false), EpisodeNumberFallback.Plan(null, "Jujutsu Kaisen 01 [BDRemux]"));

    [Fact]
    public void Number_Taken_From_Folder_Is_Corrected()
        => Assert.Equal(
            new EpisodeNumberFix(1, ReplacesWrongNumber: true),
            EpisodeNumberFallback.Plan(366, "[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC"));

    [Fact]
    public void Number_Taken_From_Folder_Suffix_Is_Corrected()
    {
        // Folder "[Beatrice-Raws] Fate Stay Night - Unlimited Blade Works TV-1": Jellyfin numbers every episode 1.
        Assert.Equal(
            new EpisodeNumberFix(5, ReplacesWrongNumber: true),
            EpisodeNumberFallback.Plan(1, "[Beatrice-Raws] Fate Stay Night - Unlimited Blade Works 05 [BDRip 1920x1080 HEVC TrueHD]"));
    }

    [Theory]
    [InlineData(5, "Show S01E05 1080p")] // Jellyfin's own number is in the name: trust it
    [InlineData(1, "Rick.and.Morty.S08E01.WEBDL.1080p.RGzsRutracker")]
    [InlineData(1, "[Beatrice-Raws] Fate Stay Night - Unlimited Blade Works 00 [BDRip 1920x1080 HEVC TrueHD]")] // episode 0: no valid fix
    [InlineData(12, "Show 12 [BDRemux]")] // already correct
    [InlineData(366, "Some Show [BDRemux]")] // looks wrong, but nothing better to offer
    [InlineData(null, "Some Show [BDRemux]")]
    public void Leaves_Number_Alone(int? current, string name) => Assert.Null(EpisodeNumberFallback.Plan(current, name));

    [Fact]
    public void Enabled_By_Default() => Assert.True(new PluginConfiguration().FixEpisodeNumbers);
}
