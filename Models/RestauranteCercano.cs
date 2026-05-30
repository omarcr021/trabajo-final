namespace trabfinal.Models;

public class RestauranteCercano
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoComida { get; set; } = string.Empty;
    public string Distancia { get; set; } = string.Empty;
    public string DireccionCorta { get; set; } = string.Empty;
    public decimal Calificacion { get; set; }
}
