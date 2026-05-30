using Microsoft.EntityFrameworkCore;
using trabfinal.Data;
using trabfinal.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IRestaurantesService, RestaurantesService>();
builder.Services.AddScoped<IEventosService, EventosService>();

// Add DbContext
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "app.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

// Add Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "proyecto_campusgo:";
});

builder.Services.AddScoped<MLService>();

// Add Authentication Cookie
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
    EnsureTareasUsuarioIdColumn(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Render gestiona el HTTPS automáticamente, por lo que deshabilitamos esta redirección interna 
// para evitar el warning "Failed to determine the https port for redirect".
// app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.MapGet("/test-api", async (trabfinal.Services.IRestaurantesService r, trabfinal.Services.IEventosService e, trabfinal.Data.AppDbContext db) => {
    await r.SincronizarRestaurantesAsync();
    await e.SincronizarEventosAsync();
    var rests = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(System.Linq.Queryable.Select(db.RestaurantesCercanos, x => x.Nombre));
    var evs = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(System.Linq.Queryable.Select(db.Eventos, x => x.Titulo));
    return new { Restaurantes = rests, Eventos = evs };
});

app.Run();

static void EnsureTareasUsuarioIdColumn(AppDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        connection.Open();
    }

    using var tableCommand = connection.CreateCommand();
    tableCommand.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'Tareas';";
    if (tableCommand.ExecuteScalar() is null)
    {
        return;
    }

    var hasUsuarioId = false;
    using (var columnsCommand = connection.CreateCommand())
    {
        columnsCommand.CommandText = "PRAGMA table_info('Tareas');";
        using var reader = columnsCommand.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader["name"]?.ToString(), "UsuarioId", StringComparison.OrdinalIgnoreCase))
            {
                hasUsuarioId = true;
                break;
            }
        }
    }

    if (!hasUsuarioId)
    {
        using var addColumnCommand = connection.CreateCommand();
        addColumnCommand.CommandText = "ALTER TABLE Tareas ADD COLUMN UsuarioId INTEGER NULL;";
        addColumnCommand.ExecuteNonQuery();
    }

    using var indexCommand = connection.CreateCommand();
    indexCommand.CommandText = "CREATE INDEX IF NOT EXISTS IX_Tareas_UsuarioId ON Tareas (UsuarioId);";
    indexCommand.ExecuteNonQuery();
}
