using Microsoft.EntityFrameworkCore;
using trabfinal.Models;

namespace trabfinal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Tarea> Tareas { get; set; }
    public DbSet<Evento> Eventos { get; set; }
    public DbSet<Lugar> Lugares { get; set; }
    public DbSet<RestauranteCercano> RestaurantesCercanos { get; set; }
    public DbSet<TipEstudio> TipsEstudio { get; set; }
    public DbSet<ExamenPlan> ExamenesPlanes { get; set; }
    public DbSet<InscripcionEvento> InscripcionesEventos { get; set; }
    public DbSet<ComentarioLugar> ComentariosLugares { get; set; }
    public DbSet<ComentarioRestaurante> ComentariosRestaurantes { get; set; }
    public DbSet<SincronizacionLog> SincronizacionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // InscripcionEvento: índice único para evitar inscripciones duplicadas
        modelBuilder.Entity<InscripcionEvento>()
            .HasIndex(i => new { i.UsuarioId, i.EventoId })
            .IsUnique();

        // InscripcionEvento -> Usuario
        modelBuilder.Entity<InscripcionEvento>()
            .HasOne(i => i.Usuario)
            .WithMany(u => u.InscripcionesEventos)
            .HasForeignKey(i => i.UsuarioId);

        // InscripcionEvento -> Evento
        modelBuilder.Entity<InscripcionEvento>()
            .HasOne(i => i.Evento)
            .WithMany(e => e.Inscripciones)
            .HasForeignKey(i => i.EventoId);

        // ComentarioLugar -> Usuario
        modelBuilder.Entity<ComentarioLugar>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ComentariosLugares)
            .HasForeignKey(c => c.UsuarioId);

        // ComentarioLugar -> Lugar
        modelBuilder.Entity<ComentarioLugar>()
            .HasOne(c => c.Lugar)
            .WithMany(l => l.Comentarios)
            .HasForeignKey(c => c.LugarId);

        // ComentarioRestaurante -> Usuario
        modelBuilder.Entity<ComentarioRestaurante>()
            .HasOne(c => c.Usuario)
            .WithMany(u => u.ComentariosRestaurantes)
            .HasForeignKey(c => c.UsuarioId);

        // ComentarioRestaurante -> RestauranteCercano
        modelBuilder.Entity<ComentarioRestaurante>()
            .HasOne(c => c.Restaurante)
            .WithMany(r => r.Comentarios)
            .HasForeignKey(c => c.RestauranteId);

        // ExamenPlan -> Usuario
        modelBuilder.Entity<ExamenPlan>()
            .ToTable("ExamenesPlanes")
            .HasOne(e => e.Usuario)
            .WithMany(u => u.ExamenesPlanes)
            .HasForeignKey(e => e.UsuarioId);
    }
}