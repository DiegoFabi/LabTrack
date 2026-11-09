using LabTrack.Data;
using LabTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Controllers;

[Authorize(Roles = nameof(RolUsuario.Encargado))]
public class CategoriasController : Controller
{
    private readonly LabTrackContext _context;

    public CategoriasController(LabTrackContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categorias = await _context.Categorias.AsNoTracking()
            .Include(c => c.Equipos)
            .OrderBy(c => c.Nombre)
            .ToListAsync();
        return View(categorias);
    }

    public IActionResult Create() => View(new Categoria());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Nombre,Descripcion")] Categoria categoria)
    {
        await ValidarNombreUnicoAsync(categoria);
        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        _context.Categorias.Add(categoria);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Categoría registrada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        return categoria is null ? NotFound() : View(categoria);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Descripcion")] Categoria categoria)
    {
        if (id != categoria.Id)
        {
            return NotFound();
        }

        await ValidarNombreUnicoAsync(categoria);
        if (!ModelState.IsValid)
        {
            return View(categoria);
        }

        var existente = await _context.Categorias.FindAsync(id);
        if (existente is null)
        {
            return NotFound();
        }

        existente.Nombre = categoria.Nombre.Trim();
        existente.Descripcion = categoria.Descripcion;
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Categoría actualizada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var categoria = await _context.Categorias.AsNoTracking()
            .Include(c => c.Equipos)
            .FirstOrDefaultAsync(c => c.Id == id);
        return categoria is null ? NotFound() : View(categoria);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria is null)
        {
            return NotFound();
        }

        if (await _context.Equipos.AnyAsync(e => e.CategoriaId == id))
        {
            TempData["Error"] = "No se puede eliminar la categoría porque tiene equipos asociados.";
            return RedirectToAction(nameof(Index));
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();
        TempData["Exito"] = "Categoría eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarNombreUnicoAsync(Categoria categoria)
    {
        if (string.IsNullOrWhiteSpace(categoria.Nombre))
        {
            return;
        }

        var nombre = categoria.Nombre.Trim();
        if (await _context.Categorias.AnyAsync(c => c.Nombre == nombre && c.Id != categoria.Id))
        {
            ModelState.AddModelError(nameof(Categoria.Nombre), "Ya existe una categoría con ese nombre.");
        }
    }
}
