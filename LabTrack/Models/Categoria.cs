using System.ComponentModel.DataAnnotations;

namespace LabTrack.Models;

public class Categoria
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
}
