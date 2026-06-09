namespace trabfinal.Models;

public class RestauranteCercano
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoComida { get; set; } = string.Empty;
    public string Distancia { get; set; } = string.Empty;
    public string DireccionCorta { get; set; } = string.Empty;
    public decimal Calificacion { get; set; }
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;

    // Navegación inversa
    public ICollection<ComentarioRestaurante> Comentarios { get; set; } = new List<ComentarioRestaurante>();
}
