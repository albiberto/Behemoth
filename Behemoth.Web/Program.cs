using Behemoth.Web;
using Behemoth.Web.Handlers;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

    var scopes = builder.Configuration.GetSection("ApiScopes").Get<string[]>();
    foreach (var scope in scopes ?? []) options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
});


builder.Services.AddScoped<BehemothApiAuthorizationMessageHandler>();
builder.Services
    .AddHttpClient<BehemothHttpClient>(client => client.BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? builder.HostEnvironment.BaseAddress))
    .AddHttpMessageHandler<BehemothApiAuthorizationMessageHandler>();

builder.Services.AddMudServices();

await builder.Build().RunAsync();