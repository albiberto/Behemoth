using System.Text.Json;
using Behemoth.Web;
using Behemoth.Web.Handlers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

AddHttpClient(builder);

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.Cache.CacheLocation = "localStorage";
    options.ProviderOptions.DefaultAccessTokenScopes.Add("https://graph.microsoft.com/User.Read");
});

builder.Services.AddScoped<AuthTokenHandler>();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
return;

void AddHttpClient(WebAssemblyHostBuilder webAssemblyHostBuilder)
{
    var backendUrl = Environment.GetEnvironmentVariable("BACKEND_URL")
                     ?? builder.Configuration["BackendUrl"]
                     ?? throw new InvalidOperationException("BackendUrl non configurato");

    webAssemblyHostBuilder.Services
        .AddHttpClient<BehemothHttpClient>(client => client.BaseAddress = new(backendUrl))
        .AddHttpMessageHandler<AuthTokenHandler>();

    webAssemblyHostBuilder.Services.AddOptions<JsonSerializerOptions>()
        .Configure(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
        });
}