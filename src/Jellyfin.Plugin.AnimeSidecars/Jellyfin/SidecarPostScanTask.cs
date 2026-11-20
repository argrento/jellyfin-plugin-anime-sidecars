using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>Runs sidecar sync after every library scan.</summary>
public class SidecarPostScanTask : ILibraryPostScanTask
{
    private readonly SidecarSyncService _service;

    /// <summary>Initializes a new instance of the <see cref="SidecarPostScanTask"/> class.</summary>
    /// <param name="service">Sync service.</param>
    public SidecarPostScanTask(SidecarSyncService service)
    {
        _service = service;
    }

    /// <inheritdoc />
    public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        => _service.SyncAllAsync(progress, cancellationToken);
}
