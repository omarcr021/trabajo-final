using Microsoft.AspNetCore.Mvc;

namespace trabfinal.Controllers;

public class AccountController : Controller
{
    public IActionResult Login()
    {
        return View();
    }

    public IActionResult Register()
    {
        return View();
    }

    public IActionResult Dashboard()
    {
        ViewBag.Nombre = "Omar";
        return View();
    }

    [HttpPost]
    public IActionResult LoginPost(string email, string password)
    {
        return RedirectToAction("Dashboard");
    }
}