using Jellyfin.Plugin.AnimeSidecars.Jellyfin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.AnimeSidecars.Api;

/// <summary>Admin endpoints for preview and manual sync.</summary>
[ApiController]
[Route("AnimeSidecars")]
[Authorize(Policy = "RequiresElevation")]
[Produces("application/json")]
public class AnimeSidecarsController : ControllerBase
{
    private readonly SidecarSyncService _service;

    /// <summary>Initializes a new instance of the <see cref="AnimeSidecarsController"/> class.</summary>
    /// <param name="service">Sync service.</param>
    public AnimeSidecarsController(SidecarSyncService service)
    {
        _service = service;
    }

    /// <summary>Previews planned sidecars of a series.</summary>
    /// <param name="seriesId">Series id.</param>
    /// <returns>Rows.</returns>
    [HttpGet("Preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PreviewRow>> Preview([FromQuery] Guid seriesId)
        => Ok(_service.PreviewSeries(seriesId));

    /// <summary>Synchronizes one series now.</summary>
    /// <param name="seriesId">Series id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Changed item count.</returns>
    [HttpPost("Sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Sync([FromQuery] Guid seriesId, CancellationToken cancellationToken)
        => Ok(new { changed = await _service.SyncSeriesAsync(seriesId, cancellationToken).ConfigureAwait(false) });
}
