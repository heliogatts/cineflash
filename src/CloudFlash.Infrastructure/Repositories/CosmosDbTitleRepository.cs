using CloudFlash.Core.Entities;
using CloudFlash.Core.Interfaces;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace CloudFlash.Infrastructure.Repositories;

public class CosmosDbTitleRepository : ITitleRepository
{
    private readonly Container _container;
    private readonly string _databaseName = "CloudFlashDB";
    private readonly string _containerName = "Titles";

    public CosmosDbTitleRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["CosmosDb:DatabaseName"] ?? _databaseName;
        var containerName = configuration["CosmosDb:ContainerName"] ?? _containerName;
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<IEnumerable<Title>> SearchAsync(string query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c WHERE CONTAINS(UPPER(c.name), UPPER(@query)) OR CONTAINS(UPPER(c.originalName), UPPER(@query)) OR CONTAINS(UPPER(c.overview), UPPER(@query))")
            .WithParameter("@query", query);

        var results = new List<Title>();
        var iterator = _container.GetItemQueryIterator<Title>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = pageSize
            });

        var skipped = 0;
        var targetSkip = (page - 1) * pageSize;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            foreach (var item in response)
            {
                if (skipped < targetSkip)
                {
                    skipped++;
                    continue;
                }

                if (results.Count >= pageSize)
                    break;

                results.Add(item);
            }

            if (results.Count >= pageSize)
                break;
        }

        return results;
    }

    public async Task<Title?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _container.ReadItemAsync<Title>(id, new PartitionKey(id), cancellationToken: cancellationToken);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Title> CreateAsync(Title title, CancellationToken cancellationToken = default)
    {
        title.Id = Guid.NewGuid().ToString();
        title.CreatedAt = DateTime.UtcNow;
        title.UpdatedAt = DateTime.UtcNow;

        var response = await _container.CreateItemAsync(title, new PartitionKey(title.Id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task<Title> UpdateAsync(Title title, CancellationToken cancellationToken = default)
    {
        title.UpdatedAt = DateTime.UtcNow;
        var response = await _container.UpsertItemAsync(title, new PartitionKey(title.Id), cancellationToken: cancellationToken);
        return response.Resource;
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _container.DeleteItemAsync<Title>(id, new PartitionKey(id), cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<Title>> GetByGenreAsync(string genre, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c WHERE ARRAY_CONTAINS(c.genreIds, @genre)")
            .WithParameter("@genre", genre);

        var results = new List<Title>();
        var iterator = _container.GetItemQueryIterator<Title>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = pageSize
            });

        var skipped = 0;
        var targetSkip = (page - 1) * pageSize;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            foreach (var item in response)
            {
                if (skipped < targetSkip)
                {
                    skipped++;
                    continue;
                }

                if (results.Count >= pageSize)
                    break;

                results.Add(item);
            }

            if (results.Count >= pageSize)
                break;
        }

        return results;
    }

    public async Task<IEnumerable<Title>> GetByPlatformAsync(string platform, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var queryDefinition = new QueryDefinition(
            "SELECT * FROM c JOIN sa IN c.streamingAvailabilities WHERE UPPER(sa.platform) = UPPER(@platform)")
            .WithParameter("@platform", platform);

        var results = new List<Title>();
        var iterator = _container.GetItemQueryIterator<Title>(
            queryDefinition,
            requestOptions: new QueryRequestOptions
            {
                MaxItemCount = pageSize
            });

        var skipped = 0;
        var targetSkip = (page - 1) * pageSize;

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(cancellationToken);

            foreach (var item in response)
            {
                if (skipped < targetSkip)
                {
                    skipped++;
                    continue;
                }

                if (results.Count >= pageSize)
                    break;

                results.Add(item);
            }

            if (results.Count >= pageSize)
                break;
        }

        return results;
    }
}
