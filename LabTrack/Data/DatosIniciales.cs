using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data;

/// <summary>
/// Datos iniciales (usuarios, categorías y equipos). Se registran en el modelo con HasData,
/// por lo que se insertan al ejecutar Update-Database junto con la migración.
/// </summary>
public static class DatosIniciales
{
    // Hashes generados una sola vez con PasswordHasher (valores fijos para que la migración no cambie).
    // ADMIN01 -> Admin123*   |   Estudiantes -> Estudiante123*
    private const string HashAdmin = "AQAAAAIAAYagAAAAEK9+v+01h357habHkNShJFGLLFuAhq74+mlwPkS7ps3tu8Zjm6HGyW3njx3PVu0IPg==";
    private const string HashEstudiante = "AQAAAAIAAYagAAAAECGK0lOAyeAlOmiFdbfmjxRK0n78zUevqLMdGYK336DqVJb6ApHShpZYU4PMKa/2Mg==";

    private static readonly DateTime FechaRegistro = new(2026, 11, 1, 8, 0, 0);

    public static void Cargar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>().HasData(
            Usuario(1, "ADMIN01", "Encargado de Laboratorio", "encargado@labtrack.edu.pe", HashAdmin, RolUsuario.Encargado),
            Usuario(2, "N00012345", "Ana Torres Ríos", "ana.torres@upn.pe", HashEstudiante, RolUsuario.Estudiante),
            Usuario(3, "N00023456", "Luis Ramírez Paz", "luis.ramirez@upn.pe", HashEstudiante, RolUsuario.Estudiante),
            Usuario(4, "N00034567", "María Quispe Soto", "maria.quispe@upn.pe", HashEstudiante, RolUsuario.Estudiante));

        const int computo = 1, electronica = 2, redes = 3;
        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = computo, Nombre = "Cómputo", Descripcion = "Laptops, computadoras y periféricos." },
            new Categoria { Id = electronica, Nombre = "Electrónica", Descripcion = "Instrumentos y kits de electrónica." },
            new Categoria { Id = redes, Nombre = "Redes", Descripcion = "Equipos de networking y comunicaciones." });

        modelBuilder.Entity<Equipo>().HasData(
            new Equipo { Id = 1, CodigoInventario = "CMP-001", Nombre = "Laptop Dell Latitude 5440", CategoriaId = computo, Ubicacion = "Lab. A-101", Descripcion = "Core i7, 16 GB RAM, 512 GB SSD." },
            new Equipo { Id = 2, CodigoInventario = "CMP-002", Nombre = "Laptop Lenovo ThinkPad E14", CategoriaId = computo, Ubicacion = "Lab. A-101", Descripcion = "Core i5, 16 GB RAM." },
            new Equipo { Id = 3, CodigoInventario = "CMP-003", Nombre = "Raspberry Pi 5 (8 GB)", CategoriaId = computo, Ubicacion = "Lab. A-102" },
            new Equipo { Id = 4, CodigoInventario = "CMP-004", Nombre = "Proyector Epson PowerLite", CategoriaId = computo, Ubicacion = "Lab. A-102", Estado = EstadoEquipo.Mantenimiento },
            new Equipo { Id = 5, CodigoInventario = "ELE-001", Nombre = "Osciloscopio Rigol DS1054Z", CategoriaId = electronica, Ubicacion = "Lab. B-201" },
            new Equipo { Id = 6, CodigoInventario = "ELE-002", Nombre = "Multímetro Fluke 117", CategoriaId = electronica, Ubicacion = "Lab. B-201" },
            new Equipo { Id = 7, CodigoInventario = "ELE-003", Nombre = "Kit Arduino Uno R4", CategoriaId = electronica, Ubicacion = "Lab. B-202" },
            new Equipo { Id = 8, CodigoInventario = "ELE-004", Nombre = "Fuente de poder regulable 30V", CategoriaId = electronica, Ubicacion = "Lab. B-202" },
            new Equipo { Id = 9, CodigoInventario = "RED-001", Nombre = "Router Cisco ISR 4321", CategoriaId = redes, Ubicacion = "Lab. C-301" },
            new Equipo { Id = 10, CodigoInventario = "RED-002", Nombre = "Switch Cisco Catalyst 2960", CategoriaId = redes, Ubicacion = "Lab. C-301" },
            new Equipo { Id = 11, CodigoInventario = "RED-003", Nombre = "Access Point Ubiquiti U6", CategoriaId = redes, Ubicacion = "Lab. C-302" },
            new Equipo { Id = 12, CodigoInventario = "RED-004", Nombre = "Crimpadora + Tester de cable", CategoriaId = redes, Ubicacion = "Lab. C-302" });
    }

    private static Usuario Usuario(int id, string codigo, string nombre, string email, string hash, RolUsuario rol) => new()
    {
        Id = id,
        Codigo = codigo,
        NombreCompleto = nombre,
        Email = email,
        PasswordHash = hash,
        Rol = rol,
        Estado = EstadoUsuario.Activo,
        FechaRegistro = FechaRegistro
    };
}
