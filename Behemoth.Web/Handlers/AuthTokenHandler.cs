using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Behemoth.Web.Handlers;

public class AuthTokenHandler(IAccessTokenProvider tokenProvider, IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var scopes = configuration.GetSection("ApiScopes").Get<string[]>();

        if (scopes is null || scopes.Length == 0) throw new InvalidOperationException("ApiScopes non è configurato in appsettings.json.");
        
        var tokenResult = await tokenProvider.RequestAccessToken(
            new AccessTokenRequestOptions
            {
                Scopes = scopes
            });

        if (tokenResult.TryGetToken(out var token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return await base.SendAsync(request, cancellationToken);
    }
}