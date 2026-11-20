using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class SidecarRulesTests
{
    [Fact]
    public void Defaults_Parse()
    {
        var r = SidecarRules.FromConfiguration(new PluginConfiguration());
        Assert.Equal("ru", r.LanguageTokens["RUS"]);
        Assert.Equal("ja", r.LanguageTokens["JAPANESE"]);
        Assert.Contains(".ass", r.SubtitleExtensions);
        Assert.Contains(".mka", r.AudioExtensions);
        Assert.Equal(3, r.MaxDepth);
        Assert.False(r.IncludeAudio);
    }

    [Fact]
    public void Malformed_Line_Ignored_And_Values_Trimmed()
    {
        var c = new PluginConfiguration { LanguageTokens = new[] { "garbage", " ger , de = de " } };
        var r = SidecarRules.FromConfiguration(c);
        Assert.Equal("de", r.LanguageTokens["GER"]);
        Assert.Equal("de", r.LanguageTokens["DE"]);
        Assert.Equal(2, r.LanguageTokens.Count);
    }
}
