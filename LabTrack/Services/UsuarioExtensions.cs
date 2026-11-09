using System.Security.Claims;

namespace LabTrack.Services;

public static class UsuarioExtensions
{
    public const string ClaimCodigo = "codigo";

    public static int ObtenerId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static string ObtenerCodigo(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimCodigo) ?? string.Empty;
}
