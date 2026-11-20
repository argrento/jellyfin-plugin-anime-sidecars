using Jellyfin.Plugin.AnimeSidecars.Configuration;
using Jellyfin.Plugin.AnimeSidecars.Core;

namespace Jellyfin.Plugin.AnimeSidecars.Tests;

public sealed class SidecarSynchronizerTests : IDisposable
{
    private readonly TempTree _t = new();

    private string Target => _t.Path_("meta");

    private PlannedSidecar Planned(string name, string content = "subs")
        => new(_t.Add("src/" + name, content), name, SidecarKind.Subtitle, "X", "ru", false, false);

    public void Dispose() => _t.Dispose();

    [Fact]
    public void Creates_Symlinks()
    {
        var plan = new[] { Planned("Ep.A.ru.ass"), Planned("Ep.B.ru.ass") };
        var r = SidecarSynchronizer.Apply(Target, plan, LinkMode.Symlink);
        Assert.True(r.Changed);
        Assert.Equal(2, r.Created);
        foreach (var p in plan)
        {
            Assert.Equal(p.SourcePath, new FileInfo(Path.Combine(Target, p.TargetFileName)).LinkTarget);
        }

        Assert.Equal(new[] { "Ep.A.ru.ass", "Ep.B.ru.ass" }, SidecarSynchronizer.ReadManifest(Target).Order());
    }

    [Fact]
    public void Copy_Mode_Copies()
    {
        var p = Planned("Ep.A.ru.ass", "hello");
        SidecarSynchronizer.Apply(Target, new[] { p }, LinkMode.Copy);
        var f = new FileInfo(Path.Combine(Target, p.TargetFileName));
        Assert.Null(f.LinkTarget);
        Assert.Equal("hello", File.ReadAllText(f.FullName));
    }

    [Fact]
    public void Second_Run_Is_Noop()
    {
        var plan = new[] { Planned("Ep.A.ru.ass") };
        SidecarSynchronizer.Apply(Target, plan, LinkMode.Symlink);
        var r = SidecarSynchronizer.Apply(Target, plan, LinkMode.Symlink);
        Assert.False(r.Changed);
        Assert.Equal(0, r.Created);
        Assert.Equal(0, r.Removed);
    }

    [Fact]
    public void Removes_Stale_Owned()
    {
        var a = Planned("Ep.A.ru.ass");
        var b = Planned("Ep.B.ru.ass");
        SidecarSynchronizer.Apply(Target, new[] { a, b }, LinkMode.Symlink);
        var r = SidecarSynchronizer.Apply(Target, new[] { a }, LinkMode.Symlink);
        Assert.Equal(1, r.Removed);
        Assert.True(r.Changed);
        Assert.False(File.Exists(Path.Combine(Target, b.TargetFileName)));
        Assert.Equal(new[] { "Ep.A.ru.ass" }, SidecarSynchronizer.ReadManifest(Target));
    }

    [Fact]
    public void Foreign_File_Untouched()
    {
        var foreign = _t.Add("meta/Ep.rus.srt", "jellyfin");
        SidecarSynchronizer.Apply(Target, new[] { Planned("Ep.A.ru.ass") }, LinkMode.Symlink);
        Assert.Equal("jellyfin", File.ReadAllText(foreign));

        var clash = Planned("Ep.rus.srt", "mine");
        var r = SidecarSynchronizer.Apply(Target, new[] { clash }, LinkMode.Symlink);
        Assert.Equal("jellyfin", File.ReadAllText(foreign));
        Assert.Null(new FileInfo(foreign).LinkTarget);
        Assert.Single(r.Errors);
        Assert.DoesNotContain("Ep.rus.srt", SidecarSynchronizer.ReadManifest(Target));
    }

    [Fact]
    public void Empty_Plan_Does_Not_Create_Dir()
    {
        var r = SidecarSynchronizer.Apply(Target, Array.Empty<PlannedSidecar>(), LinkMode.Symlink);
        Assert.False(r.Changed);
        Assert.False(Directory.Exists(Target));
    }

    [Fact]
    public void Mode_Switch_Replaces()
    {
        var plan = new[] { Planned("Ep.A.ru.ass") };
        SidecarSynchronizer.Apply(Target, plan, LinkMode.Symlink);
        var r = SidecarSynchronizer.Apply(Target, plan, LinkMode.Copy);
        Assert.Equal(1, r.Created);
        Assert.Null(new FileInfo(Path.Combine(Target, "Ep.A.ru.ass")).LinkTarget);
    }

    [Fact]
    public void Source_Change_Relinks()
    {
        SidecarSynchronizer.Apply(Target, new[] { Planned("Ep.A.ru.ass") }, LinkMode.Symlink);
        var moved = new PlannedSidecar(_t.Add("src2/Ep.A.ru.ass"), "Ep.A.ru.ass", SidecarKind.Subtitle, "X", "ru", false, false);
        var r = SidecarSynchronizer.Apply(Target, new[] { moved }, LinkMode.Symlink);
        Assert.True(r.Changed);
        Assert.Equal(moved.SourcePath, new FileInfo(Path.Combine(Target, "Ep.A.ru.ass")).LinkTarget);
    }

    [Fact]
    public void Symlink_Mode_Accepts_Existing_Copy()
    {
        // A copy left by the symlink->copy fallback must not be recreated on every run.
        var plan = new[] { Planned("Ep.A.ru.ass") };
        SidecarSynchronizer.Apply(Target, plan, LinkMode.Copy);
        Assert.False(SidecarSynchronizer.Apply(Target, plan, LinkMode.Symlink).Changed);
    }

    [Fact]
    public void Failure_Midway_Keeps_Manifest_Of_Placed_Files()
    {
        var missing = new PlannedSidecar(_t.Path_("src/gone.ass"), "Ep.A.ru.ass", SidecarKind.Subtitle, "A", "ru", false, false);
        var ok = Planned("Ep.B.ru.ass");
        var r = SidecarSynchronizer.Apply(Target, new[] { missing, ok }, LinkMode.Copy);
        Assert.Single(r.Errors);
        Assert.Equal(new[] { "Ep.B.ru.ass" }, SidecarSynchronizer.ReadManifest(Target));
        Assert.True(File.Exists(Path.Combine(Target, "Ep.B.ru.ass")));
    }
}
