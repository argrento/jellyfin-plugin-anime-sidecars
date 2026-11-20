using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class FolderIgnoreMatcherTests
{
    private static readonly string[] P = new PluginConfiguration().IgnoreFolderPatterns;

    [Theory]
    [InlineData("RUS Subs [Crunchyroll]")]
    [InlineData("Rus sound")]
    [InlineData("Rus Dub  [Мега-Аниме]")]
    [InlineData("bonus")]
    [InlineData("Creditless OP-ED")]
    [InlineData("FONTS")]
    [InlineData("RUS Sub")]
    [InlineData("Rus Sub")]
    public void Matches_Service_Folders(string name) => Assert.True(FolderIgnoreMatcher.IsMatch(name, P));

    [Theory]
    [InlineData("Juju Stroll [SP]")]
    [InlineData("Season 01")]
    [InlineData("NCIS")]
    public void Does_Not_Match_Content_Folders(string name) => Assert.False(FolderIgnoreMatcher.IsMatch(name, P));

    [Fact]
    public void Question_Mark_Matches_One_Char()
    {
        Assert.True(FolderIgnoreMatcher.IsMatch("CD1", new[] { "CD?" }));
        Assert.False(FolderIgnoreMatcher.IsMatch("CD12", new[] { "CD?" }));
    }

    [Fact]
    public void Cyrillic_Case_Insensitive()
        => Assert.True(FolderIgnoreMatcher.IsMatch("НАДПИСИ", new[] { "надписи" }));

    [Fact]
    public void Never_At_Library_Root()
        => Assert.False(FolderIgnoreMatcher.ShouldIgnoreDirectory("Sound! Euphonium", parentIsLibraryRoot: true, inSelectedLibrary: true, parentHasVideo: true, P));

    [Fact]
    public void Only_In_Selected_Library()
    {
        Assert.False(FolderIgnoreMatcher.ShouldIgnoreDirectory("Bonus", false, inSelectedLibrary: false, parentHasVideo: true, P));
        Assert.True(FolderIgnoreMatcher.ShouldIgnoreDirectory("Bonus", false, true, parentHasVideo: true, P));
    }

    [Theory]
    [InlineData("Season 2 (English Dub)")]
    [InlineData("Dubstep")]
    [InlineData("Scans")]
    [InlineData("The Sound of Music")]
    public void Only_Next_To_Videos(string name)
        => Assert.False(FolderIgnoreMatcher.ShouldIgnoreDirectory(name, false, true, parentHasVideo: false, P));

    [Fact]
    public void Parent_Has_Video_Detects_Release_Folder()
    {
        using var release = new TempTree("Show 01.mkv", "RUS Subs/Show 01.ass");
        using var plain = new TempTree("Season 1/Show 01.mkv", "cover.jpg");
        Assert.True(FolderIgnoreMatcher.DirectoryHasVideo(release.Root));
        Assert.False(FolderIgnoreMatcher.DirectoryHasVideo(plain.Root));
        Assert.False(FolderIgnoreMatcher.DirectoryHasVideo("/nonexistent/x"));
    }
}
