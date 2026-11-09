using System.ComponentModel.DataAnnotations;

namespace LabTrack.Models;

public class Usuario
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required, StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [EmailAddress, StringLength(100)]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; } = RolUsuario.Estudiante;

    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;

    [Display(Name = "Intentos fallidos")]
    public int IntentosFallidos { get; set; }

    [Display(Name = "Bloqueado hasta")]
    public DateTime? BloqueadoHasta { get; set; }

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }

    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();

    public bool EstaBloqueado(DateTime ahora) => BloqueadoHasta.HasValue && BloqueadoHasta.Value > ahora;
}
