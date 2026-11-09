using System.ComponentModel.DataAnnotations;
using LabTrack.Models;

namespace LabTrack.ViewModels;

public class RechazoViewModel
{
    public int ReservaId { get; set; }

    [Required(ErrorMessage = "Debe ingresar el motivo del rechazo.")]
    [StringLength(250, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 250 caracteres.")]
    [Display(Name = "Motivo del rechazo")]
    public string? Motivo { get; set; }

    public Reserva? Reserva { get; set; }
}
