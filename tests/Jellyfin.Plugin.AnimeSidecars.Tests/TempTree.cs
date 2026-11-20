namespace Jellyfin.Plugin.AnimeSidecars.Tests;

/// <summary>Temporary directory tree with files created from relative paths.</summary>
public sealed class TempTree : IDisposable
{
    public TempTree(params string[] relativeFiles)
    {
        Root = Path.Combine(Path.GetTempPath(), "anime-sidecars-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        foreach (var f in relativeFiles)
        {
            Add(f);
        }
    }

    public string Root { get; }

    public string Path_(string relative) => Path.Combine(Root, relative);

    public string Add(string relative, string content = "x")
    {
        var full = Path_(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
        return full;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch (IOException)
        {
        }
    }
}
