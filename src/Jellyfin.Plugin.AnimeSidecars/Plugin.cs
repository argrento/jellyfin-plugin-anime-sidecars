using Jellyfin.Plugin.AnimeSidecars.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AnimeSidecars;

/// <summary>Anime Sidecars plugin entry point.</summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>Plugin id.</summary>
    public static readonly Guid PluginId = Guid.Parse("5f2c1d7e-8a3b-4c9e-9b61-2e7d4a0c3f18");

    /// <summary>Initializes a new instance of the <see cref="Plugin"/> class.</summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>Gets the current plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Anime Sidecars";

    /// <inheritdoc />
    public override Guid Id => PluginId;

    /// <inheritdoc />
    public override string Description => "Links subtitles/audio from release subfolders as external tracks and hides service folders.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "AnimeSidecars",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html",
            },
        };
    }
}
