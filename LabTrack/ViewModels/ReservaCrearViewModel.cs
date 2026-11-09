using System.ComponentModel.DataAnnotations;

namespace LabTrack.ViewModels;

public class ReservaCrearViewModel
{
    [Display(Name = "Equipo")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un equipo.")]
    public int EquipoId { get; set; }

    [Required(ErrorMessage = "Seleccione la fecha.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime? Fecha { get; set; }

    [Required(ErrorMessage = "Seleccione la hora de inicio.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de inicio")]
    public TimeSpan? HoraInicio { get; set; }

    [Required(ErrorMessage = "Seleccione la hora de fin.")]
    [DataType(DataType.Time)]
    [Display(Name = "Hora de fin")]
    public TimeSpan? HoraFin { get; set; }

    public DateTime Inicio => Fecha!.Value.Date + HoraInicio!.Value;
    public DateTime Fin => Fecha!.Value.Date + HoraFin!.Value;
}
