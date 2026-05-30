using Microsoft.AspNetCore.Mvc;
using trabfinal.Data;
using trabfinal.Models;
using trabfinal.Services;
using Microsoft.EntityFrameworkCore;
 
namespace trabfinal.Controllers;
 
/// <summary>
/// API REST para los modelos ML.NET:
///   GET  /api/ml/riesgo/{usuarioId}          → Clasificación de riesgo académico
///   GET  /api/ml/recomendaciones/{usuarioId} → Tips de estudio recomendados
///   POST /api/ml/entrenar                    → Re-entrena ambos modelos
/// </summary>
[ApiController]
[Route("api/ml")]
public class MLController : ControllerBase
{
    private readonly MLService _mlService;
    private readonly AppDbContext _db;
 
    public MLController(MLService mlService, AppDbContext db)
    {
        _mlService = mlService;
        _db = db;
    }
 
    // ──────────────────────────────────────────────
    // GET /api/ml/riesgo/{usuarioId}
    // ──────────────────────────────────────────────
    /// <summary>
    /// Predice el nivel de riesgo académico (Bajo / Medio / Alto) de un estudiante.
    /// </summary>
    [HttpGet("riesgo/{usuarioId}")]
    public async Task<IActionResult> GetRiesgo(int usuarioId)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { mensaje = "Usuario no encontrado" });
 
        var examenes = await _db.ExamenPlanes
            .Where(e => true) // en un sistema real filtrarías por usuarioId
            .ToListAsync();
 
        var eventos = await _db.Eventos.ToListAsync();
 
        var prediccion = _mlService.PredecirRiesgo(examenes, eventos);
 
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
 
    // ──────────────────────────────────────────────
    // GET /api/ml/recomendaciones/{usuarioId}
    // ──────────────────────────────────────────────
    /// <summary>
    /// Retorna los 3 tips de estudio más recomendados para el estudiante.
    /// </summary>
    [HttpGet("recomendaciones/{usuarioId}")]
    public async Task<IActionResult> GetRecomendaciones(int usuarioId, [FromQuery] int cantidad = 3)
    {
        var usuario = await _db.Usuarios.FindAsync(usuarioId);
        if (usuario == null)
            return NotFound(new { mensaje = "Usuario no encontrado" });
 
        var tips = await _db.TipsEstudio.ToListAsync();
        if (!tips.Any())
            return Ok(new { mensaje = "No hay tips registrados aún.", recomendaciones = Array.Empty<object>() });
 
        var usuarios = await _db.Usuarios.ToListAsync();
 
        // Obtener el nivel de riesgo actual para contexto
        var examenes = await _db.ExamenPlanes.ToListAsync();
        var eventos = await _db.Eventos.ToListAsync();
        var riesgo = _mlService.PredecirRiesgo(examenes, eventos);
 
        var recomendaciones = _mlService.RecomendarTips(usuarioId, riesgo.NivelRiesgo, tips, usuarios, cantidad);
 
        return Ok(new
        {
            usuarioId,
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
 
    // ──────────────────────────────────────────────
    // POST /api/ml/entrenar
    // ──────────────────────────────────────────────
    /// <summary>
    /// Re-entrena ambos modelos ML con los datos actuales de la base de datos.
    /// </summary>
    [HttpPost("entrenar")]
    public async Task<IActionResult> Entrenar()
    {
        var examenes = await _db.ExamenPlanes.ToListAsync();
        var eventos  = await _db.Eventos.ToListAsync();
        var tips     = await _db.TipsEstudio.ToListAsync();
        var usuarios = await _db.Usuarios.ToListAsync();
 
        _mlService.EntrenarModeloRiesgo(examenes, eventos);
        _mlService.EntrenarModeloRecomendacion(tips, usuarios);
 
        return Ok(new
        {
            mensaje = "✅ Modelos entrenados correctamente.",
            fechaEntrenamiento = DateTime.Now,
            examenesUsados = examenes.Count,
            tipsUsados = tips.Count
        });
    }
}