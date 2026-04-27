namespace trabfinal.Models;

public class ComentarioRestaurante
{
    public int Id { get; set; }
    public int RestauranteId { get; set; }
    public int UsuarioId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Calificacion { get; set; }
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }
}
