using CloudFlash.Application.Handlers;
using CloudFlash.Application.Mappings;
using CloudFlash.Core.Interfaces;
using CloudFlash.Infrastructure.Repositories;
using CloudFlash.Infrastructure.Services;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Nest;
using Serilog;
using System.Reflection;

namespace CloudFlash.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configurar Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.ApplicationInsights(TelemetryConfiguration.CreateDefault(), TelemetryConverter.Traces)
            .CreateLogger();

        try
        {
            Log.Information("Starting CloudFlash API");

            var builder = WebApplication.CreateBuilder(args);

            // Configurar Serilog
            builder.Host.UseSerilog();

            // Add services to the container.
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            await ConfigureAsync(app);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Controllers
        services.AddControllers();

        // API Versioning
        services.AddApiVersioning(opt =>
        {
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.AssumeDefaultVersionWhenUnspecified = true;
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        // Swagger/OpenAPI
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "CloudFlash API",
                Version = "v1",
                Description = "API para consulta de disponibilidade de filmes e séries em plataformas de streaming",
                Contact = new()
                {
                    Name = "CloudFlash Team",
                    Email = "team@cloudflash.com"
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // Application Insights
        services.AddApplicationInsightsTelemetry();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("DefaultPolicy", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SearchTitlesQueryHandler).Assembly));

        // AutoMapper
        services.AddAutoMapper(typeof(TitleMappingProfile));

        // Azure Cosmos DB
        services.AddSingleton<CosmosClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("CosmosDb")
                ?? throw new InvalidOperationException("CosmosDb connection string is required");

            return new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            });
        });

        // Elasticsearch
        services.AddSingleton<IElasticClient>(provider =>
        {
            var connectionString = configuration.GetConnectionString("Elasticsearch")
                ?? "http://localhost:9200";

            var settings = new ConnectionSettings(new Uri(connectionString))
                .DefaultIndex("titles")
                .PrettyJson()
                .DisableDirectStreaming();

            return new ElasticClient(settings);
        });

        // HTTP Client for external APIs
        services.AddHttpClient<TmdbStreamingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "CloudFlash/1.0");
        });

        // Repository
        services.AddScoped<ITitleRepository, CosmosDbTitleRepository>();

        // Services
        services.AddScoped<ISearchService, ElasticsearchService>();
        services.AddScoped<IExternalStreamingService, TmdbStreamingService>();

        // Health Checks
        services.AddHealthChecks();
        // TODO: Update CosmosDB health check for .NET 9 compatibility
        // The API for AddAzureCosmosDB has changed in version 9.0

        services.AddHealthChecksUI(opt =>
        {
            opt.SetEvaluationTimeInSeconds(15);
            opt.MaximumHistoryEntriesPerEndpoint(60);
            opt.SetApiMaxActiveRequests(1);
            opt.AddHealthCheckEndpoint("default api", "/health");
        }).AddInMemoryStorage();
    }

    private static async Task ConfigureAsync(WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CloudFlash API v1");
                c.RoutePrefix = string.Empty;
            });
        }

        app.UseHttpsRedirection();

        app.UseCors("DefaultPolicy");

        app.UseSerilogRequestLogging();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllers();

        // Health Checks
        app.MapHealthChecks("/health");
        app.MapHealthChecksUI();

        // Initialize database and search index
        await InitializeDatabaseAsync(app.Services);
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        try
        {
            // Initialize Cosmos DB
            var cosmosClient = scope.ServiceProvider.GetRequiredService<CosmosClient>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var databaseName = configuration["CosmosDb:DatabaseName"] ?? "CloudFlashDB";
            var containerName = configuration["CosmosDb:ContainerName"] ?? "Titles";

            var database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            await database.Database.CreateContainerIfNotExistsAsync(containerName, "/id");

            Log.Information("Cosmos DB initialized successfully");

            // Initialize Elasticsearch
            var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

            if (!await searchService.IndexExistsAsync("titles"))
            {
                await searchService.CreateIndexAsync("titles");
                Log.Information("Elasticsearch index created successfully");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize database");
        }
    }
}
