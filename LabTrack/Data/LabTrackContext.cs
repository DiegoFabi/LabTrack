using LabTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace LabTrack.Data;

public class LabTrackContext : DbContext
{
    public LabTrackContext(DbContextOptions<LabTrackContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Equipo> Equipos => Set<Equipo>();
    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>(e =>
        {
            e.HasIndex(u => u.Codigo).IsUnique();
            e.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);
            e.Property(u => u.Estado).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Categoria>(e =>
        {
            e.HasIndex(c => c.Nombre).IsUnique();
        });

        modelBuilder.Entity<Equipo>(e =>
        {
            e.HasIndex(x => x.CodigoInventario).IsUnique();
            e.Property(x => x.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(x => x.Categoria)
                .WithMany(c => c.Equipos)
                .HasForeignKey(x => x.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reserva>(e =>
        {
            e.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20);
            e.HasOne(r => r.Usuario)
                .WithMany(u => u.Reservas)
                .HasForeignKey(r => r.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Equipo)
                .WithMany(x => x.Reservas)
                .HasForeignKey(r => r.EquipoId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => new { r.EquipoId, r.Estado });
            e.HasIndex(r => new { r.UsuarioId, r.Estado });
        });

        DatosIniciales.Cargar(modelBuilder);
    }
}
