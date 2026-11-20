using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AnimeSidecars.Jellyfin;

/// <summary>Drops metadata an episode received for a wrong episode number.</summary>
public static class EpisodeMetadataReset
{
    /// <summary>Clears provider-supplied fields so a refresh fills them for the corrected number.</summary>
    /// <param name="episode">The episode whose number was just corrected.</param>
    public static void ClearProviderData(Episode episode)
    {
        episode.Name = string.Empty;
        episode.OriginalTitle = null;
        episode.Overview = null;
        episode.PremiereDate = null;
        episode.ProductionYear = null;
        episode.CommunityRating = null;
        episode.ProviderIds.Clear();
    }

    /// <summary>Picks the name of an episode whose number was corrected.</summary>
    /// <param name="providerNames">Names metadata providers returned for the corrected number.</param>
    /// <param name="currentName">Name after the refresh (e.g. the title embedded in the file).</param>
    /// <param name="fileName">Name parsed from the file name.</param>
    /// <returns>The first provider name, else the current name, else the file name.</returns>
    public static string ChooseName(IEnumerable<string?> providerNames, string? currentName, string fileName)
        => providerNames.FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? (string.IsNullOrWhiteSpace(currentName) ? fileName : currentName);
}
