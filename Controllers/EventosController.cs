using Microsoft.AspNetCore.Mvc;
using trabfinal.Models;

namespace trabfinal.Controllers;

[Route("Eventos")]
public class EventosController : Controller
{
    private static readonly List<Evento> Eventos = new()
    {
        new Evento
        {
            Id = 1,
            Titulo = "Hackathon de Innovacion Universitaria",
            Categoria = "Academico",
            FechaInicio = DateTime.Today.AddDays(1).AddHours(9),
            FechaFin = DateTime.Today.AddDays(1).AddHours(18),
            Ubicacion = "Laboratorio de Computo A-204",
            Descripcion = "Una jornada intensiva para desarrollar soluciones digitales con estudiantes de distintas facultades y mentores invitados."
        },
        new Evento
        {
            Id = 2,
            Titulo = "Feria de Clubes y Vida Estudiantil",
            Categoria = "Social",
            FechaInicio = DateTime.Today.AddDays(3).AddHours(11),
            FechaFin = DateTime.Today.AddDays(3).AddHours(16),
            Ubicacion = "Plaza Central del Campus",
            Descripcion = "Descubre talleres, voluntariados, comunidades artisticas y espacios de integracion para nuevos estudiantes."
        },
        new Evento
        {
            Id = 3,
            Titulo = "Torneo Interfacultades de Futbol",
            Categoria = "Deportivo",
            FechaInicio = DateTime.Today.AddDays(5).AddHours(8),
            FechaFin = DateTime.Today.AddDays(5).AddHours(13),
            Ubicacion = "Complejo Deportivo Universitario",
            Descripcion = "Encuentro deportivo entre facultades con actividades de animacion, musica y puntos de hidratacion."
        },
        new Evento
        {
            Id = 4,
            Titulo = "Jornada de Matricula y Orientacion",
            Categoria = "Administrativo",
            FechaInicio = DateTime.Today.AddDays(2).AddHours(10),
            FechaFin = DateTime.Today.AddDays(2).AddHours(14),
            Ubicacion = "Auditorio Principal",
            Descripcion = "Sesion informativa sobre procesos de matricula, calendario academico, becas y tramites estudiantiles."
        },
        new Evento
        {
            Id = 5,
            Titulo = "Seminario de Empleabilidad y CV",
            Categoria = "Academico",
            FechaInicio = DateTime.Today.AddDays(8).AddHours(15),
            FechaFin = DateTime.Today.AddDays(8).AddHours(18),
            Ubicacion = "Sala de Conferencias B",
            Descripcion = "Especialistas en reclutamiento compartiran consejos practicos para mejorar tu perfil profesional y entrevistas."
        }
    };

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View(Eventos.OrderBy(e => e.FechaInicio).ToList());
    }

    [HttpGet("Details/{id:int}")]
    public IActionResult Details(int id)
    {
        var evento = Eventos.FirstOrDefault(e => e.Id == id);
        if (evento is null)
        {
            return NotFound();
        }

        ViewBag.EventosSimilares = Eventos
            .Where(e => e.Id != id && e.Categoria == evento.Categoria)
            .OrderBy(e => e.FechaInicio)
            .Take(3)
            .ToList();

        return View(evento);
    }
}
