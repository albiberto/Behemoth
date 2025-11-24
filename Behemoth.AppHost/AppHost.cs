using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var (storage, blobs) = AbbBlobStorage();
var cosmos = AddCosmos();
var cache = AddCache();

var functions = builder.AddAzureFunctionsProject<Projects.Behemoth_Functions>("functions")
    .WithExternalHttpEndpoints()
    .WithHostStorage(storage)
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blobs)
    .WaitFor(blobs);

var web = builder.AddProject<Projects.Behemoth_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(functions)
    .WaitFor(cache)
    .WaitFor(functions);

builder.Build().Run();
return;

IResourceBuilder<AzureCosmosDBResource> AddCosmos()
{
    var resourceBuilder = builder.AddAzureCosmosDB("cosmos");
    if (!builder.ExecutionContext.IsPublishMode)
        resourceBuilder.RunAsEmulator(emulator =>
            emulator
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume());

    var database = resourceBuilder.AddCosmosDatabase("behemoth-db");
    database.AddContainer("Profiles", "/id");
    database.AddContainer("Replays", "/id");

    return resourceBuilder;
}

(IResourceBuilder<AzureStorageResource> storage, IResourceBuilder<AzureBlobStorageResource> blobs) AbbBlobStorage()
{
    var storage = builder.AddAzureStorage("storage");
    if (!builder.ExecutionContext.IsPublishMode)
        storage.RunAsEmulator(azurite =>
            azurite
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume());

    storage.AddBlobContainer("behemoth-container");
    var blobs = storage.AddBlobs("blobs");
    
    return (storage, blobs);
}

IResourceBuilder<AzureRedisCacheResource> AddCache()
{
    var resourceBuilder = builder.AddAzureRedis("cache");

    if (!builder.ExecutionContext.IsPublishMode)
        resourceBuilder.RunAsContainer(redisContainer =>
            redisContainer
                .WithRedisInsight()
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume(isReadOnly: false)
                .WithPersistence(TimeSpan.FromMinutes(1), 100));

    return resourceBuilder;
}