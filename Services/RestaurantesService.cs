using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using trabfinal.Data;
using trabfinal.Models;

namespace trabfinal.Services;

public class RestaurantesService : IRestaurantesService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;

    public RestaurantesService(HttpClient httpClient, AppDbContext context)
    {
        _httpClient = httpClient;
        _context = context;
        // Overpass API requiere un User-Agent válido
        if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }
    }

    public async Task SincronizarRestaurantesAsync()
    {
        var log = await _context.SincronizacionLogs.FirstOrDefaultAsync(l => l.Entidad == "Restaurantes");

        // Si existe registro y tiene menos de 1 hora, no hacer nada (Caché vigente)
        // Reducido a 1 hora para evitar que la app.db local subida a github bloquee las requests
        if (log != null && (DateTime.Now - log.UltimaSincronizacion).TotalHours < 1)
        {
            return;
        }

        // Coordenadas de USMP FIA (La Molina)
        string lat = "-12.0735";
        string lon = "-76.9429";
        int radio = 1000; // 1km a la redonda

        // Consulta en Overpass QL para restaurantes, comida rápida y cafés
        string query = $"[out:json];(node[\"amenity\"=\"restaurant\"](around:{radio},{lat},{lon});node[\"amenity\"=\"fast_food\"](around:{radio},{lat},{lon});node[\"amenity\"=\"cafe\"](around:{radio},{lat},{lon}););out center;";
        string url = $"https://lz4.overpass-api.de/api/interpreter?data={Uri.EscapeDataString(query)}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(jsonString);
            var elements = document.RootElement.GetProperty("elements");

            var nuevosRestaurantes = new List<RestauranteCercano>();
            int count = 0;

            foreach (var element in elements.EnumerateArray())
            {
                if (count >= 15) break; // Limitar a 15 locales

                if (element.TryGetProperty("tags", out var tags))
                {
                    string nombre = tags.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Restaurante sin nombre" : "Restaurante sin nombre";
                    
                    if (nombre == "Restaurante sin nombre") continue;

                    string cuisine = tags.TryGetProperty("cuisine", out var cuisineProp) ? cuisineProp.GetString() ?? "Comida Variada" : "Comida Variada";
                    string addr = tags.TryGetProperty("addr:street", out var addrProp) ? addrProp.GetString() ?? "Cerca al campus" : "Cerca al campus";

                    // Formatear la etiqueta cuisine
                    cuisine = cuisine.Replace(";", ", ");
                    // Letra inicial mayúscula
                    if (cuisine.Length > 0)
                        cuisine = char.ToUpper(cuisine[0]) + cuisine.Substring(1);
                    if (cuisine.Length > 30) cuisine = cuisine.Substring(0, 27) + "...";

                    // Generar distancia y calificación (simulado al no tener API de routing ni reviews rica)
                    string distancia = $"{(count + 1) * 2} min";
                    decimal calificacion = 3.5m + (decimal)(new Random().NextDouble() * 1.5);

                    nuevosRestaurantes.Add(new RestauranteCercano
                    {
                        Nombre = nombre,
                        TipoComida = cuisine,
                        Distancia = distancia,
                        DireccionCorta = addr,
                        Calificacion = Math.Round(calificacion, 1),
                        ImagenUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&q=80&w=500" // Imagen genérica por defecto
                    });

                    count++;
                }
            }

            if (nuevosRestaurantes.Any())
            {
                // Limpiar restaurantes antiguos antes de guardar los nuevos
                _context.RestaurantesCercanos.RemoveRange(_context.RestaurantesCercanos);

                _context.RestaurantesCercanos.AddRange(nuevosRestaurantes);

                // Actualizar log de sincronizacion
                if (log == null)
                {
                    log = new SincronizacionLog { Entidad = "Restaurantes", UltimaSincronizacion = DateTime.Now };
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error al sincronizar restaurantes desde Overpass: {ex.Message}");
            await InsertarRestaurantesDeRespaldo();
        }
    }

    private async Task InsertarRestaurantesDeRespaldo()
    {
        var log = await _context.SincronizacionLogs.FirstOrDefaultAsync(l => l.Entidad == "Restaurantes");
        if (log != null && (DateTime.Now - log.UltimaSincronizacion).TotalHours < 24)
        {
            if (await _context.RestaurantesCercanos.AnyAsync()) return;
        }

        var respaldo = new List<RestauranteCercano>
        {
            new RestauranteCercano { Nombre = "Pardos Chicken", TipoComida = "Peruvian", Distancia = "5 min", DireccionCorta = "Av. La Fontana", Calificacion = 4.5m, ImagenUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&q=80&w=500" },
            new RestauranteCercano { Nombre = "Subway", TipoComida = "Sandwich", Distancia = "2 min", DireccionCorta = "Av. Javier Prado", Calificacion = 4.0m, ImagenUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&q=80&w=500" },
            new RestauranteCercano { Nombre = "Chifa El Dorado", TipoComida = "Chinese", Distancia = "8 min", DireccionCorta = "Flora Tristan", Calificacion = 3.8m, ImagenUrl = "https://images.unsplash.com/photo-1517248135467-4c7edcad34c4?auto=format&fit=crop&q=80&w=500" }
        };

        // Limpiar antiguos antes de guardar los nuevos
        _context.RestaurantesCercanos.RemoveRange(_context.RestaurantesCercanos);
        _context.RestaurantesCercanos.AddRange(respaldo);

        // Actualizar log de sincronizacion para respaldo
        if (log == null)
        {
            log = new SincronizacionLog { Entidad = "Restaurantes", UltimaSincronizacion = DateTime.Now };
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
