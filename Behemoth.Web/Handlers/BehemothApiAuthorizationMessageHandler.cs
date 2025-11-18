using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Behemoth.Web.Handlers;

public class BehemothApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public BehemothApiAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, IConfiguration config) : base(provider, navigation)
    {
        var serviceUrl = config["BackendUrl"] ?? throw new InvalidOperationException("BackendUrl non; configurato.");
        var scopes = config.GetSection("ApiScopes").Get<string[]>();

        ConfigureHandler(
            [serviceUrl]!, // A quali URL inviare il token?
            scopes // Quali scope chiedere per questi URL?
        );
    }
}