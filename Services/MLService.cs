using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using trabfinal.MLModels;
using trabfinal.Models;
 
namespace trabfinal.Services;
 
/// <summary>
/// Servicio ML.NET con dos modelos:
/// 1. Clasificación: nivel de riesgo académico (Bajo/Medio/Alto)
/// 2. Recomendación: tips de estudio personalizados (Matrix Factorization)
/// </summary>
public class MLService
{
    private readonly MLContext _mlContext;
    private ITransformer? _modelRiesgo;
    private ITransformer? _modelRecomendacion;
    private readonly string _modelRiesgoPath;
    private readonly string _modelRecomendacionPath;
 
    public MLService(IWebHostEnvironment env)
    {
        _mlContext = new MLContext(seed: 42);
        _modelRiesgoPath = Path.Combine(env.ContentRootPath, "MLModels", "riesgo_model.zip");
        _modelRecomendacionPath = Path.Combine(env.ContentRootPath, "MLModels", "recomendacion_model.zip");
 
        Directory.CreateDirectory(Path.GetDirectoryName(_modelRiesgoPath)!);
 
        // Cargar modelos si ya existen
        if (File.Exists(_modelRiesgoPath))
            _modelRiesgo = _mlContext.Model.Load(_modelRiesgoPath, out _);
 
        if (File.Exists(_modelRecomendacionPath))
            _modelRecomendacion = _mlContext.Model.Load(_modelRecomendacionPath, out _);
    }
 
    // ─────────────────────────────────────────────
    // MODELO 1: CLASIFICACIÓN DE RIESGO ACADÉMICO
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Entrena el modelo de clasificación con datos generados desde los exámenes y eventos del usuario.
    /// </summary>
    public void EntrenarModeloRiesgo(List<ExamenPlan> examenes, List<Evento> eventos)
    {
        var datos = GenerarDatosEntrenamientoRiesgo(examenes, eventos);
 
        if (datos.Count < 5)
            datos = GenerarDatosSinteticosRiesgo(); // fallback con datos de ejemplo
 
        var dataView = _mlContext.Data.LoadFromEnumerable(datos);
 
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label")
            .Append(_mlContext.Transforms.Concatenate("Features",
                nameof(RiesgoEstudianteInput.TotalExamenes),
                nameof(RiesgoEstudianteInput.ExamenesAlaPrioridad),
                nameof(RiesgoEstudianteInput.PorcentajePreparado),
                nameof(RiesgoEstudianteInput.TotalEventos),
                nameof(RiesgoEstudianteInput.DiasProximoExamen)))
            .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
 
        _modelRiesgo = pipeline.Fit(dataView);
        _mlContext.Model.Save(_modelRiesgo, dataView.Schema, _modelRiesgoPath);
    }
 
    /// <summary>
    /// Predice el nivel de riesgo académico de un estudiante.
    /// </summary>
    public RiesgoEstudiantePrediction PredecirRiesgo(
        List<ExamenPlan> examenes, List<Evento> eventos)
    {
        if (_modelRiesgo == null)
            EntrenarModeloRiesgo(examenes, eventos);
 
        var input = ExtraerFeaturesRiesgo(examenes, eventos);
        var engine = _mlContext.Model
            .CreatePredictionEngine<RiesgoEstudianteInput, RiesgoEstudiantePrediction>(_modelRiesgo!);
 
        return engine.Predict(input);
    }
 
    private RiesgoEstudianteInput ExtraerFeaturesRiesgo(
        List<ExamenPlan> examenes, List<Evento> eventos)
    {
        int total = examenes.Count;
        int altaPrioridad = examenes.Count(e => e.Prioridad == "Alta");
        int preparados = examenes.Count(e => e.EstadoPreparacion == "Preparado");
        float pctPreparado = total > 0 ? (float)preparados / total * 100 : 0;
 
        var hoy = DateTime.Today;
        var proxExamen = examenes
            .Where(e => e.Fecha >= hoy)
            .OrderBy(e => e.Fecha)
            .FirstOrDefault();
        float diasProximo = proxExamen != null ? (float)(proxExamen.Fecha - hoy).TotalDays : 30;
 
        return new RiesgoEstudianteInput
        {
            TotalExamenes = total,
            ExamenesAlaPrioridad = altaPrioridad,
            PorcentajePreparado = pctPreparado,
            TotalEventos = eventos.Count,
            DiasProximoExamen = diasProximo
        };
    }
 
