using System.ComponentModel.DataAnnotations;

namespace trabfinal.Models;

public class Evento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Ubicacion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }

    // Navegación inversa
    public ICollection<InscripcionEvento> Inscripciones { get; set; } = new List<InscripcionEvento>();
}
