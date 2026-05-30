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
        // DESCOMENTADO LUEGO DE FORZAR LA CREACIÓN DE CATEGORIAS
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
            var nuevosEventos = new List<Evento>();
            var clasificaciones = new[] { "Music", "Sports", "Arts & Theatre", "Miscellaneous", "Film" };

            // Realizamos varias llamadas por categoría y recorremos la paginación de Ticketmaster.
            foreach (var clasificacion in clasificaciones)
            {
                var page = 0;
                var totalPages = 1;

                while (page < totalPages)
                {
                    string url = $"https://app.ticketmaster.com/discovery/v2/events.json?countryCode=PE&classificationName={Uri.EscapeDataString(clasificacion)}&size=50&page={page}&apikey={_apiKey}";
                    var response = await _httpClient.GetAsync(url);

                    if (!response.IsSuccessStatusCode)
                    {
                        break; // Pasamos a la siguiente clasificación
                    }

                    var jsonString = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(jsonString);

                    if (document.RootElement.TryGetProperty("page", out var pageElement) &&
                        pageElement.TryGetProperty("totalPages", out var totalPagesProp) &&
                        totalPagesProp.TryGetInt32(out var totalPagesValue) && totalPagesValue > 0)
                    {
                        totalPages = totalPagesValue;
                    }

                    if (document.RootElement.TryGetProperty("_embedded", out var embeddedElement) &&
                        embeddedElement.TryGetProperty("events", out var eventsArray))
                    {
                        foreach (var element in eventsArray.EnumerateArray())
                        {
                            string titulo = element.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Evento sin título" : "Evento sin título";

                            string descripcion = "Evento oficial disponible en Ticketmaster.";
                            if (element.TryGetProperty("info", out var infoProp) && infoProp.ValueKind != JsonValueKind.Null)
                            {
                                descripcion = infoProp.GetString() ?? descripcion;
                            }

                            if (descripcion.Length > 200) descripcion = descripcion.Substring(0, 197) + "...";

                            string fechaInicioStr = "";
                            if (element.TryGetProperty("dates", out var datesProp) && datesProp.TryGetProperty("start", out var startProp))
                            {
                                string? localDate = startProp.TryGetProperty("localDate", out var dProp) ? dProp.GetString() : "";
                                string? localTime = startProp.TryGetProperty("localTime", out var tProp) ? tProp.GetString() : "00:00:00";
                                fechaInicioStr = $"{localDate}T{localTime}";
                            }

                            DateTime fechaInicio = DateTime.Now;
                            DateTime.TryParse(fechaInicioStr, out fechaInicio);
                            DateTime fechaFin = fechaInicio.AddHours(3);

                            string imagenUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&q=80&w=500";
                            if (element.TryGetProperty("images", out var imagesProp) && imagesProp.GetArrayLength() > 0)
                            {
                                imagenUrl = imagesProp[0].TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? imagenUrl : imagenUrl;
                            }

                            string categoriaNombre = "Entretenimiento";
                            if (element.TryGetProperty("classifications", out var classProp) && classProp.ValueKind == JsonValueKind.Array && classProp.GetArrayLength() > 0)
                            {
                                var firstClass = classProp[0];
                                if (firstClass.TryGetProperty("segment", out var segmentProp) && segmentProp.ValueKind == JsonValueKind.Object)
                                {
                                    var ticketmasterSegment = segmentProp.TryGetProperty("name", out var segNameProp) ? segNameProp.GetString() : "General";
                                    categoriaNombre = ticketmasterSegment switch
                                    {
                                        "Music" => "Conciertos",
                                        "Sports" => "Deportes",
                                        "Arts & Theatre" => "Teatros",
                                        "Film" => "Culturales",
                                        "Miscellaneous" => "Entretenimiento",
                                        "Family" => "Circos e Infantiles",
                                        "Circus" => "Circos e Infantiles",
                                        _ => "Entretenimiento"
                                    };
                                }
                            }

                            string ubicacion = "Lima, Perú";
                            if (element.TryGetProperty("_embedded", out var embProp) && embProp.TryGetProperty("venues", out var venuesProp) && venuesProp.GetArrayLength() > 0)
                            {
                                ubicacion = venuesProp[0].TryGetProperty("name", out var venueNameProp) ? venueNameProp.GetString() ?? ubicacion : ubicacion;
                            }

                            // Evitar duplicados
                            if (!nuevosEventos.Any(e => e.Titulo == titulo && e.FechaInicio == fechaInicio))
                            {
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
                        }
                    }

                    page++;
                }
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
            new Evento { Titulo = "Hackathon de Innovación Universitaria", Categoria = "Culturales", FechaInicio = DateTime.Today.AddDays(1).AddHours(9), FechaFin = DateTime.Today.AddDays(1).AddHours(18), Ubicacion = "Laboratorio de Cómputo A-204", Descripcion = "Una jornada intensiva para desarrollar soluciones digitales con estudiantes y mentores invitados.", ImagenUrl = "https://images.unsplash.com/photo-1504384308090-c894fdcc538d?auto=format&fit=crop&q=80&w=500" },
            new Evento { Titulo = "Feria de Clubes y Vida Estudiantil", Categoria = "Entretenimiento", FechaInicio = DateTime.Today.AddDays(3).AddHours(11), FechaFin = DateTime.Today.AddDays(3).AddHours(16), Ubicacion = "Plaza Central del Campus", Descripcion = "Descubre talleres, voluntariados, comunidades artísticas y espacios de integración para nuevos estudiantes.", ImagenUrl = "https://images.unsplash.com/photo-1511632765486-a01980e01a18?auto=format&fit=crop&q=80&w=500" },
            new Evento { Titulo = "Torneo Interfacultades de Fútbol", Categoria = "Deportes", FechaInicio = DateTime.Today.AddDays(5).AddHours(8), FechaFin = DateTime.Today.AddDays(5).AddHours(13), Ubicacion = "Complejo Deportivo Universitario", Descripcion = "Encuentro deportivo entre facultades con actividades de animación, música y puntos de hidratación.", ImagenUrl = "https://images.unsplash.com/photo-1518605368461-1e1e38ce7058?auto=format&fit=crop&q=80&w=500" }
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
