using System.Diagnostics;
using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly LabTrackContext _context;
    private readonly TimeProvider _reloj;

    public HomeController(LabTrackContext context, TimeProvider reloj)
    {
        _context = context;
        _reloj = reloj;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(nameof(RolUsuario.Encargado)))
        {
            return View("PanelEncargado", await ConstruirPanelEncargadoAsync());
        }

        var usuarioId = User.ObtenerId();
        var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var reservas = await _context.Reservas.AsNoTracking()
            .Include(r => r.Equipo).ThenInclude(e => e!.Categoria)
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();

        var modelo = new PanelEstudianteViewModel
        {
            Usuario = usuario,
            ReservasActivas = reservas
                .Where(r => r.Estado is EstadoReserva.Pendiente or EstadoReserva.Aprobada)
                .OrderBy(r => r.Inicio)
                .ToList(),
            UltimasReservas = reservas.Take(5).ToList(),
            EquiposDisponibles = await _context.Equipos.CountAsync(e => e.Estado == EstadoEquipo.Disponible)
        };

        return View("PanelEstudiante", modelo);
    }

    private async Task<PanelEncargadoViewModel> ConstruirPanelEncargadoAsync()
    {
        var ahora = _reloj.GetLocalNow().DateTime;
        var estados = await _context.Equipos.AsNoTracking()
            .GroupBy(e => e.Estado)
            .Select(g => new { Estado = g.Key, Total = g.Count() })
            .ToListAsync();

        int Contar(EstadoEquipo estado) => estados.FirstOrDefault(x => x.Estado == estado)?.Total ?? 0;

        return new PanelEncargadoViewModel
        {
            TotalEquipos = estados.Sum(x => x.Total),
            EquiposDisponibles = Contar(EstadoEquipo.Disponible),
            EquiposPrestados = Contar(EstadoEquipo.Prestado),
            EquiposMantenimiento = Contar(EstadoEquipo.Mantenimiento),
            ReservasPendientes = await _context.Reservas.CountAsync(r => r.Estado == EstadoReserva.Pendiente),
            PrestamosActivos = await _context.Reservas.CountAsync(r => r.Estado == EstadoReserva.Aprobada),
            PrestamosVencidos = await _context.Reservas.CountAsync(r => r.Estado == EstadoReserva.Aprobada && r.Fin < ahora),
            EstudiantesPenalizados = await _context.Usuarios.CountAsync(u => u.Estado == EstadoUsuario.Penalizado),
            UltimasSolicitudes = await _context.Reservas.AsNoTracking()
                .Include(r => r.Usuario)
                .Include(r => r.Equipo)
                .OrderByDescending(r => r.FechaSolicitud)
                .Take(5)
                .ToListAsync()
        };
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
