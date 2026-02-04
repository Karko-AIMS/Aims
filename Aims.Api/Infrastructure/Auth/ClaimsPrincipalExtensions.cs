using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aims.Api.Domain.Entities;

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

    /// <summary>
    /// orgId claim이 없으면 null (InternalAdmin 시나리오)
    /// </summary>
    public static Guid? GetOrgIdOrNull(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("orgId");

        if (Guid.TryParse(raw, out var id))
            return id;

        return null;
    }

    /// <summary>
    /// Org-bound API에서 사용 (Operator/Viewer)
    /// orgId가 없으면 예외
    /// </summary>
    public static Guid GetOrgIdRequired(this ClaimsPrincipal user)
    {
        var id = user.GetOrgIdOrNull();
        if (id.HasValue)
            return id.Value;

        throw new InvalidOperationException("Missing/invalid orgId claim.");
    }

    /// <summary>
    /// 기존 코드 호환용 (VehiclesController 등)
    /// </summary>
    public static Guid GetOrgId(this ClaimsPrincipal user)
        => user.GetOrgIdRequired();

    public static bool IsInternalAdmin(this ClaimsPrincipal user)
        => user.IsInRole(UserRole.InternalAdmin.ToString());
}
