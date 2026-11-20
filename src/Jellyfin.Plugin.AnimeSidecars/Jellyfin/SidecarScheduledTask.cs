using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>Manual "sync now" task in the dashboard.</summary>
public class SidecarScheduledTask : IScheduledTask
{
    private readonly SidecarSyncService _service;

    /// <summary>Initializes a new instance of the <see cref="SidecarScheduledTask"/> class.</summary>
    /// <param name="service">Sync service.</param>
    public SidecarScheduledTask(SidecarSyncService service)
    {
        _service = service;
    }

    /// <inheritdoc />
    public string Name => "Anime Sidecars: sync";

    /// <inheritdoc />
    public string Key => "AnimeSidecarsSync";

    /// <inheritdoc />
    public string Description => "Links subtitles/audio from release subfolders into item metadata folders.";

    /// <inheritdoc />
    public string Category => "Library";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        => _service.SyncAllAsync(progress, cancellationToken);

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();
}
