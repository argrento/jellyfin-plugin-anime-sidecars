using Jellyfin.Plugin.AnimeSidecars.Jellyfin;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class EpisodeMetadataResetTests
{
    [Fact]
    public void Clears_Data_Fetched_For_The_Wrong_Number()
    {
        // Jellyfin numbered every file in "Bleach 001-366 TV" as 366 and fetched episode 366's data for all of them.
        var episode = new Episode
        {
            Path = "/anime/Bleach/[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC.mkv",
            IndexNumber = 1,
            ParentIndexNumber = 1,
            Name = "Changing History, Unchanging Heart",
            OriginalTitle = "Original 366",
            Overview = "Overview of episode 366",
            PremiereDate = new DateTime(2012, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            ProductionYear = 2012,
            CommunityRating = 8.1f,
        };
        episode.ProviderIds["Tmdb"] = "366366";

        EpisodeMetadataReset.ClearProviderData(episode);

        Assert.True(string.IsNullOrEmpty(episode.Name));
        Assert.Null(episode.OriginalTitle);
        Assert.Null(episode.Overview);
        Assert.Null(episode.PremiereDate);
        Assert.Null(episode.ProductionYear);
        Assert.Null(episode.CommunityRating);
        Assert.Empty(episode.ProviderIds);

        // What identifies the file and the corrected position stays.
        Assert.Equal("/anime/Bleach/[DBD-Raws] Bleach 001 TV JAP 1080p BDRip HEVC 10bit AAC.mkv", episode.Path);
        Assert.Equal(1, episode.IndexNumber);
        Assert.Equal(1, episode.ParentIndexNumber);
    }
}

public class EpisodeNameChoiceTests
{
    [Fact]
    public void Provider_Name_Wins_Over_Embedded_Title()
        => Assert.Equal(
            "The Day I Became a Shinigami",
            EpisodeMetadataReset.ChooseName(new[] { null, "The Day I Became a Shinigami" }, "[DBD-Raws] Bleach 001 TV JAP 1080p", "Bleach 001 TV JAP"));

    [Fact]
    public void Keeps_Current_Name_When_Providers_Have_None()
        => Assert.Equal(
            "[DBD-Raws] Bleach 001 TV JAP 1080p",
            EpisodeMetadataReset.ChooseName(new[] { "", " " }, "[DBD-Raws] Bleach 001 TV JAP 1080p", "Bleach 001 TV JAP"));

    [Fact]
    public void Falls_Back_To_File_Name_When_Nothing_Else()
        => Assert.Equal("Bleach 001 TV JAP", EpisodeMetadataReset.ChooseName(Array.Empty<string?>(), "", "Bleach 001 TV JAP"));
}
