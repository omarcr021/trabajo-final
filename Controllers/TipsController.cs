using Microsoft.AspNetCore.Mvc;
using trabfinal.Models;

namespace trabfinal.Controllers;

[Route("Tips")]
public class TipsController : Controller
{
    private static readonly List<TipEstudio> Tips = new()
    {
        new TipEstudio
        {
            Id = 1,
            Titulo = "Estudia en bloques de 50 minutos",
            Categoria = "Enfoque",
            Descripcion = "Divide tu estudio en bloques intensos con pausas cortas para mantener concentracion y evitar fatiga mental.",
            AccionRecomendada = "Haz 4 bloques por sesion con descansos de 10 minutos."
        },
        new TipEstudio
        {
            Id = 2,
            Titulo = "Resume cada tema en una hoja",
            Categoria = "Comprension",
            Descripcion = "Condensar la informacion obliga a identificar ideas clave y mejora la retencion antes del examen.",
            AccionRecomendada = "Prepara una hoja resumen por unidad o capitulo."
        },
        new TipEstudio
        {
            Id = 3,
            Titulo = "Practica con preguntas reales",
            Categoria = "Repaso",
            Descripcion = "Resolver ejercicios y preguntas tipo examen te ayuda a detectar vacios antes de la evaluacion.",
            AccionRecomendada = "Simula al menos un examen por curso cada semana."
        },
        new TipEstudio
        {
            Id = 4,
            Titulo = "Explica el tema en voz alta",
            Categoria = "Memoria",
            Descripcion = "Si puedes explicar un tema con claridad, es una buena señal de que realmente lo entiendes.",
            AccionRecomendada = "Ensaya como si se lo enseñaras a un compañero."
        },
        new TipEstudio
        {
            Id = 5,
            Titulo = "Prioriza los cursos mas cercanos",
            Categoria = "Planificacion",
            Descripcion = "No todos los examenes tienen la misma urgencia. Ordena tu semana segun fechas y dificultad.",
            AccionRecomendada = "Empieza siempre por el examen mas proximo o mas complejo."
        },
        new TipEstudio
        {
            Id = 6,
            Titulo = "Duerme bien la noche anterior",
            Categoria = "Bienestar",
            Descripcion = "Dormir mejora la consolidacion de la memoria y tu rendimiento durante la evaluacion.",
            AccionRecomendada = "Evita trasnochar antes de un parcial."
        }
    };

    private static readonly List<ExamenPlan> Examenes = new()
    {
        new ExamenPlan
        {
            Id = 1,
            Curso = "Programacion",
            Fecha = DateTime.Today.AddDays(2).AddHours(9),
            Tipo = "Parcial",
            Temas = "POO, listas, archivos y validaciones.",
            Prioridad = "Alta",
            EstadoPreparacion = "Repasar ejercicios practicos y excepciones."
        },
        new ExamenPlan
        {
            Id = 2,
            Curso = "Base de Datos",
            Fecha = DateTime.Today.AddDays(4).AddHours(11),
            Tipo = "Practica calificada",
            Temas = "Modelo entidad relacion, joins, subconsultas y normalizacion.",
            Prioridad = "Alta",
            EstadoPreparacion = "Practicar consultas SQL y diagramas."
        },
        new ExamenPlan
        {
            Id = 3,
            Curso = "Matematica Discreta",
            Fecha = DateTime.Today.AddDays(6).AddHours(8),
            Tipo = "Control",
            Temas = "Logica proposicional, conjuntos, relaciones y combinatoria.",
            Prioridad = "Media",
            EstadoPreparacion = "Resolver problemas tipo y repasar formulas."
        },
        new ExamenPlan
        {
            Id = 4,
            Curso = "Arquitectura de Computadoras",
            Fecha = DateTime.Today.AddDays(9).AddHours(10),
            Tipo = "Exposicion evaluada",
            Temas = "CPU, memoria, buses y jerarquia de almacenamiento.",
            Prioridad = "Media",
            EstadoPreparacion = "Preparar diapositivas y conceptos clave."
        }
    };

    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        var model = new TipsDashboardViewModel
        {
            Tips = Tips,
            Examenes = Examenes.OrderBy(e => e.Fecha).ToList()
        };

        return View(model);
    }
}
