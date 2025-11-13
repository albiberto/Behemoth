using Behemoth.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.AddServiceDefaults();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.AddCosmosDbContext<BehemothContext>("cosmos", "behemoth");
builder.Services.AddAzureClients(clientBuilder => clientBuilder.AddBlobServiceClient(builder.Configuration.GetConnectionString("blob")));

builder.Build().Run();
