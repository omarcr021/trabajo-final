using Microsoft.ML.Data;

namespace trabfinal.MLModels;

/// <summary>
/// Datos de entrada para clasificar el nivel de riesgo académico de un estudiante.
/// </summary>
public class RiesgoEstudianteInput
{
    [LoadColumn(0)]
    public float TotalExamenes { get; set; }

    [LoadColumn(1)]
    public float ExamenesAlaPrioridad { get; set; }  // cuántos tienen prioridad Alta

    [LoadColumn(2)]
    public float PorcentajePreparado { get; set; }   // % con EstadoPreparacion = "Preparado"

    [LoadColumn(3)]
    public float TotalEventos { get; set; }

    [LoadColumn(4)]
    public float DiasProximoExamen { get; set; }     // días hasta el próximo examen

    [LoadColumn(5)]
    [ColumnName("Label")]
    public string NivelRiesgo { get; set; } = string.Empty; // "Bajo", "Medio", "Alto"
}