using Microsoft.AspNetCore.Mvc;
using trabfinal.Data;
using trabfinal.Models;
using trabfinal.Services;
using Microsoft.EntityFrameworkCore;

namespace trabfinal.Controllers;

[ApiController]
[Route("api/ml")]
public class MLController : ControllerBase
{
    private readonly MLService _mlService;
    private readonly AppDbContext _db;

    // Tips y Exámenes estáticos de fallback (se usan solo si la DB está vacía)
    private static readonly List<TipEstudio> TipsFallback = new()
    {
        new TipEstudio { Id=1, Titulo="Estudia en bloques de 50 minutos", Categoria="Enfoque", Descripcion="Divide tu estudio en bloques intensos con pausas cortas.", AccionRecomendada="Haz 4 bloques por sesion con descansos de 10 minutos." },
        new TipEstudio { Id=2, Titulo="Resume cada tema en una hoja", Categoria="Comprension", Descripcion="Condensar la informacion mejora la retencion antes del examen.", AccionRecomendada="Prepara una hoja resumen por unidad o capitulo." },
        new TipEstudio { Id=3, Titulo="Practica con preguntas reales", Categoria="Repaso", Descripcion="Resolver ejercicios tipo examen te ayuda a detectar vacios.", AccionRecomendada="Simula al menos un examen por curso cada semana." },
        new TipEstudio { Id=4, Titulo="Explica el tema en voz alta", Categoria="Memoria", Descripcion="Si puedes explicar un tema con claridad, realmente lo entiendes.", AccionRecomendada="Ensaya como si se lo enseñaras a un compañero." },
        new TipEstudio { Id=5, Titulo="Prioriza los cursos mas cercanos", Categoria="Planificacion", Descripcion="Ordena tu semana segun fechas y dificultad.", AccionRecomendada="Empieza por el examen mas proximo o mas complejo." },
        new TipEstudio { Id=6, Titulo="Duerme bien la noche anterior", Categoria="Bienestar", Descripcion="Dormir mejora la consolidacion de la memoria.", AccionRecomendada="Evita trasnochar antes de un parcial." }
    };

    private static readonly List<ExamenPlan> ExamenesFallback = new()
    {
        new ExamenPlan { Id=1, Curso="Programacion", Fecha=DateTime.Today.AddDays(2), Tipo="Parcial", Temas="POO, listas", Prioridad="Alta", EstadoPreparacion="Repasar ejercicios" },
        new ExamenPlan { Id=2, Curso="Base de Datos", Fecha=DateTime.Today.AddDays(4), Tipo="Practica", Temas="SQL, joins", Prioridad="Alta", EstadoPreparacion="Practicar consultas" },
        new ExamenPlan { Id=3, Curso="Matematica Discreta", Fecha=DateTime.Today.AddDays(6), Tipo="Control", Temas="Logica", Prioridad="Media", EstadoPreparacion="Resolver problemas" },
        new ExamenPlan { Id=4, Curso="Arquitectura", Fecha=DateTime.Today.AddDays(9), Tipo="Exposicion", Temas="CPU, memoria", Prioridad="Media", EstadoPreparacion="Preparar diapositivas" }
    };

    public MLController(MLService mlService, AppDbContext db)
    {
        _mlService = mlService;
        _db = db;
    }

    /// <summary>
    /// Obtiene tips y exámenes desde la DB, con fallback a datos estáticos si la DB está vacía.
    /// Si la DB está vacía, hace seed automáticamente.
    /// </summary>
    private async Task<List<TipEstudio>> ObtenerTipsAsync()
    {
        var tips = await _db.TipsEstudio.ToListAsync();
        if (!tips.Any())
        {
            // Seed: insertar tips de fallback en la DB
            _db.TipsEstudio.AddRange(TipsFallback);
            await _db.SaveChangesAsync();
            tips = await _db.TipsEstudio.ToListAsync();
        }
        return tips;
    }

    private async Task<List<ExamenPlan>> ObtenerExamenesAsync()
    {
        var examenes = await _db.ExamenesPlanes.ToListAsync();
        return examenes.Any() ? examenes : ExamenesFallback;
    }

    [HttpGet("riesgo/{usuarioId}")]
    public async Task<IActionResult> GetRiesgo(int usuarioId)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { mensaje = "Usuario no encontrado" });

        var examenes = await ObtenerExamenesAsync();
        var prediccion = _mlService.PredecirRiesgo(examenes, new List<Evento>());

        return Ok(new
        {
            usuarioId,
            nombre = usuario.Nombre,
            nivelRiesgo = prediccion.NivelRiesgo,
            scores = prediccion.Score,
            descripcion = prediccion.NivelRiesgo switch
            {
                "Alto"  => "⚠️ Atención: tienes varios exámenes próximos con baja preparación.",
                "Medio" => "📊 Moderado: puedes mejorar organizando mejor tus tiempos.",
                _       => "✅ Vas bien. Sigue con tu ritmo de estudio actual."
            }
        });
    }

    [HttpGet("recomendaciones/{usuarioId}")]
    public async Task<IActionResult> GetRecomendaciones(int usuarioId, [FromQuery] int cantidad = 3)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { mensaje = "Usuario no encontrado" });

        var tips = await ObtenerTipsAsync();
        var examenes = await ObtenerExamenesAsync();
        var usuarios = await _db.Usuarios.ToListAsync();
        var riesgo = _mlService.PredecirRiesgo(examenes, new List<Evento>());
        var recomendaciones = _mlService.RecomendarTips(usuarioId, riesgo.NivelRiesgo, tips, usuarios, cantidad);

        return Ok(new
        {
            usuarioId,
            nombre = usuario.Nombre,
            nivelRiesgo = riesgo.NivelRiesgo,
            recomendaciones = recomendaciones.Select(r => new
            {
                id = r.Tip.Id,
                titulo = r.Tip.Titulo,
                categoria = r.Tip.Categoria,
                descripcion = r.Tip.Descripcion,
                accionRecomendada = r.Tip.AccionRecomendada,
                scoreRelevancia = Math.Round(r.Score, 2)
            })
        });
    }

    [HttpPost("entrenar")]
    public async Task<IActionResult> Entrenar()
    {
        var tips = await ObtenerTipsAsync();
        var examenes = await ObtenerExamenesAsync();
        var usuarios = await _db.Usuarios.ToListAsync();
        
        _mlService.EntrenarModeloRiesgo(examenes, new List<Evento>());
        _mlService.EntrenarModeloRecomendacion(tips, usuarios);

        return Ok(new
        {
            mensaje = "✅ Modelos entrenados correctamente.",
            fechaEntrenamiento = DateTime.Now,
            tipsUsados = tips.Count,
            usuariosUsados = usuarios.Count
        });
    }
}