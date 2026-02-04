using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Aims.Api.Infrastructure.Auth;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? user.FindFirstValue("sub");

        if (Guid.TryParse(raw, out var id))
            return id;

        throw new InvalidOperationException("Missing/invalid user id claim (sub).");
    }

    public static Guid GetOrgId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("orgId");

        if (Guid.TryParse(raw, out var id))
            return id;

        throw new InvalidOperationException("Missing/invalid orgId claim.");
    }
}
