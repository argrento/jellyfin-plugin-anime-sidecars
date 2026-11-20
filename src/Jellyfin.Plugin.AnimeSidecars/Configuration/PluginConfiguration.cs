using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AnimeSidecars.Configuration;

/// <summary>How sidecar files are placed into the item's internal metadata folder.</summary>
public enum LinkMode
{
    /// <summary>Create a symbolic link to the source file (falls back to copy on failure).</summary>
    Symlink,

    /// <summary>Copy the source file.</summary>
    Copy,
}

/// <summary>Plugin configuration.</summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets a value indicating whether external audio files are linked too.</summary>
    public bool IncludeAudio { get; set; }

    /// <summary>Gets or sets the placement mode.</summary>
    public LinkMode LinkMode { get; set; } = LinkMode.Symlink;

    /// <summary>Gets or sets glob patterns of folder names hidden from the library.</summary>
    public string[] IgnoreFolderPatterns { get; set; } =
        { "Bonus", "*Sub*", "*Sound*", "*Dub*", "NC", "Creditless*", "Menu", "fonts", "Scans" };

    /// <summary>Gets or sets language token lines, e.g. <c>RUS,RU,RUSSIAN=ru</c>.</summary>
    public string[] LanguageTokens { get; set; } =
        { "RUS,RU,RUSSIAN=ru", "ENG,EN,ENGLISH=en", "JPN,JAP,JP,JAPANESE=ja", "UKR,UA=uk" };

    /// <summary>Gets or sets folder names that mark "signs" (forced) subtitles.</summary>
    public string[] SignsWords { get; set; } = { "надписи", "signs", "forced" };

    /// <summary>Gets or sets subtitle group priority; the first match per language becomes default.</summary>
    public string[] SubtitlePriority { get; set; } = Array.Empty<string>();

    /// <summary>Gets or sets a value indicating whether missing episode numbers are filled from names like <c>Show 01 [Tag]</c>.</summary>
    public bool FixEpisodeNumbers { get; set; } = true;

    /// <summary>Gets or sets the library ids to process; empty means all libraries.</summary>
    public Guid[] LibraryIds { get; set; } = Array.Empty<Guid>();
}
