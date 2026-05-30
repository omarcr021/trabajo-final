using System;

namespace trabfinal.Models;

public class SincronizacionLog
{
    public int Id { get; set; }
    public string Entidad { get; set; } = string.Empty;
    public DateTime UltimaSincronizacion { get; set; }
}
