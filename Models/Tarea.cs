using System.ComponentModel.DataAnnotations;

namespace trabfinal.Models;

public class Tarea
{
    public int Id { get; set; }

    [Required]
    public string Titulo { get; set; } = string.Empty;

    public string? Materia { get; set; }

    [Required]
    public string Prioridad { get; set; } = "media";

    public bool Completada { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }
}
