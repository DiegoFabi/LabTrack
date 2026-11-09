using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace LabTrack.Models;

public static class EnumExtensions
{
    public static string Nombre(this Enum valor)
    {
        var miembro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
        return miembro?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
    }

    public static string CssBadge(this EstadoEquipo estado) => estado switch
    {
        EstadoEquipo.Disponible => "bg-success",
        EstadoEquipo.Prestado => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    public static string CssBadge(this EstadoReserva estado) => estado switch
    {
        EstadoReserva.Pendiente => "bg-info text-dark",
        EstadoReserva.Aprobada => "bg-success",
        EstadoReserva.Rechazada => "bg-danger",
        EstadoReserva.Devuelta => "bg-primary",
        _ => "bg-secondary"
    };

    public static string CssBadge(this EstadoUsuario estado) =>
        estado == EstadoUsuario.Activo ? "bg-success" : "bg-danger";
}
