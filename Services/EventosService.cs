using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using trabfinal.Data;
using trabfinal.Models;
using Microsoft.Extensions.Configuration;

namespace trabfinal.Services;

public class EventosService : IEventosService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly string? _apiKey;

    public EventosService(HttpClient httpClient, AppDbContext context, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _apiKey = configuration["TicketmasterApiKey"]; // Leerá de User Secrets o appsettings.json
    }

    public async Task SincronizarEventosAsync()
    {
        var log = await _context.SincronizacionLogs.FirstOrDefaultAsync(l => l.Entidad == "Eventos");
        
        // Si existe registro y tiene menos de 1 hora, no hacer nada (Caché vigente)
        // Reducido a 1 hora para evitar que datos locales en app.db interfieran con el despliegue
        if (log != null && (DateTime.Now - log.UltimaSincronizacion).TotalHours < 1)
        {
            return;
        }

        // Si el usuario aún no configura su API Key, insertamos datos simulados de respaldo
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            await InsertarEventosDeRespaldo();
            return;
        }

        try
        {
            // Búsqueda de eventos públicos en Lima vía Ticketmaster Discovery API
            string url = $"https://app.ticketmaster.com/discovery/v2/events.json?city=Lima&apikey={_apiKey}";
            var response = await _httpClient.GetAsync(url);
            
            // Si la API key es inválida o expiró, usamos respaldo
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error con la API de Ticketmaster. Mostrando datos de respaldo.");
                await InsertarEventosDeRespaldo();
                return;
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);
            
            if (!document.RootElement.TryGetProperty("_embedded", out var embeddedElement) || 
                !embeddedElement.TryGetProperty("events", out var eventsArray))
            {
                await InsertarEventosDeRespaldo();
                return;
            }

            var nuevosEventos = new List<Evento>();

            foreach (var element in eventsArray.EnumerateArray().Take(10))
            {
                string titulo = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Evento sin título" : "Evento sin título";
                
                string descripcion = "Evento oficial disponible en Ticketmaster.";
                if (element.TryGetProperty("info", out var infoProp) && infoProp.ValueKind != JsonValueKind.Null)
                {
                    descripcion = infoProp.GetString() ?? descripcion;
                }
                
                // Recortar descripción larga
                if (descripcion.Length > 200) descripcion = descripcion.Substring(0, 197) + "...";

                string fechaInicioStr = "";
                if (element.TryGetProperty("dates", out var datesProp) && datesProp.TryGetProperty("start", out var startProp))
                {
                    string localDate = startProp.TryGetProperty("localDate", out var dProp) ? dProp.GetString() : "";
                    string localTime = startProp.TryGetProperty("localTime", out var tProp) ? tProp.GetString() : "00:00:00";
                    fechaInicioStr = $"{localDate}T{localTime}";
                }

                DateTime fechaInicio = DateTime.Now;
                DateTime.TryParse(fechaInicioStr, out fechaInicio);
                DateTime fechaFin = fechaInicio.AddHours(3); // Ticketmaster raramente expone la fecha de fin de un concierto

                string imagenUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&q=80&w=500";
                if (element.TryGetProperty("images", out var imagesProp) && imagesProp.GetArrayLength() > 0)
                {
                    imagenUrl = imagesProp[0].TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? imagenUrl : imagenUrl;
                }

                string categoriaNombre = "General";
                if (element.TryGetProperty("classifications", out var classProp) && classProp.GetArrayLength() > 0)
                {
                    var firstClass = classProp[0];
                    if (firstClass.TryGetProperty("segment", out var segmentProp))
                    {
                        categoriaNombre = segmentProp.TryGetProperty("name", out var segNameProp) ? segNameProp.GetString() ?? "General" : "General";
                    }
                }

                string ubicacion = "Lima, Perú";
                if (element.TryGetProperty("_embedded", out var embProp) && embProp.TryGetProperty("venues", out var venuesProp) && venuesProp.GetArrayLength() > 0)
                {
                    ubicacion = venuesProp[0].TryGetProperty("name", out var venueNameProp) ? venueNameProp.GetString() ?? ubicacion : ubicacion;
                }

                nuevosEventos.Add(new Evento
                {
                    Titulo = titulo,
                    Categoria = categoriaNombre,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Ubicacion = ubicacion,
                    Descripcion = descripcion,
                    ImagenUrl = imagenUrl
                });
            }

            if (nuevosEventos.Any())
            {
                // Limpiar eventos antiguos antes de guardar los nuevos
                _context.Eventos.RemoveRange(_context.Eventos);

                _context.Eventos.AddRange(nuevosEventos);

                // Actualizar log de sincronizacion
                if (log == null)
                {
                    log = new SincronizacionLog { Entidad = "Eventos", UltimaSincronizacion = DateTime.Now };
                    _context.SincronizacionLogs.Add(log);
                }
                else
                {
                    log.UltimaSincronizacion = DateTime.Now;
                    _context.SincronizacionLogs.Update(log);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                await InsertarEventosDeRespaldo();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al sincronizar eventos desde Ticketmaster: {ex.Message}");
            await InsertarEventosDeRespaldo();
        }
    }

    private async Task InsertarEventosDeRespaldo()
    {
        var log = await _context.SincronizacionLogs.FirstOrDefaultAsync(l => l.Entidad == "Eventos");
        if (log != null && (DateTime.Now - log.UltimaSincronizacion).TotalHours < 24)
        {
            if (await _context.Eventos.AnyAsync()) return;
        }

        // Datos de respaldo en caso la API falle o no haya Key
        var eventosRespaldo = new List<Evento>
        {
            new Evento { Titulo = "Hackathon de Innovación Universitaria", Categoria = "Académico", FechaInicio = DateTime.Today.AddDays(1).AddHours(9), FechaFin = DateTime.Today.AddDays(1).AddHours(18), Ubicacion = "Laboratorio de Cómputo A-204", Descripcion = "Una jornada intensiva para desarrollar soluciones digitales con estudiantes y mentores invitados.", ImagenUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?auto=format&fit=crop&q=80&w=500" },
            new Evento { Titulo = "Feria de Clubes y Vida Estudiantil", Categoria = "Social", FechaInicio = DateTime.Today.AddDays(3).AddHours(11), FechaFin = DateTime.Today.AddDays(3).AddHours(16), Ubicacion = "Plaza Central del Campus", Descripcion = "Descubre talleres, voluntariados, comunidades artísticas y espacios de integración para nuevos estudiantes.", ImagenUrl = "https://images.unsplash.com/photo-1511632765486-a01980e01a18?auto=format&fit=crop&q=80&w=500" },
            new Evento { Titulo = "Torneo Interfacultades de Fútbol", Categoria = "Deportivo", FechaInicio = DateTime.Today.AddDays(5).AddHours(8), FechaFin = DateTime.Today.AddDays(5).AddHours(13), Ubicacion = "Complejo Deportivo Universitario", Descripcion = "Encuentro deportivo entre facultades con actividades de animación, música y puntos de hidratación.", ImagenUrl = "https://images.unsplash.com/photo-1518605368461-1e1e38ce7058?auto=format&fit=crop&q=80&w=500" }
        };

        // Limpiar eventos antiguos antes de guardar los nuevos
        _context.Eventos.RemoveRange(_context.Eventos);
        _context.Eventos.AddRange(eventosRespaldo);

        // Actualizar log de sincronizacion para respaldo
        if (log == null)
        {
            log = new SincronizacionLog { Entidad = "Eventos", UltimaSincronizacion = DateTime.Now };
            _context.SincronizacionLogs.Add(log);
        }
        else
        {
            log.UltimaSincronizacion = DateTime.Now;
            _context.SincronizacionLogs.Update(log);
        }

        await _context.SaveChangesAsync();
    }
}
