using Microsoft.AspNetCore.Mvc;

namespace trabfinal.Controllers;

[Route("Tareas")]
public class TareasController : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public IActionResult Index()
    {
        return View();
    }
}