    private List<RiesgoEstudianteInput> GenerarDatosEntrenamientoRiesgo(
        List<ExamenPlan> examenes, List<Evento> eventos)
    {
        // Generar variaciones del estudiante actual para entrenamiento
        var datos = new List<RiesgoEstudianteInput>();
        var baseInput = ExtraerFeaturesRiesgo(examenes, eventos);
 
        // Variaciones con etiquetas lógicas
        datos.Add(new RiesgoEstudianteInput { TotalExamenes = 5, ExamenesAlaPrioridad = 0, PorcentajePreparado = 90, TotalEventos = 8, DiasProximoExamen = 14, NivelRiesgo = "Bajo" });
        datos.Add(new RiesgoEstudianteInput { TotalExamenes = 4, ExamenesAlaPrioridad = 1, PorcentajePreparado = 70, TotalEventos = 5, DiasProximoExamen = 7, NivelRiesgo = "Medio" });
        datos.Add(new RiesgoEstudianteInput { TotalExamenes = 6, ExamenesAlaPrioridad = 3, PorcentajePreparado = 20, TotalEventos = 2, DiasProximoExamen = 2, NivelRiesgo = "Alto" });
 
        // Agregar el perfil real con etiqueta derivada
        string etiqueta = baseInput.PorcentajePreparado >= 70 ? "Bajo"
                        : baseInput.PorcentajePreparado >= 40 ? "Medio" : "Alto";
        baseInput.NivelRiesgo = etiqueta;
        datos.Add(baseInput);
 
        return datos;
    }
 
    private List<RiesgoEstudianteInput> GenerarDatosSinteticosRiesgo()
    {
        return new List<RiesgoEstudianteInput>
        {
            new() { TotalExamenes=5, ExamenesAlaPrioridad=0, PorcentajePreparado=95, TotalEventos=10, DiasProximoExamen=20, NivelRiesgo="Bajo" },
            new() { TotalExamenes=4, ExamenesAlaPrioridad=1, PorcentajePreparado=80, TotalEventos=7,  DiasProximoExamen=12, NivelRiesgo="Bajo" },
            new() { TotalExamenes=3, ExamenesAlaPrioridad=1, PorcentajePreparado=60, TotalEventos=5,  DiasProximoExamen=6,  NivelRiesgo="Medio" },
            new() { TotalExamenes=5, ExamenesAlaPrioridad=2, PorcentajePreparado=50, TotalEventos=4,  DiasProximoExamen=4,  NivelRiesgo="Medio" },
            new() { TotalExamenes=6, ExamenesAlaPrioridad=4, PorcentajePreparado=20, TotalEventos=1,  DiasProximoExamen=1,  NivelRiesgo="Alto" },
            new() { TotalExamenes=7, ExamenesAlaPrioridad=5, PorcentajePreparado=10, TotalEventos=0,  DiasProximoExamen=1,  NivelRiesgo="Alto" },
            new() { TotalExamenes=3, ExamenesAlaPrioridad=2, PorcentajePreparado=30, TotalEventos=2,  DiasProximoExamen=3,  NivelRiesgo="Alto" },
            new() { TotalExamenes=4, ExamenesAlaPrioridad=0, PorcentajePreparado=85, TotalEventos=6,  DiasProximoExamen=15, NivelRiesgo="Bajo" },
            new() { TotalExamenes=5, ExamenesAlaPrioridad=1, PorcentajePreparado=55, TotalEventos=3,  DiasProximoExamen=5,  NivelRiesgo="Medio" },
            new() { TotalExamenes=6, ExamenesAlaPrioridad=3, PorcentajePreparado=25, TotalEventos=1,  DiasProximoExamen=2,  NivelRiesgo="Alto" },
        };
    }
 
    // ─────────────────────────────────────────────
    // MODELO 2: RECOMENDACIÓN DE TIPS
    // ─────────────────────────────────────────────
 
