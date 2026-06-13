namespace trabfinal.Models;

public class AuditoriaViewModel
{
    public List<SincronizacionLog> Logs { get; set; } = new();
    public int TotalUsuarios { get; set; }
    public int TotalEventos { get; set; }
    public int EventosActivos { get; set; }
    public int TotalLugares { get; set; }
    public int LugaresActivos { get; set; }
    public int TotalRestaurantes { get; set; }
    public int RestaurantesActivos { get; set; }
    public int TotalTips { get; set; }
    public int TotalTareas { get; set; }
    public int TotalExamenes { get; set; }
    public int TotalInscripciones { get; set; }
    public int TotalComentariosLugares { get; set; }
    public int TotalComentariosRestaurantes { get; set; }
}
