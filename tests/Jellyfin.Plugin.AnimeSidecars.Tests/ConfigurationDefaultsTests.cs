using Jellyfin.Plugin.AnimeSidecars.Configuration;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class ConfigurationDefaultsTests
{
    [Fact]
    public void Defaults_Match_Spec()
    {
        var c = new PluginConfiguration();
        Assert.False(c.IncludeAudio);
        Assert.Equal(LinkMode.Symlink, c.LinkMode);
        Assert.Equal(new[] { "Bonus", "*Sub*", "*Sound*", "*Dub*", "NC", "Creditless*", "Menu", "fonts", "Scans" }, c.IgnoreFolderPatterns);
        Assert.Contains("RUS,RU,RUSSIAN=ru", c.LanguageTokens);
        Assert.Equal(new[] { "надписи", "signs", "forced" }, c.SignsWords);
        Assert.Empty(c.SubtitlePriority);
        Assert.Empty(c.LibraryIds);
    }
}
