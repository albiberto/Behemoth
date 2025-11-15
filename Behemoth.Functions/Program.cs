using Behemoth.Functions.Middlewares;
using Behemoth.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if(builder.Environment.IsDevelopment()) builder.UseMiddleware<LocalAuthSimulatorMiddleware>();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.AddCosmosDbContext<BehemothContext>("cosmos", "behemoth-db");
builder.AddAzureBlobServiceClient("blobs");

builder.Build().Run();
