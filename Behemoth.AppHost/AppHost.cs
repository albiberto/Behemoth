using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var blob = AbbBlobStorage("storage");
var cosmos = AddCosmos("cosmos");
var cache = builder.AddRedis("cache");

var functions = builder.AddProject<Projects.Behemoth_Functions>("functions")    
    .WithReference(cosmos)
    .WaitFor(cosmos)
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(blob)
    .WaitFor(blob);

var web = builder.AddProject<Projects.Behemoth_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(functions)
    .WaitFor(cache)
    .WaitFor(functions);

builder.Build().Run();
return;

IResourceBuilder<AzureBlobStorageContainerResource> AbbBlobStorage(string name)
{
    var storage = builder.AddAzureStorage(name);
    if (!builder.ExecutionContext.IsPublishMode) storage.RunAsEmulator();
    
    return storage.AddBlobContainer("behemoth");
}

IResourceBuilder<AzureCosmosDBResource> AddCosmos(string name)
{
    var resourceBuilder = builder.AddAzureCosmosDB(name);
    if (!builder.ExecutionContext.IsPublishMode) resourceBuilder.RunAsEmulator();

    var database = resourceBuilder.AddCosmosDatabase("behemoth");
    database.AddContainer("Players", "/id");
    database.AddContainer("Replays", "/id");
    
    return resourceBuilder;
}