    /// <summary>
    /// Entrena el modelo de recomendación con interacciones usuario-tip.
    /// </summary>
    public void EntrenarModeloRecomendacion(List<TipEstudio> tips, List<Usuario> usuarios)
    {
        var datos = GenerarInteraccionesRecomendacion(tips, usuarios);
        var dataView = _mlContext.Data.LoadFromEnumerable(datos);
 
        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "UsuarioIdEncoded",
            MatrixRowIndexColumnName = "TipIdEncoded",
            LabelColumnName = "Label",
            NumberOfIterations = 20,
            ApproximationRank = 8,
        };
 
        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey("UsuarioIdEncoded", nameof(TipRecomendacionInput.UsuarioId))
            .Append(_mlContext.Transforms.Conversion
                .MapValueToKey("TipIdEncoded", nameof(TipRecomendacionInput.TipId)))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(options));
 
        _modelRecomendacion = pipeline.Fit(dataView);
        _mlContext.Model.Save(_modelRecomendacion, dataView.Schema, _modelRecomendacionPath);
    }
 
    /// <summary>
    /// Recomienda los N tips más relevantes para un usuario dado su nivel de riesgo.
    /// </summary>
    public List<(TipEstudio Tip, float Score)> RecomendarTips(
        int usuarioId, string nivelRiesgo, List<TipEstudio> todosLosTips,
        List<Usuario> usuarios, int cantidad = 3)
    {
        if (_modelRecomendacion == null)
            EntrenarModeloRecomendacion(todosLosTips, usuarios);
 
        var engine = _mlContext.Model
            .CreatePredictionEngine<TipRecomendacionInput, TipRecomendacionPrediction>(_modelRecomendacion!);
 
        var resultados = todosLosTips
            .Select(tip =>
            {
                var pred = engine.Predict(new TipRecomendacionInput
                {
                    UsuarioId = usuarioId,
                    TipId = tip.Id
                });
                // Sanitizar scores: NaN/Infinity ocurren cuando el usuario
                // no estaba en los datos de entrenamiento del modelo
                var safeScore = float.IsNaN(pred.Score) || float.IsInfinity(pred.Score)
                    ? 0f
                    : pred.Score;
                return (Tip: tip, Score: safeScore);
            })
            .OrderByDescending(x => x.Score)
            .Take(cantidad)
            .ToList();
 
        return resultados;
    }
 
    private List<TipRecomendacionInput> GenerarInteraccionesRecomendacion(
        List<TipEstudio> tips, List<Usuario> usuarios)
    {
        var datos = new List<TipRecomendacionInput>();
        var rng = new Random(42);
 
        // Mapeo de categorías a nivel de riesgo para puntuar mejor
        var categoriaRiesgo = new Dictionary<string, string>
        {
            { "Organización", "Alto" },
            { "Concentración", "Alto" },
            { "Memoria", "Medio" },
            { "Motivación", "Medio" },
            { "Lectura", "Bajo" },
            { "Repaso", "Bajo" },
        };
 
        foreach (var usuario in usuarios)
        {
            foreach (var tip in tips)
            {
                // Generar puntuación según afinidad categoría-usuario
                float puntuacion = 2.5f + (float)(rng.NextDouble() * 1.5);
 
                // Si la categoría coincide con el perfil simulado, subir puntuación
                bool esRelevante = categoriaRiesgo.TryGetValue(tip.Categoria, out var _);
                if (esRelevante) puntuacion += (float)(rng.NextDouble() * 1.5);
 
                datos.Add(new TipRecomendacionInput
                {
                    UsuarioId = usuario.Id,
                    TipId = tip.Id,
                    Puntuacion = Math.Min(puntuacion, 5.0f)
                });
            }
        }
 
        // Datos sintéticos extra si hay pocos usuarios
        if (usuarios.Count < 3)
        {
            for (int u = 100; u <= 110; u++)
                foreach (var tip in tips)
                    datos.Add(new TipRecomendacionInput
                    {
                        UsuarioId = u,
                        TipId = tip.Id,
                        Puntuacion = 2f + (float)(rng.NextDouble() * 3f)
                    });
        }
 
        return datos;
    }
}