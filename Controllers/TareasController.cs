using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using trabfinal.Data;
using trabfinal.Models;

namespace trabfinal.Controllers;

[Route("Tareas")]
public class TareasController : Controller
{
    private const string TareasCacheKey = "tareas:listado";
    private readonly AppDbContext _context;
    private readonly IDistributedCache _cache;

    public TareasController(AppDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("Listar")]
    public async Task<IActionResult> Listar()
    {
        var tareasEnCache = await _cache.GetStringAsync(TareasCacheKey);
        if (!string.IsNullOrEmpty(tareasEnCache))
        {
            return Content(tareasEnCache, "application/json");
        }

        var tareas = await _context.Tareas
            .OrderBy(t => t.Completada)
            .ThenByDescending(t => t.FechaCreacion)
            .Select(t => new TareaResponse(
                t.Id,
                t.Titulo,
                t.Materia,
                t.Prioridad,
                t.Completada))
            .ToListAsync();

        var json = JsonSerializer.Serialize(tareas);
        await _cache.SetStringAsync(
            TareasCacheKey,
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

        var prioridad = NormalizarPrioridad(request.Prioridad);
        var tarea = new Tarea
        {
            Titulo = request.Titulo.Trim(),
            Materia = string.IsNullOrWhiteSpace(request.Materia) ? null : request.Materia.Trim(),
            Prioridad = prioridad,
            Completada = false,
            FechaCreacion = DateTime.UtcNow
        };

        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(TareasCacheKey);

        return Json(TareaResponse.FromEntity(tarea));
    }

    [HttpPost("CambiarEstado/{id:int}")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea is null)
        {
            return NotFound();
        }

        tarea.Completada = !tarea.Completada;
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(TareasCacheKey);

        return Json(new { id = tarea.Id, completada = tarea.Completada });
    }

    [HttpPost("Eliminar/{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var tarea = await _context.Tareas.FindAsync(id);
        if (tarea is null)
        {
            return NotFound();
        }

        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        await _cache.RemoveAsync(TareasCacheKey);

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

    public class CrearTareaRequest
    {
        public string Titulo { get; set; } = string.Empty;
        public string? Materia { get; set; }
        public string Prioridad { get; set; } = "media";
    }

    private sealed record TareaResponse(
        int id,
        string titulo,
        string? materia,
        string prioridad,
        bool completada)
    {
        public static TareaResponse FromEntity(Tarea tarea)
        {
            return new TareaResponse(
                tarea.Id,
                tarea.Titulo,
                tarea.Materia,
                tarea.Prioridad,
                tarea.Completada);
        }
    }
}
