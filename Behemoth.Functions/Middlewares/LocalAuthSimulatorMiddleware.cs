using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Behemoth.Functions.Middlewares;

public class LocalAuthSimulatorMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var req = await context.GetHttpRequestDataAsync();

        if (req is not null && req.Headers.TryGetValues("Authorization", out var authValues))
        {
            var tokenString = authValues.FirstOrDefault()?.Replace("Bearer ", "").Trim();

            if (!string.IsNullOrEmpty(tokenString))
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(tokenString);
                    
                    var claims = jwtToken.Claims.ToList();
                    
                    var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope");

                    if (scopeClaim != null)
                    {
                        var scopes = scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        claims.AddRange(scopes.Select(scope => new Claim("scp", scope)));
                    }
                    
                    var identity = new ClaimsIdentity(claims, "jwt-local-test");
                    var principal = new ClaimsPrincipal(identity);

                    context.Items.TryAdd("User", principal);
                }
                catch
                {
                    throw new InvalidOperationException("Failed to parse or process JWT token in local auth simulator middleware. Check if token is valid.");
                }
        }

        await next(context);
    }
}