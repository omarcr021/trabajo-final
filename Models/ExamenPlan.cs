namespace trabfinal.Models;

public class ExamenPlan
{
    public int Id { get; set; }
    public string Curso { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Temas { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public string EstadoPreparacion { get; set; } = string.Empty;

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
}
