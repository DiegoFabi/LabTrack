using LabTrack.Data;
using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Services;

public interface IReservaService
{
    Task<ResultadoOperacion> CrearAsync(int usuarioId, int equipoId, DateTime inicio, DateTime fin);
    Task<ResultadoOperacion> CancelarAsync(int reservaId, int usuarioId);
    Task<ResultadoOperacion> AprobarAsync(int reservaId);
    Task<ResultadoOperacion> RechazarAsync(int reservaId, string? motivo);
    Task<ResultadoOperacion> MarcarDevolucionAsync(int reservaId);
}

public class ReservaService : IReservaService
{
    public const string MensajeCruceHorario = "El equipo ya tiene una reserva aprobada que se cruza con el horario seleccionado.";
    public const string MensajeReservaSimultanea = "No puede tener más de un (1) equipo reservado de forma simultánea.";
    public const string MensajeEquipoNoDisponible = "Solo se pueden reservar equipos en estado \"Disponible\".";
    public const string MensajeEstudiantePenalizado = "Su cuenta se encuentra penalizada. No puede realizar nuevas reservas.";
    public const string MensajeMotivoObligatorio = "Debe ingresar el motivo del rechazo.";

    private readonly LabTrackContext _context;
    private readonly TimeProvider _reloj;

    public ReservaService(LabTrackContext context, TimeProvider reloj)
    {
        _context = context;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    public async Task<ResultadoOperacion> CrearAsync(int usuarioId, int equipoId, DateTime inicio, DateTime fin)
    {
        if (fin <= inicio)
        {
            return ResultadoOperacion.Error("La hora de fin debe ser posterior a la hora de inicio.");
        }

        if (inicio < Ahora)
        {
            return ResultadoOperacion.Error("No se pueden registrar reservas en fechas u horas pasadas.");
        }

        var usuario = await _context.Usuarios.FindAsync(usuarioId);
        if (usuario is null || usuario.Rol != RolUsuario.Estudiante)
        {
            return ResultadoOperacion.Error("Solo los estudiantes pueden registrar reservas.");
        }

        if (usuario.Estado == EstadoUsuario.Penalizado)
        {
            return ResultadoOperacion.Error(MensajeEstudiantePenalizado);
        }

        var equipo = await _context.Equipos.FindAsync(equipoId);
        if (equipo is null)
        {
            return ResultadoOperacion.Error("El equipo seleccionado no existe.");
        }

        if (equipo.Estado != EstadoEquipo.Disponible)
        {
            return ResultadoOperacion.Error(MensajeEquipoNoDisponible);
        }

        // Regla de negocio HU-03 (9): no se permite cruzar con una reserva aprobada del mismo equipo.
        if (await ExisteCruceConAprobadaAsync(equipoId, inicio, fin))
        {
            return ResultadoOperacion.Error(MensajeCruceHorario);
        }

        // Regla de negocio HU-03 (10): un estudiante no puede tener más de un equipo reservado a la vez.
        var tieneSimultanea = await _context.Reservas.AnyAsync(r =>
            r.UsuarioId == usuarioId &&
            (r.Estado == EstadoReserva.Pendiente || r.Estado == EstadoReserva.Aprobada) &&
            r.Inicio < fin && inicio < r.Fin);
        if (tieneSimultanea)
        {
            return ResultadoOperacion.Error(MensajeReservaSimultanea);
        }

        _context.Reservas.Add(new Reserva
        {
            UsuarioId = usuarioId,
            EquipoId = equipoId,
            Inicio = inicio,
            Fin = fin,
            Estado = EstadoReserva.Pendiente,
            FechaSolicitud = Ahora
        });
        await _context.SaveChangesAsync();

        return ResultadoOperacion.Ok("Solicitud de reserva registrada. Queda pendiente de aprobación.");
    }

    public async Task<ResultadoOperacion> CancelarAsync(int reservaId, int usuarioId)
    {
        var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId && r.UsuarioId == usuarioId);
        if (reserva is null)
        {
            return ResultadoOperacion.Error("La reserva no existe.");
        }

        if (reserva.Estado != EstadoReserva.Pendiente)
        {
            return ResultadoOperacion.Error("Solo se pueden cancelar reservas pendientes.");
        }

        reserva.Estado = EstadoReserva.Cancelada;
        reserva.FechaRespuesta = Ahora;
        await _context.SaveChangesAsync();
        return ResultadoOperacion.Ok("La reserva fue cancelada.");
    }

