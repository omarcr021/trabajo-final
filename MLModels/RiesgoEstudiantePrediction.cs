using Microsoft.ML.Data;
 
namespace trabfinal.MLModels;
 
/// <summary>
/// Resultado de la predicción del nivel de riesgo académico.
/// </summary>
public class RiesgoEstudiantePrediction
{
    [ColumnName("PredictedLabel")]
    public string NivelRiesgo { get; set; } = string.Empty;
 
    public float[] Score { get; set; } = Array.Empty<float>();
}