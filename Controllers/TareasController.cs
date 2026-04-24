using Microsoft.AspNetCore.Mvc;

namespace trabfinal.Controllers;

public class TareasController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}