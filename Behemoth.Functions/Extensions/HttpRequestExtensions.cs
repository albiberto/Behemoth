using System.Security.Claims;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    
    namespace Behemoth.Functions.Extensions;
    
    public static class HttpRequestExtensions
    {
        public static string GetUserEmail(this HttpRequestData req)
        {
            var principal = req.FunctionContext.GetClaimsPrincipal();
            return principal?.FindFirst(ClaimTypes.Email)?.Value
                        ?? principal?.FindFirst("preferred_username")?.Value
                        ?? throw new InvalidOperationException("Cannot determine user email from claims.");
        }
    
        public static Guid GetUserId(this HttpRequestData req)
        {
            var principal = req.FunctionContext.GetClaimsPrincipal();
            var sub = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal?.FindFirst("sub")?.Value
                        ?? principal?.FindFirst("oid")?.Value
                        ?? throw new InvalidOperationException("Cannot determine user ID from claims.");
            
            return Guid.Parse(sub);
        }
    
        private static ClaimsPrincipal? GetClaimsPrincipal(this FunctionContext context)
        {
            const string ClaimsPrincipalKey = "User";
    
            if (context.Items.TryGetValue(ClaimsPrincipalKey, out var value) && value is ClaimsPrincipal principal)
                return principal;
    
            return null;
        }
    }