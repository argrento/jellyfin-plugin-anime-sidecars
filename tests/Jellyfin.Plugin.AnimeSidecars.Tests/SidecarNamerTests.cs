using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class SidecarNamerTests
{
    private const string B = "Ep 01";

    private static SidecarRules Rules(params string[] priority)
        => SidecarRules.FromConfiguration(new PluginConfiguration { SubtitlePriority = priority });

    private static SidecarCandidate Sub(string? title, string? lang = "ru", bool forced = false, string ext = ".ass")
        => new($"/src/{title}/{B}{ext}", SidecarKind.Subtitle, title, lang, forced, ext);

    [Fact]
    public void Plan_Builds_Names()
        => Assert.Equal("Ep 01.Crunchyroll.ru.ass", Assert.Single(SidecarNamer.Plan(B, new[] { Sub("Crunchyroll") }, Rules())).TargetFileName);

    [Fact]
    public void Plan_Without_Title_Or_Language()
        => Assert.Equal("Ep 01.srt", Assert.Single(SidecarNamer.Plan(B, new[] { Sub(null, null, ext: ".srt") }, Rules())).TargetFileName);

    [Fact]
    public void Plan_Forced()
        => Assert.Equal("Ep 01.Надписи.ru.forced.ass", Assert.Single(SidecarNamer.Plan(B, new[] { Sub("Надписи", forced: true) }, Rules())).TargetFileName);

    [Fact]
    public void Plan_Default_From_Priority()
    {
        var plan = SidecarNamer.Plan(B, new[] { Sub("Anku & mutagenb"), Sub("Crunchyroll") }, Rules("crunchyroll", "Anku & mutagenb"));
        var cr = plan.Single(p => p.Title == "Crunchyroll");
        var anku = plan.Single(p => p.Title == "Anku & mutagenb");
        Assert.True(cr.IsDefault);
        Assert.Equal("Ep 01.Crunchyroll.ru.default.ass", cr.TargetFileName);
        Assert.False(anku.IsDefault);
        Assert.Equal("Ep 01.Anku & mutagenb.ru.ass", anku.TargetFileName);
    }

    [Fact]
    public void Plan_Default_Per_Language()
    {
        var plan = SidecarNamer.Plan(B, new[] { Sub("A", "ru"), Sub("A", "en") }, Rules("A"));
        Assert.All(plan, p => Assert.True(p.IsDefault));
    }

    [Fact]
    public void Plan_No_Default_Without_Priority_Match()
        => Assert.All(SidecarNamer.Plan(B, new[] { Sub("X"), Sub("Y") }, Rules("Z")), p => Assert.False(p.IsDefault));

    [Fact]
    public void Plan_Default_Never_For_Audio()
    {
        var audio = new SidecarCandidate("/src/a.mka", SidecarKind.Audio, "JAM CLUB", "ru", false, ".mka");
        var p = Assert.Single(SidecarNamer.Plan(B, new[] { audio }, Rules("JAM CLUB")));
        Assert.False(p.IsDefault);
        Assert.Equal("Ep 01.JAM CLUB.ru.mka", p.TargetFileName);
    }

    [Fact]
    public void Plan_Default_Skips_Forced()
        => Assert.False(Assert.Single(SidecarNamer.Plan(B, new[] { Sub("Надписи", forced: true) }, Rules("Надписи"))).IsDefault);

    [Fact]
    public void Plan_Collision()
    {
        var a = Sub("X") with { SourcePath = "/a/Ep 01.ass" };
        var b = Sub("X") with { SourcePath = "/b/Ep 01.ass" };
        var names = SidecarNamer.Plan(B, new[] { a, b }, Rules()).Select(p => p.TargetFileName).ToArray();
        Assert.Equal(new[] { "Ep 01.X.ru.ass", "Ep 01.X 2.ru.ass" }, names);
    }

    [Fact]
    public void Plan_Collision_Without_Title()
    {
        var a = Sub(null) with { SourcePath = "/a/Ep 01.ass" };
        var b = Sub(null) with { SourcePath = "/b/Ep 01.ass" };
        var names = SidecarNamer.Plan(B, new[] { a, b }, Rules()).Select(p => p.TargetFileName).ToArray();
        Assert.Equal(new[] { "Ep 01.ru.ass", "Ep 01.2.ru.ass" }, names);
    }

    [Fact]
    public void Plan_Uses_Folder_Title_Not_File_Suffix()
    {
        var c = new SidecarCandidate("/x/[Gezell]/Ep 01.Gezell.mka", SidecarKind.Audio, "Gezell", "ru", false, ".mka");
        Assert.Equal("Ep 01.Gezell.ru.mka", Assert.Single(SidecarNamer.Plan(B, new[] { c }, Rules())).TargetFileName);
    }
}
