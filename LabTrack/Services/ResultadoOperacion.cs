namespace LabTrack.Services;

public class ResultadoOperacion
{
    public bool Exito { get; private init; }
    public string Mensaje { get; private init; } = string.Empty;

    public static ResultadoOperacion Ok(string mensaje) => new() { Exito = true, Mensaje = mensaje };
    public static ResultadoOperacion Error(string mensaje) => new() { Exito = false, Mensaje = mensaje };
}
