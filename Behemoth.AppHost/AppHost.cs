using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var blobs = AbbBlobStorage("storage");
var cosmos = AddCosmos("cosmos");
var cache = AddRedis("cache");

var functions = builder.AddProject<Projects.Behemoth_Functions>("functions")
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

IResourceBuilder<AzureBlobStorageResource> AbbBlobStorage(string name)
{
    var storage = builder.AddAzureStorage(name);
    if (!builder.ExecutionContext.IsPublishMode)
        storage.RunAsEmulator(azurite =>
        {
            azurite.WithLifetime(ContainerLifetime.Persistent);
            azurite.WithDataVolume();
        });

    storage.AddBlobContainer("behemoth-container");
    return storage.AddBlobs("blobs");
}

IResourceBuilder<AzureCosmosDBResource> AddCosmos(string name)
{
    var resourceBuilder = builder.AddAzureCosmosDB(name);
    if (!builder.ExecutionContext.IsPublishMode) resourceBuilder.RunAsEmulator();

    var database = resourceBuilder.AddCosmosDatabase("behemoth-db");
    database.AddContainer("Players", "/id");
    database.AddContainer("Replays", "/id");

    return resourceBuilder;
}

IResourceBuilder<RedisResource> AddRedis(string name) =>
    builder.AddRedis(name)
        .WithRedisInsight()
        .WithDataVolume(isReadOnly: false)
        .WithPersistence(TimeSpan.FromMinutes(1), 100);