namespace trabfinal.Models;

public class AdminLugaresViewModel
{
    public List<Lugar> Lugares { get; set; } = new();
    public List<RestauranteCercano> Restaurantes { get; set; } = new();
}
