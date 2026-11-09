using LabTrack.Data;
using LabTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LabTrack.Services;

public enum ResultadoLogin
{
    Exitoso,
    CredencialesInvalidas,
    CuentaBloqueada
}

public record LoginResultado(ResultadoLogin Resultado, Usuario? Usuario = null, int MinutosRestantes = 0);

public interface IAuthService
{
    Task<LoginResultado> ValidarCredencialesAsync(string codigo, string password);
}

public class AuthService : IAuthService
{
    public const string MensajeCredencialesInvalidas = "Credenciales inválidas";

    private readonly LabTrackContext _context;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly TimeProvider _reloj;
    private readonly SeguridadOptions _opciones;

    public AuthService(LabTrackContext context, IPasswordHasher<Usuario> hasher, TimeProvider reloj, IOptions<SeguridadOptions> opciones)
    {
        _context = context;
        _hasher = hasher;
        _reloj = reloj;
        _opciones = opciones.Value;
    }

    public async Task<LoginResultado> ValidarCredencialesAsync(string codigo, string password)
    {
        if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrEmpty(password))
        {
            return new LoginResultado(ResultadoLogin.CredencialesInvalidas);
        }

        var ahora = _reloj.GetLocalNow().DateTime;
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Codigo == codigoNormalizado);

        if (usuario is null)
        {
            return new LoginResultado(ResultadoLogin.CredencialesInvalidas);
        }

        if (usuario.EstaBloqueado(ahora))
        {
            return new LoginResultado(ResultadoLogin.CuentaBloqueada, null, MinutosRestantes(usuario, ahora));
        }

        var verificacion = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= _opciones.MaxIntentosFallidos)
            {
                usuario.IntentosFallidos = 0;
                usuario.BloqueadoHasta = ahora.AddMinutes(_opciones.MinutosBloqueo);
                await _context.SaveChangesAsync();
                return new LoginResultado(ResultadoLogin.CuentaBloqueada, null, _opciones.MinutosBloqueo);
            }

            await _context.SaveChangesAsync();
            return new LoginResultado(ResultadoLogin.CredencialesInvalidas);
        }

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = _hasher.HashPassword(usuario, password);
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _context.SaveChangesAsync();

        return new LoginResultado(ResultadoLogin.Exitoso, usuario);
    }

    private static int MinutosRestantes(Usuario usuario, DateTime ahora) =>
        Math.Max(1, (int)Math.Ceiling((usuario.BloqueadoHasta!.Value - ahora).TotalMinutes));
}
