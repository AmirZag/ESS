using System.Security.Claims;

namespace ESS.Api.Helpers.Extentions;

public static class ClaimPrincipalExtensions
{
    public static string? GetIdentityId(this ClaimsPrincipal? principal)
    {
        string? identityId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return identityId;
    }
}
