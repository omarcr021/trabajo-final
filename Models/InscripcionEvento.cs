namespace trabfinal.Models;

public class InscripcionEvento
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    public Usuario? Usuario { get; set; }
}
