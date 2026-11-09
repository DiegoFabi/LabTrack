using LabTrack.Data;
using LabTrack.Models;
using LabTrack.Services;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize(Roles = nameof(RolUsuario.Encargado))]
public class UsuariosController : Controller
{
    private readonly LabTrackContext _context;
    private readonly IPasswordHasher<Usuario> _hasher;
    private readonly TimeProvider _reloj;

    public UsuariosController(LabTrackContext context, IPasswordHasher<Usuario> hasher, TimeProvider reloj)
    {
        _context = context;
        _hasher = hasher;
        _reloj = reloj;
    }

    public async Task<IActionResult> Index(string? busqueda, RolUsuario? rol)
    {
        var consulta = _context.Usuarios.AsNoTracking().AsQueryable();

        if (rol.HasValue)
        {
            consulta = consulta.Where(u => u.Rol == rol.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(u => u.Codigo.Contains(texto) || u.NombreCompleto.Contains(texto));
        }

        ViewBag.Busqueda = busqueda;
        ViewBag.Rol = rol;
        ViewBag.Ahora = _reloj.GetLocalNow().DateTime;
        return View(await consulta.OrderBy(u => u.Rol).ThenBy(u => u.NombreCompleto).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .Include(u => u.Reservas).ThenInclude(r => r.Equipo)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
        {
            return NotFound();
        }

        ViewBag.Ahora = _reloj.GetLocalNow().DateTime;
        return View(usuario);
    }

    public IActionResult Create() => View(new UsuarioFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioFormViewModel modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.Password))
        {
            ModelState.AddModelError(nameof(modelo.Password), "La contraseña es obligatoria.");
        }

        await ValidarCodigoUnicoAsync(modelo);
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = new Usuario
        {
            Codigo = modelo.Codigo.Trim().ToUpperInvariant(),
            NombreCompleto = modelo.NombreCompleto.Trim(),
            Email = modelo.Email,
            Rol = modelo.Rol,
            Estado = modelo.Estado,
            FechaRegistro = _reloj.GetLocalNow().DateTime
        };
        usuario.PasswordHash = _hasher.HashPassword(usuario, modelo.Password!);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Usuario registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        return View(new UsuarioFormViewModel
        {
            Id = usuario.Id,
            Codigo = usuario.Codigo,
            NombreCompleto = usuario.NombreCompleto,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Estado = usuario.Estado
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioFormViewModel modelo)
    {
        if (id != modelo.Id)
        {
            return NotFound();
        }

        await ValidarCodigoUnicoAsync(modelo);
        if (id == User.ObtenerId() && modelo.Rol != RolUsuario.Encargado)
        {
            ModelState.AddModelError(nameof(modelo.Rol), "No puede quitarse a sí mismo el rol de Encargado.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Codigo = modelo.Codigo.Trim().ToUpperInvariant();
        usuario.NombreCompleto = modelo.NombreCompleto.Trim();
        usuario.Email = modelo.Email;
        usuario.Rol = modelo.Rol;
        usuario.Estado = modelo.Estado;
        if (!string.IsNullOrWhiteSpace(modelo.Password))
        {
            usuario.PasswordHash = _hasher.HashPassword(usuario, modelo.Password);
        }

        await _context.SaveChangesAsync();
        TempData["Exito"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var usuario = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return usuario is null ? NotFound() : View(usuario);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (id == User.ObtenerId())
        {
            TempData["Error"] = "No puede eliminar su propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        if (await _context.Reservas.AnyAsync(r => r.UsuarioId == id))
        {
            TempData["Error"] = "No se puede eliminar el usuario porque tiene reservas registradas.";
            return RedirectToAction(nameof(Index));
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Usuario eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desbloquear(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.BloqueadoHasta = null;
        usuario.IntentosFallidos = 0;
        await _context.SaveChangesAsync();
        TempData["Exito"] = $"La cuenta de {usuario.NombreCompleto} fue desbloqueada.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarPenalizacion(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Estado = EstadoUsuario.Activo;
        await _context.SaveChangesAsync();
        TempData["Exito"] = $"Se retiró la penalización de {usuario.NombreCompleto}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarCodigoUnicoAsync(UsuarioFormViewModel modelo)
    {
        if (string.IsNullOrWhiteSpace(modelo.Codigo))
        {
            return;
        }

        var codigo = modelo.Codigo.Trim().ToUpperInvariant();
        if (await _context.Usuarios.AnyAsync(u => u.Codigo == codigo && u.Id != modelo.Id))
        {
            ModelState.AddModelError(nameof(modelo.Codigo), "Ya existe un usuario con ese código.");
        }
    }
}
