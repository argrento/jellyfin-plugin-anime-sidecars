using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class SidecarOnlyFolderTests
{
    private const string Ep1 = "[ToonsHub] BLEACH Thousand-Year Blood War - S01E01 (JAP 2160p x264 AAC) [Multi-Subs]";
    private const string Ep2 = "[ToonsHub] BLEACH Thousand-Year Blood War - S01E02 (JAP 2160p x264 AAC) [Multi-Subs]";

    private static bool Check(TempTree tree, string folder)
        => FolderIgnoreMatcher.IsSidecarOnlyFolder(tree.Path_(folder), new CachingFileLister());

    [Fact]
    public void Subtitle_Folder_Named_After_Translator_Is_Sidecar_Only()
    {
        // "RikuSempai" matches no pattern, but holds nothing except subtitles of the videos next to it.
        using var tree = new TempTree(
            $"Show/{Ep1}.mkv",
            $"Show/{Ep2}.mkv",
            $"Show/RikuSempai/{Ep1}.ass",
            $"Show/RikuSempai/{Ep2}.ass");
        Assert.True(Check(tree, "Show/RikuSempai"));
    }

    [Fact]
    public void Nested_Track_Folders_Are_Sidecar_Only()
    {
        using var tree = new TempTree(
            "Show/Show 01.mkv",
            "Show/Kansai/Voices/Show 01.ru.mka",
            "Show/Kansai/Voices/readme.txt");
        Assert.True(Check(tree, "Show/Kansai"));
    }

    [Fact]
    public void Folder_With_Videos_Is_Kept()
    {
        // Specials or a season folder: the subtitles belong to the videos inside it.
        using var tree = new TempTree(
            "Show/Show 01.mkv",
            "Show/Juju Stroll/Juju Stroll 01.mkv",
            "Show/Juju Stroll/Show 01.ass");
        Assert.False(Check(tree, "Show/Juju Stroll"));
    }

    [Fact]
    public void Folder_Without_Matching_Files_Is_Kept()
    {
        using var tree = new TempTree(
            "Show/Show 01.mkv",
            "Show/Artbook/cover.jpg",
            "Show/Artbook/Show 02.ass");
        Assert.False(Check(tree, "Show/Artbook"));
    }

    [Fact]
    public void Folder_Next_To_No_Video_Is_Kept()
    {
        using var tree = new TempTree(
            "Show/Season 1/Show 01.mkv",
            "Show/Subs/Show 01.ass");
        Assert.False(Check(tree, "Show/Subs"));
    }

    [Fact]
    public void Empty_Folder_Is_Kept()
    {
        using var tree = new TempTree("Show/Show 01.mkv");
        Directory.CreateDirectory(tree.Path_("Show/Extra"));
        Assert.False(Check(tree, "Show/Extra"));
    }

    [Fact]
    public void Video_Below_Search_Depth_Does_Not_Count_As_Sidecar()
    {
        using var tree = new TempTree(
            "Show/Show 01.mkv",
            "Show/Extras/Show 01.ass",
            "Show/Extras/a/b/c/clip.mkv");
        Assert.False(Check(tree, "Show/Extras"));
    }
}
