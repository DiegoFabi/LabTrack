using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize]
public class ReservasController : Controller
{
    private const string RolEstudiante = nameof(RolUsuario.Estudiante);
    private const string RolEncargado = nameof(RolUsuario.Encargado);

    private readonly LabTrackContext _context;
    private readonly IReservaService _reservaService;
    private readonly TimeProvider _reloj;

    public ReservasController(LabTrackContext context, IReservaService reservaService, TimeProvider reloj)
    {
        _context = context;
        _reservaService = reservaService;
        _reloj = reloj;
    }

    // ---------------------------------------------------------------
    // Estudiante (HU-03)
    // ---------------------------------------------------------------

    [Authorize(Roles = RolEstudiante)]
    public async Task<IActionResult> Index()
    {
        var usuarioId = User.ObtenerId();
        var reservas = await _context.Reservas.AsNoTracking()
            .Include(r => r.Equipo).ThenInclude(e => e!.Categoria)
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.FechaSolicitud)
            .ToListAsync();
        return View(reservas);
    }

    [Authorize(Roles = RolEstudiante)]
    public async Task<IActionResult> Create(int? equipoId)
    {
        var ahora = _reloj.GetLocalNow().DateTime;
        var modelo = new ReservaCrearViewModel
        {
            EquipoId = equipoId ?? 0,
            Fecha = ahora.Date
        };
        await CargarEquiposDisponiblesAsync(modelo.EquipoId);
        return View(modelo);
    }

    [HttpPost]
    [Authorize(Roles = RolEstudiante)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReservaCrearViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _reservaService.CrearAsync(User.ObtenerId(), modelo.EquipoId, modelo.Inicio, modelo.Fin);
            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }

        await CargarEquiposDisponiblesAsync(modelo.EquipoId);
        return View(modelo);
    }

    [HttpPost]
    [Authorize(Roles = RolEstudiante)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancelar(int id)
    {
        var resultado = await _reservaService.CancelarAsync(id, User.ObtenerId());
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var reserva = await _context.Reservas.AsNoTracking()
            .Include(r => r.Usuario)
            .Include(r => r.Equipo).ThenInclude(e => e!.Categoria)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reserva is null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RolEncargado) && reserva.UsuarioId != User.ObtenerId())
        {
            return Forbid();
        }

        return View(reserva);
    }

    // ---------------------------------------------------------------
    // Encargado: gestión de solicitudes (HU-04)
    // ---------------------------------------------------------------

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Pendientes()
    {
        var reservas = await _context.Reservas.AsNoTracking()
            .Include(r => r.Usuario)
            .Include(r => r.Equipo)
            .Where(r => r.Estado == EstadoReserva.Pendiente)
            .OrderBy(r => r.Inicio)
            .ToListAsync();
        return View(reservas);
    }

    [HttpPost]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(int id)
    {
        var resultado = await _reservaService.AprobarAsync(id);
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Pendientes));
    }

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Rechazar(int id)
    {
        var reserva = await ObtenerReservaConDetalleAsync(id);
        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.Estado != EstadoReserva.Pendiente)
        {
            TempData["Error"] = "Solo se pueden rechazar reservas pendientes.";
            return RedirectToAction(nameof(Pendientes));
        }

        return View(new RechazoViewModel { ReservaId = id, Reserva = reserva });
    }

    [HttpPost]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(RechazoViewModel modelo)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _reservaService.RechazarAsync(modelo.ReservaId, modelo.Motivo);
            if (resultado.Exito)
            {
                TempData["Exito"] = resultado.Mensaje;
                return RedirectToAction(nameof(Pendientes));
            }

            ModelState.AddModelError(string.Empty, resultado.Mensaje);
        }

        modelo.Reserva = await ObtenerReservaConDetalleAsync(modelo.ReservaId);
        if (modelo.Reserva is null)
        {
            return NotFound();
        }

        return View(modelo);
    }

    // ---------------------------------------------------------------
    // Encargado: devoluciones y penalización (HU-05)
    // ---------------------------------------------------------------

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Activas(string? busqueda)
    {
        var consulta = _context.Reservas.AsNoTracking()
            .Include(r => r.Usuario)
            .Include(r => r.Equipo)
            .Where(r => r.Estado == EstadoReserva.Aprobada);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(r =>
                r.Usuario!.NombreCompleto.Contains(texto) ||
                r.Usuario.Codigo.Contains(texto) ||
                r.Equipo!.Nombre.Contains(texto) ||
                r.Equipo.CodigoInventario.Contains(texto));
        }

        ViewBag.Busqueda = busqueda;
        ViewBag.Ahora = _reloj.GetLocalNow().DateTime;
        return View(await consulta.OrderBy(r => r.Fin).ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarcarDevolucion(int id)
    {
        var resultado = await _reservaService.MarcarDevolucionAsync(id);
        TempData[resultado.Exito ? "Exito" : "Error"] = resultado.Mensaje;
        return RedirectToAction(nameof(Activas));
    }

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Historial(EstadoReserva? estado, string? busqueda)
    {
        var consulta = _context.Reservas.AsNoTracking()
            .Include(r => r.Usuario)
            .Include(r => r.Equipo)
            .AsQueryable();

        if (estado.HasValue)
        {
            consulta = consulta.Where(r => r.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(r =>
                r.Usuario!.NombreCompleto.Contains(texto) ||
                r.Usuario.Codigo.Contains(texto) ||
                r.Equipo!.Nombre.Contains(texto) ||
                r.Equipo.CodigoInventario.Contains(texto));
        }

        ViewBag.Estado = estado;
        ViewBag.Busqueda = busqueda;
        return View(await consulta.OrderByDescending(r => r.FechaSolicitud).ToListAsync());
    }

    private Task<Reserva?> ObtenerReservaConDetalleAsync(int id) =>
        _context.Reservas.AsNoTracking()
            .Include(r => r.Usuario)
            .Include(r => r.Equipo)
            .FirstOrDefaultAsync(r => r.Id == id);

    private async Task CargarEquiposDisponiblesAsync(int seleccionado)
    {
        var equipos = await _context.Equipos.AsNoTracking()
            .Include(e => e.Categoria)
            .Where(e => e.Estado == EstadoEquipo.Disponible)
            .OrderBy(e => e.CodigoInventario)
            .Select(e => new { e.Id, Texto = e.CodigoInventario + " - " + e.Nombre + " (" + e.Categoria!.Nombre + ")" })
            .ToListAsync();
        ViewBag.Equipos = new SelectList(equipos, "Id", "Texto", seleccionado);
    }
}
