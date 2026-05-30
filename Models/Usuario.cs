namespace trabfinal.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Carrera { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Colección de tareas del usuario
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    public ICollection<InscripcionEvento> InscripcionesEventos { get; set; } = new List<InscripcionEvento>();
    public ICollection<ComentarioLugar> ComentariosLugares { get; set; } = new List<ComentarioLugar>();
    public ICollection<ComentarioRestaurante> ComentariosRestaurantes { get; set; } = new List<ComentarioRestaurante>();
}
