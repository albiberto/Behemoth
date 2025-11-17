using System.Text.Json;
using Azure.Core.Serialization;
using Behemoth.Contracts.Validators;
using Behemoth.Functions.Middlewares;
using Behemoth.Infrastructure;
using FluentValidation;
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

builder.Services.Configure<WorkerOptions>(options =>
{
    options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileRequestValidator>();

builder.AddCosmosDbContext<BehemothContext>("cosmos", "behemoth-db");
builder.AddAzureBlobServiceClient("blobs");

builder.Build().Run();
