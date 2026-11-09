using LabTrack.Data;
using LabTrack.Models;
using LabTrack.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize]
public class EquiposController : Controller
{
    private const string RolEncargado = nameof(RolUsuario.Encargado);
    private readonly LabTrackContext _context;

    public EquiposController(LabTrackContext context)
    {
        _context = context;
    }

    // HU-02: Catálogo y filtrado de equipos (accesible para estudiantes y encargados).
    public async Task<IActionResult> Index(int? categoriaId, string? busqueda)
    {
        var consulta = _context.Equipos.AsNoTracking().Include(e => e.Categoria).AsQueryable();

        if (categoriaId.HasValue)
        {
            consulta = consulta.Where(e => e.CategoriaId == categoriaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            consulta = consulta.Where(e => e.Nombre.Contains(texto) || e.CodigoInventario.Contains(texto));
        }

        var modelo = new CatalogoViewModel
        {
            Equipos = await consulta.OrderBy(e => e.CodigoInventario).ToListAsync(),
            Categorias = await _context.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync(),
            CategoriaId = categoriaId,
            Busqueda = busqueda
        };
        return View(modelo);
    }

    public async Task<IActionResult> Details(int id)
    {
        var equipo = await _context.Equipos.AsNoTracking()
            .Include(e => e.Categoria)
            .FirstOrDefaultAsync(e => e.Id == id);
        return equipo is null ? NotFound() : View(equipo);
    }

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Create()
    {
        await CargarCategoriasAsync();
        return View(new Equipo());
    }

    [HttpPost]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CodigoInventario,Nombre,Descripcion,Ubicacion,CategoriaId,Estado")] Equipo equipo)
    {
        await ValidarCodigoUnicoAsync(equipo);
        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(equipo.CategoriaId);
            return View(equipo);
        }

        equipo.CodigoInventario = equipo.CodigoInventario.Trim().ToUpperInvariant();
        _context.Equipos.Add(equipo);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Equipo registrado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Edit(int id)
    {
        var equipo = await _context.Equipos.FindAsync(id);
        if (equipo is null)
        {
            return NotFound();
        }

        await CargarCategoriasAsync(equipo.CategoriaId);
        return View(equipo);
    }

    [HttpPost]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,CodigoInventario,Nombre,Descripcion,Ubicacion,CategoriaId,Estado")] Equipo equipo)
    {
        if (id != equipo.Id)
        {
            return NotFound();
        }

        await ValidarCodigoUnicoAsync(equipo);
        if (!ModelState.IsValid)
        {
            await CargarCategoriasAsync(equipo.CategoriaId);
            return View(equipo);
        }

        var existente = await _context.Equipos.FindAsync(id);
        if (existente is null)
        {
            return NotFound();
        }

        existente.CodigoInventario = equipo.CodigoInventario.Trim().ToUpperInvariant();
        existente.Nombre = equipo.Nombre;
        existente.Descripcion = equipo.Descripcion;
        existente.Ubicacion = equipo.Ubicacion;
        existente.CategoriaId = equipo.CategoriaId;
        existente.Estado = equipo.Estado;
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Equipo actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RolEncargado)]
    public async Task<IActionResult> Delete(int id)
    {
        var equipo = await _context.Equipos.AsNoTracking()
            .Include(e => e.Categoria)
            .FirstOrDefaultAsync(e => e.Id == id);
        return equipo is null ? NotFound() : View(equipo);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = RolEncargado)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var equipo = await _context.Equipos.FindAsync(id);
        if (equipo is null)
        {
            return NotFound();
        }

        if (await _context.Reservas.AnyAsync(r => r.EquipoId == id))
        {
            TempData["Error"] = "No se puede eliminar el equipo porque tiene reservas registradas. Puede marcarlo \"En mantenimiento\".";
            return RedirectToAction(nameof(Index));
        }

        _context.Equipos.Remove(equipo);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Equipo eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarCategoriasAsync(int? seleccionada = null)
    {
        var categorias = await _context.Categorias.AsNoTracking().OrderBy(c => c.Nombre).ToListAsync();
        ViewBag.Categorias = new SelectList(categorias, nameof(Categoria.Id), nameof(Categoria.Nombre), seleccionada);
    }

    private async Task ValidarCodigoUnicoAsync(Equipo equipo)
    {
        if (string.IsNullOrWhiteSpace(equipo.CodigoInventario))
        {
            return;
        }

        var codigo = equipo.CodigoInventario.Trim().ToUpperInvariant();
        if (await _context.Equipos.AnyAsync(e => e.CodigoInventario == codigo && e.Id != equipo.Id))
        {
            ModelState.AddModelError(nameof(Equipo.CodigoInventario), "Ya existe un equipo con ese código de inventario.");
        }
    }
}
