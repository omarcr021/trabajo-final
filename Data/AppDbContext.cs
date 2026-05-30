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
}