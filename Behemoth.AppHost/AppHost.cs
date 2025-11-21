using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var blobs = AbbBlobStorage();
var cosmos = AddCosmos();
var cache = AddCache();

var functions = builder.AddProject<Projects.Behemoth_Functions>("functions")
    .WithReference(cosmos)
    // .WaitFor(cosmos)
    .WithReference(cache)
    // .WaitFor(cache)
    .WithReference(blobs);
    // .WaitFor(blobs);

var web = builder.AddProject<Projects.Behemoth_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(functions)
    .WaitFor(cache)
    .WaitFor(functions);

builder.Build().Run();
return;

IResourceBuilder<AzureBlobStorageResource> AbbBlobStorage()
{
    var storage = builder.AddAzureStorage("storage");
    if (!builder.ExecutionContext.IsPublishMode)
        storage.RunAsEmulator(azurite =>
        {
            azurite.WithLifetime(ContainerLifetime.Persistent);
            azurite.WithDataVolume();
        });

    storage.AddBlobContainer("behemoth-container");
    return storage.AddBlobs("blobs");
}

IResourceBuilder<AzureCosmosDBResource> AddCosmos()
{
    var resourceBuilder = builder.AddAzureCosmosDB("cosmos");
    if (!builder.ExecutionContext.IsPublishMode) resourceBuilder.RunAsEmulator(emulator =>
    {
        emulator.WithLifetime(ContainerLifetime.Persistent);
        emulator.WithDataVolume();
    });

    var database = resourceBuilder.AddCosmosDatabase("behemoth-db");
    database.AddContainer("Profiles", "/id");
    database.AddContainer("Replays", "/id");

    return resourceBuilder;
}

IResourceBuilder<AzureRedisCacheResource> AddCache()
{
    var resourceBuilder = builder.AddAzureRedis("cache");

    if (!builder.ExecutionContext.IsPublishMode)
    {
        resourceBuilder.RunAsContainer(redisContainer =>
        {
            redisContainer
                .WithRedisInsight()
                .WithLifetime(ContainerLifetime.Persistent)
                .WithDataVolume(isReadOnly: false)
                .WithPersistence(TimeSpan.FromMinutes(1), 100);
        });
    }

    return resourceBuilder;
}