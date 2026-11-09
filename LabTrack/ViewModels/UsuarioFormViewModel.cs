using System.ComponentModel.DataAnnotations;
using LabTrack.Models;

namespace LabTrack.ViewModels;

public class UsuarioFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Correo no válido.")]
    [StringLength(100)]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [DataType(DataType.Password)]
    [StringLength(50, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [Display(Name = "Rol")]
    public RolUsuario Rol { get; set; } = RolUsuario.Estudiante;

    [Display(Name = "Estado")]
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
}
