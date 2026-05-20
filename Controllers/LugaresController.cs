using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using trabfinal.Data;
using trabfinal.Models;
using trabfinal.Services;

namespace trabfinal.Controllers;

[Authorize]
[Route("Lugares")]
public class LugaresController : Controller
{
    private readonly AppDbContext _context;
    private readonly IRestaurantesService _restaurantesService;

    public LugaresController(AppDbContext context, IRestaurantesService restaurantesService)
    {
        _context = context;
        _restaurantesService = restaurantesService;
    }

    private static readonly List<Lugar> Lugares = new()
    {
        new Lugar { Id = 1, Nombre = "FIA DATA", Categoria = "Académico", Direccion = "Av. la Fontana 1271, La Molina", Descripcion = "Espacio universitario de tecnología, clases, investigación y actividades académicas." },
        new Lugar { Id = 2, Nombre = "Biblioteca Central", Categoria = "Estudio", Direccion = "Campus principal", Descripcion = "Zona tranquila para estudiar, revisar materiales y trabajar en equipo." },
        new Lugar { Id = 3, Nombre = "Plaza Central", Categoria = "Social", Direccion = "Ingreso principal del campus", Descripcion = "Punto de encuentro para estudiantes, ferias y actividades abiertas." }
    };



    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        await _restaurantesService.SincronizarRestaurantesAsync();

        ViewBag.UsuarioId = ObtenerUsuarioId();
        ViewBag.Restaurantes = await _context.RestaurantesCercanos.ToListAsync();
        ViewBag.ComentariosRestaurantes = await _context.ComentariosRestaurantes
            .Include(c => c.Usuario)
            .OrderByDescending(c => c.FechaPublicacion)
            .ToListAsync();

        return View();
    }

    [HttpPost("ComentarRestaurante/{restauranteId:int}")]
    public async Task<IActionResult> ComentarRestaurante(int restauranteId, string texto, int calificacion)
    {
        if (!await _context.RestaurantesCercanos.AnyAsync(r => r.Id == restauranteId))
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            _context.ComentariosRestaurantes.Add(new ComentarioRestaurante
            {
                RestauranteId = restauranteId,
                UsuarioId = ObtenerUsuarioId(),
                Texto = texto.Trim(),
                Calificacion = Math.Clamp(calificacion, 1, 5),
                FechaPublicacion = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("EditarComentarioRestaurante/{id:int}")]
    public async Task<IActionResult> EditarComentarioRestaurante(int id, string texto, int calificacion)
    {
        var usuarioId = ObtenerUsuarioId();
        var comentario = await _context.ComentariosRestaurantes.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (comentario is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            comentario.Texto = texto.Trim();
            comentario.Calificacion = Math.Clamp(calificacion, 1, 5);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("EliminarComentarioRestaurante/{id:int}")]
    public async Task<IActionResult> EliminarComentarioRestaurante(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        var comentario = await _context.ComentariosRestaurantes.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (comentario is null)
        {
            return NotFound();
        }

        _context.ComentariosRestaurantes.Remove(comentario);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var lugar = Lugares.FirstOrDefault(l => l.Id == id);
        if (lugar is null)
        {
            return NotFound();
        }

        ViewBag.Restaurantes = await _context.RestaurantesCercanos.Take(6).ToListAsync();
        ViewBag.UsuarioId = ObtenerUsuarioId();
        ViewBag.Comentarios = await _context.ComentariosLugares
            .Include(c => c.Usuario)
            .Where(c => c.LugarId == id)
            .OrderByDescending(c => c.FechaPublicacion)
            .ToListAsync();

        return View(lugar);
    }

    [HttpPost("Comentar/{lugarId:int}")]
    public async Task<IActionResult> Comentar(int lugarId, string texto, int calificacion)
    {
        if (!Lugares.Any(l => l.Id == lugarId))
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            _context.ComentariosLugares.Add(new ComentarioLugar
            {
                LugarId = lugarId,
                UsuarioId = ObtenerUsuarioId(),
                Texto = texto.Trim(),
                Calificacion = Math.Clamp(calificacion, 1, 5),
                FechaPublicacion = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = lugarId });
    }

    [HttpPost("EditarComentario/{id:int}")]
    public async Task<IActionResult> EditarComentario(int id, string texto, int calificacion)
    {
        var usuarioId = ObtenerUsuarioId();
        var comentario = await _context.ComentariosLugares.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (comentario is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(texto))
        {
            comentario.Texto = texto.Trim();
            comentario.Calificacion = Math.Clamp(calificacion, 1, 5);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = comentario.LugarId });
    }

    [HttpPost("EliminarComentario/{id:int}")]
    public async Task<IActionResult> EliminarComentario(int id)
    {
        var usuarioId = ObtenerUsuarioId();
        var comentario = await _context.ComentariosLugares.FirstOrDefaultAsync(c => c.Id == id && c.UsuarioId == usuarioId);
        if (comentario is null)
        {
            return NotFound();
        }

        var lugarId = comentario.LugarId;
        _context.ComentariosLugares.Remove(comentario);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = lugarId });
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
}
