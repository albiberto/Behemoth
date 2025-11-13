var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos");
if (!builder.ExecutionContext.IsPublishMode) cosmos.RunAsEmulator();

var database = cosmos.AddCosmosDatabase("behemoth");
database.AddContainer("Players", "/id");
database.AddContainer("Replays", "/id");

var cache = builder.AddRedis("cache");

var functions = builder.AddProject<Projects.Behemoth_Functions>("functions")    
    .WithReference(cosmos)
    .WithReference(cache)
    .WaitFor(cosmos)
    .WaitFor(cache);

var web = builder.AddProject<Projects.Behemoth_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WithReference(functions)
    .WaitFor(cache)
    .WaitFor(functions);

builder.Build().Run();
