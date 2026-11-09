using System.ComponentModel.DataAnnotations;

namespace LabTrack.Models;

public class Reserva
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int EquipoId { get; set; }
    public Equipo? Equipo { get; set; }

    [Display(Name = "Inicio")]
    public DateTime Inicio { get; set; }

    [Display(Name = "Fin")]
    public DateTime Fin { get; set; }

    public EstadoReserva Estado { get; set; } = EstadoReserva.Pendiente;

    [StringLength(250)]
    [Display(Name = "Motivo de rechazo")]
    public string? MotivoRechazo { get; set; }

    [Display(Name = "Fecha de solicitud")]
    public DateTime FechaSolicitud { get; set; }

    [Display(Name = "Fecha de respuesta")]
    public DateTime? FechaRespuesta { get; set; }

    [Display(Name = "Fecha de devolución")]
    public DateTime? FechaDevolucion { get; set; }

    [Display(Name = "Devuelto con retraso")]
    public bool DevueltaConRetraso { get; set; }

    /// <summary>Indica si el intervalo [inicio, fin) se cruza con el de esta reserva.</summary>
    public bool SeCruzaCon(DateTime inicio, DateTime fin) => Inicio < fin && inicio < Fin;
}
