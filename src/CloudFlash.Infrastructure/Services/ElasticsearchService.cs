using CloudFlash.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Nest;

namespace CloudFlash.Infrastructure.Services;

public class ElasticsearchService : ISearchService
{
    private readonly IElasticClient _elasticClient;

    public ElasticsearchService(IElasticClient elasticClient)
    {
        _elasticClient = elasticClient;
    }

    public async Task IndexTitleAsync<T>(T document, string index, CancellationToken cancellationToken = default) where T : class
    {
        var response = await _elasticClient.IndexAsync(document, idx => idx.Index(index), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to index document: {response.ServerError?.Error?.Reason}");
        }
    }

    public async Task<IEnumerable<T>> SearchAsync<T>(string query, string index, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) where T : class
    {
        var searchResponse = await _elasticClient.SearchAsync<T>(s => s
            .Index(index)
            .Query(q => q
                .MultiMatch(m => m
                    .Query(query)
                    .Fields(f => f
                        .Field("name^3")
                        .Field("originalName^2")
                        .Field("overview")
                    )
                    .Type(TextQueryType.BestFields)
                    .Fuzziness(Fuzziness.Auto)
                )
            )
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Sort(sort => sort
                .Descending(SortSpecialField.Score)
                .Descending("voteAverage")
            ), cancellationToken);

        if (!searchResponse.IsValid)
        {
            throw new InvalidOperationException($"Search failed: {searchResponse.OriginalException?.Message}");
        }

        return searchResponse.Documents;
    }

    public async Task DeleteFromIndexAsync(string id, string index, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.DeleteAsync<object>(id, d => d.Index(index), cancellationToken);

        if (!response.IsValid && response.Result != Result.NotFound)
        {
            throw new InvalidOperationException($"Failed to delete document: {response.OriginalException?.Message}");
        }
    }

    public async Task<bool> IndexExistsAsync(string index, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.Indices.ExistsAsync(index, ct: cancellationToken);
        return response.Exists;
    }

    public async Task CreateIndexAsync(string index, CancellationToken cancellationToken = default)
    {
        var response = await _elasticClient.Indices.CreateAsync(index, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
                .Analysis(a => a
                    .Analyzers(an => an
                        .Custom("title_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase", "asciifolding", "stop")
                        )
                    )
                )
            )
            .Map<Core.Entities.Title>(m => m
                .Properties(p => p
                    .Text(t => t.Name(n => n.Name).Analyzer("title_analyzer"))
                    .Text(t => t.Name(n => n.OriginalName).Analyzer("title_analyzer"))
                    .Text(t => t.Name(n => n.Overview).Analyzer("standard"))
                    .Keyword(k => k.Name(n => n.Type))
                    .Date(d => d.Name(n => n.ReleaseDate))
                    .Number(n => n.Name(nn => nn.VoteAverage).Type(NumberType.Double))
                    .Number(n => n.Name(nn => nn.VoteCount).Type(NumberType.Integer))
                    .Keyword(k => k.Name(n => n.GenreIds))
                    .Nested<Core.Entities.StreamingAvailability>(ne => ne
                        .Name(n => n.StreamingAvailabilities)
                        .Properties(np => np
                            .Keyword(k => k.Name(nn => nn.Platform))
                            .Keyword(k => k.Name(nn => nn.Region))
                            .Keyword(k => k.Name(nn => nn.Type))
                        )
                    )
                )
            ), cancellationToken);

        if (!response.IsValid)
        {
            throw new InvalidOperationException($"Failed to create index: {response.OriginalException?.Message}");
        }
    }
}
