using Microsoft.EntityFrameworkCore;
using trabfinal.Models;

namespace trabfinal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<ExamenPlan> ExamenPlanes { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<TipEstudio> TipsEstudio { get; set; }
    public DbSet<Tarea> Tareas { get; set; }
    public DbSet<InscripcionEvento> InscripcionesEventos { get; set; }
    public DbSet<ComentarioLugar> ComentariosLugares { get; set; }
    public DbSet<ComentarioRestaurante> ComentariosRestaurantes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InscripcionEvento>()
            .HasIndex(i => new { i.UsuarioId, i.EventoId })
            .IsUnique();

        modelBuilder.Entity<ComentarioLugar>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ComentariosLugares)
            .HasForeignKey(c => c.UsuarioId);

        modelBuilder.Entity<ComentarioRestaurante>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ComentariosRestaurantes)
            .HasForeignKey(c => c.UsuarioId);
    }
}