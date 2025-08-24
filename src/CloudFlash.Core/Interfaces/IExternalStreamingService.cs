using CloudFlash.Core.Entities;

namespace CloudFlash.Core.Interfaces;

public interface IExternalStreamingService
{
    Task<IEnumerable<Title>> SearchTitlesAsync(string query, CancellationToken cancellationToken = default);
    Task<Title?> GetTitleDetailsAsync(string externalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StreamingAvailability>> GetStreamingAvailabilityAsync(string externalId, string region = "BR", CancellationToken cancellationToken = default);
}
