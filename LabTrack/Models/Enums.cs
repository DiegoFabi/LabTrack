using System.ComponentModel.DataAnnotations;

namespace LabTrack.Models;

public enum RolUsuario
{
    Estudiante,
    Encargado
}

public enum EstadoUsuario
{
    Activo,
    Penalizado
}

public enum EstadoEquipo
{
    Disponible,
    Prestado,
    [Display(Name = "En mantenimiento")]
    Mantenimiento
}

public enum EstadoReserva
{
    Pendiente,
    Aprobada,
    Rechazada,
    Devuelta,
    Cancelada
}
