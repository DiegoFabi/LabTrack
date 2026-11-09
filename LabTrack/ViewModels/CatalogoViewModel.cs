using LabTrack.Models;

namespace LabTrack.ViewModels;

public class CatalogoViewModel
{
    public IReadOnlyList<Equipo> Equipos { get; set; } = Array.Empty<Equipo>();
    public IReadOnlyList<Categoria> Categorias { get; set; } = Array.Empty<Categoria>();
    public int? CategoriaId { get; set; }
    public string? Busqueda { get; set; }
    public string? CategoriaSeleccionada => Categorias.FirstOrDefault(c => c.Id == CategoriaId)?.Nombre;
}
