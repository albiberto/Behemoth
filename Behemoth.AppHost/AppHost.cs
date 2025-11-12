var builder = DistributedApplication.CreateBuilder(args);

var functions = builder.AddProject<Projects.Behemoth_Functions>("functions");

builder.Build().Run();
