namespace LabTrack.Services;

public class SeguridadOptions
{
    public const string Seccion = "Seguridad";

    /// <summary>Intentos fallidos consecutivos permitidos antes de bloquear la cuenta.</summary>
    public int MaxIntentosFallidos { get; set; } = 3;

    /// <summary>Minutos que la cuenta permanece bloqueada.</summary>
    public int MinutosBloqueo { get; set; } = 5;
}
