using System.Security.Claims;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;

namespace Behemoth.Functions.Extensions;

public static class HttpRequestExtensions
{
    private const string ClaimsPrincipalKey = "User";

    private static ClaimsPrincipal? GetClaimsPrincipal(this FunctionContext context) =>
        context.Items.TryGetValue(ClaimsPrincipalKey, out var value) && value is ClaimsPrincipal principal
            ? principal
            : null;

    public static string GetUserId(this HttpRequestData req)
    {
        var principal = req.FunctionContext.GetClaimsPrincipal();

        return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? principal?.FindFirst("sub")?.Value
               ?? principal?.FindFirst("oid")?.Value
               ?? throw new InvalidOperationException("Cannot determine user ID. ClaimsPrincipal was null or did not contain 'sub'/'oid' claims.");
    }

    public static string GetUserEmail(this HttpRequestData req)
    {
        var principal = req.FunctionContext.GetClaimsPrincipal();

        var email = principal?.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal?.FindFirst("preferred_username")?.Value
                    ?? throw new InvalidOperationException("Cannot determine user email. ClaimsPrincipal was null or did not contain 'email'/'preferred_username' claims.");

        return email;
    }
}