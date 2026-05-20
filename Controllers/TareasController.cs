using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using trabfinal.Data;
using trabfinal.Models;

namespace trabfinal.Controllers;

[Authorize]
[Route("Tareas")]
public class TareasController : Controller
{
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;

    public TareasController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        await RegistrarActividadRedisAsync("index");
        return View();
    }

    [HttpGet("Listar")]
    public async Task<IActionResult> Listar()
    {
        var usuarioId = ObtenerUsuarioId();
        var tareasCacheKey = ObtenerTareasCacheKey(usuarioId);
        await RegistrarActividadRedisAsync("listar");

        var tareasEnCache = await _cache.GetStringAsync(tareasCacheKey);
        if (!string.IsNullOrEmpty(tareasEnCache))
        {
            return Content(tareasEnCache, "application/json");
        }

        var tareas = await _context.Tareas
            .Where(t => t.UsuarioId == usuarioId)
            .OrderBy(t => t.Completada)
            .ThenByDescending(t => t.FechaCreacion)
            .Select(t => new TareaResponse(
                t.Id,
                t.Titulo,
                t.Materia,
                t.Descripcion,
                t.FechaLimite,
                t.Recordatorio,
                t.Prioridad,
                t.Completada))
            .ToListAsync();

        var json = JsonSerializer.Serialize(tareas);
        await _cache.SetStringAsync(
            tareasCacheKey,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

        return Content(json, "application/json");
    }

    [HttpPost("Crear")]
    public async Task<IActionResult> Crear([FromBody] CrearTareaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            return BadRequest(new { mensaje = "El titulo es obligatorio." });
        }

        var usuarioId = ObtenerUsuarioId();
        var prioridad = NormalizarPrioridad(request.Prioridad);
        var tarea = new Tarea
        {
            Titulo = request.Titulo.Trim(),
            Materia = string.IsNullOrWhiteSpace(request.Materia) ? null : request.Materia.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
            FechaLimite = request.FechaLimite,
            Recordatorio = request.Recordatorio,
            Prioridad = prioridad,
            Completada = false,
            FechaCreacion = DateTime.UtcNow,
            UsuarioId = usuarioId
        };

        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ObtenerTareasCacheKey(usuarioId));
        await RegistrarActividadRedisAsync("crear");

        return Json(TareaResponse.FromEntity(tarea));
    }

    [HttpPost("Editar/{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarTareaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Titulo))
        {
            return BadRequest(new { mensaje = "El titulo es obligatorio." });
        }

        var usuarioId = ObtenerUsuarioId();
        var tarea = await _context.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);
        if (tarea is null)
        {
            return NotFound();
        }

        tarea.Titulo = request.Titulo.Trim();
        tarea.Materia = string.IsNullOrWhiteSpace(request.Materia) ? null : request.Materia.Trim();
        tarea.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        tarea.FechaLimite = request.FechaLimite;
        tarea.Recordatorio = request.Recordatorio;
        tarea.Prioridad = NormalizarPrioridad(request.Prioridad);

        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ObtenerTareasCacheKey(usuarioId));
        await RegistrarActividadRedisAsync("editar");

        return Json(TareaResponse.FromEntity(tarea));
    }

    [HttpPost("CambiarEstado/{id:int}")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        var tarea = await _context.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);
        if (tarea is null)
        {
            return NotFound();
        }

        tarea.Completada = !tarea.Completada;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ObtenerTareasCacheKey(usuarioId));
        await RegistrarActividadRedisAsync("cambiar-estado");

        return Json(new { id = tarea.Id, completada = tarea.Completada });
    }

    [HttpPost("Eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        var tarea = await _context.Tareas.FirstOrDefaultAsync(t => t.Id == id && t.UsuarioId == usuarioId);
        if (tarea is null)
        {
            return NotFound();
        }

        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(ObtenerTareasCacheKey(usuarioId));
        await RegistrarActividadRedisAsync("eliminar");

        return Ok();
    }

    private static string NormalizarPrioridad(string? prioridad)
    {
        return prioridad?.ToLowerInvariant() switch
        {
            "alta" => "alta",
            "baja" => "baja",
            _ => "media"
        };
    }

    private int ObtenerUsuarioId()
    {
        var userId = User.FindFirstValue("UserId");
        if (!int.TryParse(userId, out var usuarioId))
        {
            throw new InvalidOperationException("No se pudo identificar al usuario autenticado.");
        }

        return usuarioId;
    }

    private static string ObtenerTareasCacheKey(int usuarioId)
    {
        return $"tareas:listado:usuario:{usuarioId}";
    }

    private async Task RegistrarActividadRedisAsync(string accion)
    {
        var usuarioId = ObtenerUsuarioId();
        await _cache.SetStringAsync(
            $"tareas:ultima-actividad:usuario:{usuarioId}",
            JsonSerializer.Serialize(new
            {
                accion,
                usuarioId,
                fechaUtc = DateTime.UtcNow
            }),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
    }

    public class CrearTareaRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Materia { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaLimite { get; set; }
        public DateTime? Recordatorio { get; set; }
        public string Prioridad { get; set; } = "media";
    }

    public class EditarTareaRequest : CrearTareaRequest
    {
    }

    private sealed record TareaResponse(
        int id,
        string titulo,
        string? materia,
        string? descripcion,
        DateTime? fechaLimite,
        DateTime? recordatorio,
        string prioridad,
        bool completada)
    {
        public static TareaResponse FromEntity(Tarea tarea)
        {
            return new TareaResponse(
                tarea.Id,
                tarea.Titulo,
                tarea.Materia,
                tarea.Descripcion,
                tarea.FechaLimite,
                tarea.Recordatorio,
                tarea.Prioridad,
                tarea.Completada);
        }
    }
}
