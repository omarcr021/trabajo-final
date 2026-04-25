using Microsoft.AspNetCore.Mvc;

namespace trabfinal.Controllers;

[Route("Tareas")]
public class TareasController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}