    public async Task<ResultadoOperacion> AprobarAsync(int reservaId)
    {
        var reserva = await _context.Reservas.Include(r => r.Equipo).FirstOrDefaultAsync(r => r.Id == reservaId);
        if (reserva is null)
        {
            return ResultadoOperacion.Error("La reserva no existe.");
        }

        if (reserva.Estado != EstadoReserva.Pendiente)
        {
            return ResultadoOperacion.Error("Solo se pueden aprobar reservas pendientes.");
        }

        if (reserva.Equipo!.Estado == EstadoEquipo.Mantenimiento)
        {
            return ResultadoOperacion.Error("El equipo se encuentra en mantenimiento.");
        }

        if (await ExisteCruceConAprobadaAsync(reserva.EquipoId, reserva.Inicio, reserva.Fin, reserva.Id))
        {
            return ResultadoOperacion.Error(MensajeCruceHorario);
        }

        reserva.Estado = EstadoReserva.Aprobada;
        reserva.FechaRespuesta = Ahora;
        reserva.Equipo.Estado = EstadoEquipo.Prestado;
        await _context.SaveChangesAsync();

        return ResultadoOperacion.Ok("Reserva aprobada. El equipo pasó a estado \"Prestado\".");
    }

    public async Task<ResultadoOperacion> RechazarAsync(int reservaId, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return ResultadoOperacion.Error(MensajeMotivoObligatorio);
        }

        var reserva = await _context.Reservas.FindAsync(reservaId);
        if (reserva is null)
        {
            return ResultadoOperacion.Error("La reserva no existe.");
        }

        if (reserva.Estado != EstadoReserva.Pendiente)
        {
            return ResultadoOperacion.Error("Solo se pueden rechazar reservas pendientes.");
        }

        reserva.Estado = EstadoReserva.Rechazada;
        reserva.MotivoRechazo = motivo.Trim();
        reserva.FechaRespuesta = Ahora;
        await _context.SaveChangesAsync();

        return ResultadoOperacion.Ok("La reserva fue rechazada.");
    }

    public async Task<ResultadoOperacion> MarcarDevolucionAsync(int reservaId)
    {
        var reserva = await _context.Reservas
            .Include(r => r.Equipo)
            .Include(r => r.Usuario)
            .FirstOrDefaultAsync(r => r.Id == reservaId);
        if (reserva is null)
        {
            return ResultadoOperacion.Error("La reserva no existe.");
        }

        if (reserva.Estado != EstadoReserva.Aprobada)
        {
            return ResultadoOperacion.Error("Solo se puede registrar la devolución de reservas aprobadas (activas).");
        }

        var ahora = Ahora;
        reserva.Estado = EstadoReserva.Devuelta;
        reserva.FechaDevolucion = ahora;

        // HU-05 (15): el equipo vuelve a estar disponible, salvo que tenga otra reserva aprobada vigente.
        var tieneOtraAprobada = await _context.Reservas.AnyAsync(r =>
            r.EquipoId == reserva.EquipoId && r.Id != reserva.Id && r.Estado == EstadoReserva.Aprobada);
        if (!tieneOtraAprobada && reserva.Equipo!.Estado == EstadoEquipo.Prestado)
        {
            reserva.Equipo.Estado = EstadoEquipo.Disponible;
        }

        // HU-05 (16): devolución fuera de hora => el estudiante queda penalizado.
        if (ahora > reserva.Fin)
        {
            reserva.DevueltaConRetraso = true;
            reserva.Usuario!.Estado = EstadoUsuario.Penalizado;
            await _context.SaveChangesAsync();
            return ResultadoOperacion.Ok($"Devolución registrada con retraso. El estudiante {reserva.Usuario.NombreCompleto} fue penalizado.");
        }

        await _context.SaveChangesAsync();
        return ResultadoOperacion.Ok("Devolución registrada. El equipo está disponible nuevamente.");
    }

    private Task<bool> ExisteCruceConAprobadaAsync(int equipoId, DateTime inicio, DateTime fin, int? excluirReservaId = null) =>
        _context.Reservas.AnyAsync(r =>
            r.EquipoId == equipoId &&
            r.Estado == EstadoReserva.Aprobada &&
            (excluirReservaId == null || r.Id != excluirReservaId) &&
            r.Inicio < fin && inicio < r.Fin);
}
