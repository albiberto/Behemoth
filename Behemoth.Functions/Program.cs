using System.Text.Json;
using Azure.Core.Serialization;
using Behemoth.Contracts.Validators;
using Behemoth.Functions.Middlewares;
using Behemoth.Functions.Options;
using Behemoth.Infrastructure;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

if (builder.Environment.IsDevelopment()) builder.UseMiddleware<LocalAuthSimulatorMiddleware>();

builder.AddServiceDefaults();

builder.Services.AddValidatorsFromAssemblyContaining<UpdateProfileRequestValidator>();

builder.AddCosmosDbContext<BehemothContext>("cosmos", "behemoth-db");
builder.AddAzureBlobServiceClient("blobs");
builder.AddKeyedRedisDistributedCache("cache");

builder.Services.AddOptions<CacheOptions>()
    .BindConfiguration("CacheOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();
app.Run();