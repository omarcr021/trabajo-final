namespace trabfinal.Models;

public class ComentarioLugar
{
    public int Id { get; set; }
    public int LugarId { get; set; }
    public int UsuarioId { get; set; }
    public string Texto { get; set; } = string.Empty;
    public int Calificacion { get; set; }
    public DateTime FechaPublicacion { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }
    public Lugar? Lugar { get; set; }
}
