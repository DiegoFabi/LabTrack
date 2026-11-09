using LabTrack.Models;

namespace LabTrack.ViewModels;

public class PanelEstudianteViewModel
{
    public Usuario Usuario { get; set; } = null!;
    public IReadOnlyList<Reserva> ReservasActivas { get; set; } = Array.Empty<Reserva>();
    public IReadOnlyList<Reserva> UltimasReservas { get; set; } = Array.Empty<Reserva>();
    public int EquiposDisponibles { get; set; }
}

public class PanelEncargadoViewModel
{
    public int TotalEquipos { get; set; }
    public int EquiposDisponibles { get; set; }
    public int EquiposPrestados { get; set; }
    public int EquiposMantenimiento { get; set; }
    public int ReservasPendientes { get; set; }
    public int PrestamosActivos { get; set; }
    public int PrestamosVencidos { get; set; }
    public int EstudiantesPenalizados { get; set; }
    public IReadOnlyList<Reserva> UltimasSolicitudes { get; set; } = Array.Empty<Reserva>();
}
