using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Behemoth.AppHost;

public class InfrastructureInitializer(ILogger<InfrastructureInitializer> logger) : IDistributedApplicationEventingSubscriber
{
    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        // Do nothing in production mode (infrastructure is handled by Bicep/Terraform)
        if (executionContext.IsPublishMode) return Task.CompletedTask;

        eventing.Subscribe<BeforeStartEvent>((@event, ct) =>
        {
            // ⚠️ CRITICAL: Use Task.Run to avoid blocking Docker startup.
            // If we used 'await' directly here, we would block the startup thread waiting for containers that cannot start because the thread is blocked.
            _ = Task.Run(() => InitializeAsync(@event.Model), CancellationToken.None);
            
            return Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    private async Task InitializeAsync(DistributedApplicationModel app)
    {
        logger.LogInformation("🚀 [Infra] Starting background infrastructure initialization...");

        var t1 = ConfigureBlobStorage(app);
        var t2 = ConfigureCosmosDb(app);

        await Task.WhenAll(t1, t2);
    }
    
    private async Task ConfigureBlobStorage(DistributedApplicationModel app)
    {
        var resource = app.Resources.OfType<IResourceWithConnectionString>().FirstOrDefault(r => r.Name == "blobs");
        if (resource is null) return;

        // Retrieve the connection string (might fail initially if resource isn't ready, Polly handles retries later)
        // Note: Pass CancellationToken.None because we are running in a background task
        var connectionString = await resource.GetConnectionStringAsync(CancellationToken.None);
        if (string.IsNullOrEmpty(connectionString)) return;

        await ExecuteWithPollyAsync("Blob Storage", async () =>
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient("behemoth-container");

            await containerClient.CreateIfNotExistsAsync();
            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);
            
            logger.LogInformation("[Infra] Public Blob Container configured.");
        });
    }

    private async Task ConfigureCosmosDb(DistributedApplicationModel appModel)
    {
        var resource = appModel.Resources.OfType<IResourceWithConnectionString>().FirstOrDefault(r => r.Name == "cosmos");
        if (resource is null) return;

        var connectionString = await resource.GetConnectionStringAsync(CancellationToken.None);
        if (string.IsNullOrEmpty(connectionString)) return;

        await ExecuteWithPollyAsync("Cosmos DB", async () =>
        {
            using var cosmosClient = new CosmosClient(connectionString, new CosmosClientOptions
            {
                SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
            });

            var dbResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync("behemoth-db");
            var database = dbResponse.Database;

            await database.CreateContainerIfNotExistsAsync("Players", "/id");
            await database.CreateContainerIfNotExistsAsync("Replays", "/id");

            logger.LogInformation("[Infra] Cosmos DB initialized.");
        });
    }

    private async Task ExecuteWithPollyAsync(string serviceName, Func<Task> action)
    {
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 50,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Linear,
                OnRetry = args =>
                {
                    logger.LogWarning("[Infra] {Service} not ready yet. Retrying in {Delay}s (Attempt {Num}/{Max})", serviceName, args.RetryDelay.TotalSeconds, args.AttemptNumber, 20);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        try 
        {
            await pipeline.ExecuteAsync(static (func, _) => new ValueTask(func()), action, CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Infra] Setup of {Service} failed permanently.", serviceName);
        }
    }
}