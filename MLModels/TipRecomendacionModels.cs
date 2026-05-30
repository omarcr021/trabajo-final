using Microsoft.ML.Data;
 
namespace trabfinal.MLModels;
 
/// <summary>
/// Entrada para el modelo de recomendación de tips de estudio.
/// Representa la interacción usuario-tip (si el tip fue útil para ese perfil).
/// </summary>
public class TipRecomendacionInput
{
    [LoadColumn(0)]
    public float UsuarioId { get; set; }
 
    [LoadColumn(1)]
    public float TipId { get; set; }
 
    [LoadColumn(2)]
    [ColumnName("Label")]
    public float Puntuacion { get; set; } // 0.0 a 5.0 (generada sintéticamente por categoría)
}
 
/// <summary>
/// Resultado de la predicción de recomendación.
/// </summary>
public class TipRecomendacionPrediction
{
    public float Score { get; set; }
}