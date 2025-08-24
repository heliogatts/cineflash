using CloudFlash.Core.Entities;

namespace CloudFlash.Core.Interfaces;

public interface ITitleRepository
{
    Task<IEnumerable<Title>> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Title?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Title> CreateAsync(Title title, CancellationToken cancellationToken = default);
    Task<Title> UpdateAsync(Title title, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Title>> GetByGenreAsync(string genre, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IEnumerable<Title>> GetByPlatformAsync(string platform, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}
