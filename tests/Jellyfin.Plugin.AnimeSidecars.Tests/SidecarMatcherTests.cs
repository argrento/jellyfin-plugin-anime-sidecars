using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public class SidecarMatcherTests
{
    private static SidecarRules Rules(bool audio = false)
        => SidecarRules.FromConfiguration(new PluginConfiguration { IncludeAudio = audio });

    private static IReadOnlyList<SidecarCandidate> Find(TempTree t, string video, bool audio = false)
        => SidecarMatcher.FindCandidates(t.Path_(video), Rules(audio), new CachingFileLister());

    [Fact]
    public void Matches_JJK_Layout()
    {
        const string B = "Jujutsu Kaisen 01 [BDRemux]";
        using var t = new TempTree(
            $"{B}.mkv",
            $"RUS Subs/RUS Subs [Crunchyroll]/{B}.ass",
            $"RUS Subs/RUS Subs [Anku & mutagenb]/{B}.ass",
            $"RUS Sound/RUS Sound [JAM CLUB]/{B}.mka",
            "Bonus/NC/Jujutsu Kaisen (NC ED1) [BDRemux].mkv",
            "RUS Subs/RUS Subs [Crunchyroll]/Jujutsu Kaisen 02 [BDRemux].ass");

        var subs = Find(t, $"{B}.mkv");
        Assert.Equal(2, subs.Count);
        Assert.Equal(new SidecarCandidate(t.Path_($"RUS Subs/RUS Subs [Anku & mutagenb]/{B}.ass"), SidecarKind.Subtitle, "Anku & mutagenb", "ru", false, ".ass"), subs[0]);
        Assert.Equal(new SidecarCandidate(t.Path_($"RUS Subs/RUS Subs [Crunchyroll]/{B}.ass"), SidecarKind.Subtitle, "Crunchyroll", "ru", false, ".ass"), subs[1]);

        var all = Find(t, $"{B}.mkv", audio: true);
        Assert.Equal(3, all.Count);
        Assert.Contains(new SidecarCandidate(t.Path_($"RUS Sound/RUS Sound [JAM CLUB]/{B}.mka"), SidecarKind.Audio, "JAM CLUB", "ru", false, ".mka"), all);
    }

    [Fact]
    public void Matches_Fate_Suffixed_Audio()
    {
        const string B = "[Beatrice-Raws] Fate Stay Night - Unlimited Blade Works 01 [BDRip 1920x1080 HEVC TrueHD]";
        using var t = new TempTree($"{B}.mkv", $"RUS Sound/[Gezell]/{B}.Gezell.mka", $"RUS Sound/надписи/{B}.ass");
        var c = Find(t, $"{B}.mkv", audio: true);
        Assert.Contains(new SidecarCandidate(t.Path_($"RUS Sound/[Gezell]/{B}.Gezell.mka"), SidecarKind.Audio, "Gezell", "ru", false, ".mka"), c);
        Assert.Contains(new SidecarCandidate(t.Path_($"RUS Sound/надписи/{B}.ass"), SidecarKind.Subtitle, "надписи", "ru", true, ".ass"), c);
    }

    [Fact]
    public void Matches_DeathNote_Layout()
    {
        const string B = "[SOFCJ-Raws] Death Note - 01 (BDRip 1920x1080 x264 VFR 10bit FLAC)";
        using var t = new TempTree($"{B}.mkv", $"Rus Dub  [Мега-Аниме]/{B}.mka", $"Rus Sub/Надписи/{B}.ass");
        var c = Find(t, $"{B}.mkv", audio: true);
        Assert.Contains(new SidecarCandidate(t.Path_($"Rus Dub  [Мега-Аниме]/{B}.mka"), SidecarKind.Audio, "Мега-Аниме", "ru", false, ".mka"), c);
        Assert.Contains(new SidecarCandidate(t.Path_($"Rus Sub/Надписи/{B}.ass"), SidecarKind.Subtitle, "Надписи", "ru", true, ".ass"), c);
    }

    [Fact]
    public void Matches_FMA_Files_Directly_In_Lang_Folder()
    {
        const string B = "[Yousei-raws] Fullmetal Alchemist Brotherhood 01 [BDrip 1920x1080 x264 FLAC]";
        using var t = new TempTree($"{B}.mkv", $"RUS Subs/{B}.ass");
        var c = Assert.Single(Find(t, $"{B}.mkv"));
        Assert.Null(c.Title);
        Assert.Equal("ru", c.Language);
    }

    [Fact]
    public void Does_Not_Match_Longer_Episode_Number()
    {
        using var t = new TempTree("Show 1.mkv", "RUS Subs [X]/Show 10.ass");
        Assert.Empty(Find(t, "Show 1.mkv"));
    }

    [Fact]
    public void Bracket_Content_Not_Used_For_Language()
    {
        using var t = new TempTree("Ep.mkv", "RUS Subs [EN Team]/Ep.ass");
        var c = Assert.Single(Find(t, "Ep.mkv"));
        Assert.Equal("ru", c.Language);
        Assert.Equal("EN Team", c.Title);
    }

    [Fact]
    public void Skips_Files_In_Video_Folder_And_Too_Deep()
    {
        using var t = new TempTree("Ep.mkv", "Ep.ass", "a/b/c/d/Ep.ass", "a/b/c/Ep.srt");
        var c = Assert.Single(Find(t, "Ep.mkv"));
        Assert.EndsWith("a/b/c/Ep.srt", c.SourcePath);
    }

    [Fact]
    public void Dots_In_Title_Become_Spaces()
    {
        using var t = new TempTree("Ep.mkv", "Subs [Some.Group]/Ep.ass");
        Assert.Equal("Some Group", Assert.Single(Find(t, "Ep.mkv")).Title);
    }

    [Fact]
    public void Missing_Video_Folder_Returns_Empty()
        => Assert.Empty(SidecarMatcher.FindCandidates("/nonexistent/dir/Ep.mkv", Rules(), new CachingFileLister()));

    [Fact]
    public void Lister_Reports_Failures_Under_Root()
    {
        var lister = new CachingFileLister();
        Assert.Empty(lister.GetDirectories("/nonexistent/show"));
        Assert.True(lister.HasFailuresUnder("/nonexistent/show"));
        Assert.True(lister.HasFailuresUnder("/nonexistent"));
        Assert.False(lister.HasFailuresUnder("/other"));

        using var t = new TempTree("Ep.mkv", "Subs/Ep.ass");
        var ok = new CachingFileLister();
        SidecarMatcher.FindCandidates(t.Path_("Ep.mkv"), Rules(), ok);
        Assert.False(ok.HasFailuresUnder(t.Root));
    }
}
