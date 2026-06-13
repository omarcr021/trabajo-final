namespace trabfinal.Models;

public class Lugar
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public bool Activo { get; set; } = true;

    // Navegación inversa
    public ICollection<ComentarioLugar> Comentarios { get; set; } = new List<ComentarioLugar>();
}
