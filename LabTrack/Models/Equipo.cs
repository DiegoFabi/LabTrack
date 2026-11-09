using System.ComponentModel.DataAnnotations;

namespace LabTrack.Models;

public class Equipo
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código de inventario es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Código de inventario")]
    public string CodigoInventario { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [StringLength(50)]
    [Display(Name = "Ubicación")]
    public string? Ubicacion { get; set; }

    [Display(Name = "Categoría")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaId { get; set; }

    public Categoria? Categoria { get; set; }

    public EstadoEquipo Estado { get; set; } = EstadoEquipo.Disponible;

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
