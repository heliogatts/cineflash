namespace CloudFlash.Core.Interfaces;

public interface ISearchService
{
    Task IndexTitleAsync<T>(T document, string index, CancellationToken cancellationToken = default) where T : class;
    Task<IEnumerable<T>> SearchAsync<T>(string query, string index, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) where T : class;
    Task DeleteFromIndexAsync(string id, string index, CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(string index, CancellationToken cancellationToken = default);
    Task CreateIndexAsync(string index, CancellationToken cancellationToken = default);
}
