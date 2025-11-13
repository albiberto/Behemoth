using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;

namespace Behemoth.Functions.Extensions;

public static class HttpRequestExtensions
{
    public static string GetUserEmail(HttpRequestData req)
    {
        var principal = req.FunctionContext.GetClaimsPrincipal();
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("preferred_username")?.Value;
        
        return string.IsNullOrEmpty(email) 
            ? throw new InvalidOperationException("Cannot determine user email from claims.") 
            : email;
    }
}