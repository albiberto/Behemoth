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
                    var identity = new ClaimsIdentity(jwtToken.Claims, "jwt-local-test");
                    var principal = new ClaimsPrincipal(identity);

                    context.Items.TryAdd("User", principal);
                }
                catch
                {
                    throw new InvalidOperationException("Failed to parse JWT token in local auth simulator middleware.");
                }
        }

        await next(context);
    }
}