using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class LibraryPathsTests
{
    [Fact]
    public void Season_One_Only_In_Series_Root()
    {
        const string Series = "/m/Anime/Jujutsu Kaisen";
        Assert.True(LibraryPaths.IsDirectlyIn("/m/Anime/Jujutsu Kaisen/Jujutsu Kaisen 01 [BDRemux].mkv", Series));
        Assert.True(LibraryPaths.IsDirectlyIn("/m/Anime/Jujutsu Kaisen/Jujutsu Kaisen 01 [BDRemux].mkv", Series + "/"));
        Assert.False(LibraryPaths.IsDirectlyIn("/m/Anime/Jujutsu Kaisen/Juju Stroll [SP]/Juju Stroll 01 [BDRemux].mkv", Series));
        Assert.False(LibraryPaths.IsDirectlyIn("/m/Anime/Jujutsu Kaisen/x.mkv", null));
    }

    [Fact]
    public void Under_Any_Root()
    {
        var roots = new[] { "/m/Anime", "/m/Extra/" };
        Assert.True(LibraryPaths.IsUnderAny("/m/Anime/FMA/ep.mkv", roots));
        Assert.True(LibraryPaths.IsUnderAny("/m/Extra/a/ep.mkv", roots));
        Assert.False(LibraryPaths.IsUnderAny("/m/Anime2/ep.mkv", roots));
        Assert.False(LibraryPaths.IsUnderAny("/m/TV Shows/ep.mkv", roots));
        Assert.False(LibraryPaths.IsUnderAny("/m/Anime/ep.mkv", Array.Empty<string>()));
    }
}
