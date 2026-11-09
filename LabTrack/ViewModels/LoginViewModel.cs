using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Ingrese su código.")]
    [Display(Name = "Código de estudiante")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
