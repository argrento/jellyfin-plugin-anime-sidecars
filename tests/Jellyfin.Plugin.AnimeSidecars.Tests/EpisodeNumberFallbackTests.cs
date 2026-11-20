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
    public void Parses_Number(string name, int expected) => Assert.Equal(expected, EpisodeNumberFallback.TryParse(name));

    [Theory]
    [InlineData("[Yousei-raws] Fullmetal Alchemist Brotherhood (Creditless ED 1) [BDrip 1920x1080 x264 FLAC]")]
    [InlineData("Show 13.5 [BDRemux]")]
    [InlineData("Movie 2019 [BDRip]")]
    [InlineData("Some Show [BDRemux]")]
    [InlineData("Show 0 [x]")]
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

    [Fact]
    public void Enabled_By_Default() => Assert.True(new PluginConfiguration().FixEpisodeNumbers);
}
