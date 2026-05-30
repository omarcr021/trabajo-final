using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using trabfinal.Data;
using trabfinal.Models;
using trabfinal.Services;

namespace trabfinal.Controllers;

[Authorize]
[Route("Eventos")]
public class EventosController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEventosService _eventosService;

    public EventosController(AppDbContext context, IEventosService eventosService)
    {
        _context = context;
        _eventosService = eventosService;
    }



    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        await _eventosService.SincronizarEventosAsync();

        var usuarioId = ObtenerUsuarioId();
        ViewBag.EventosInscritos = await _context.InscripcionesEventos
            .Where(i => i.UsuarioId == usuarioId)
            .Select(i => i.EventoId)
            .ToListAsync();

        var eventos = await _context.Eventos.OrderBy(e => e.FechaInicio).ToListAsync();
        return View(eventos);
    }

    [HttpGet("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var evento = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return NotFound();
        }

        var usuarioId = ObtenerUsuarioId();
        ViewBag.EstaRegistrado = await _context.InscripcionesEventos
            .AnyAsync(i => i.EventoId == id && i.UsuarioId == usuarioId);

        ViewBag.EventosSimilares = await _context.Eventos
            .Where(e => e.Id != id && e.Categoria == evento.Categoria)
            .OrderBy(e => e.FechaInicio)
            .Take(3)
            .ToListAsync();

        return View(evento);
    }

    [HttpPost("Registrar/{id:int}")]
    public async Task<IActionResult> Registrar(int id)
    {
        var evento = await _context.Eventos.FirstOrDefaultAsync(e => e.Id == id);
        if (evento is null)
        {
            return NotFound();
        }

        var usuarioId = ObtenerUsuarioId();
        var yaRegistrado = await _context.InscripcionesEventos
            .AnyAsync(i => i.EventoId == id && i.UsuarioId == usuarioId);

        if (!yaRegistrado)
        {
            _context.InscripcionesEventos.Add(new InscripcionEvento
            {
                EventoId = id,
                UsuarioId = usuarioId,
                FechaRegistro = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
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
