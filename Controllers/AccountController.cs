using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using trabfinal.Data;
using trabfinal.Models;
using Microsoft.EntityFrameworkCore;

namespace trabfinal.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Dashboard");
        return View();
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated) return RedirectToAction("Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(Usuario usuario)
    {
        if (ModelState.IsValid)
        {
            var userExists = await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email);
            if (userExists)
            {
                ModelState.AddModelError("", "El correo ya está en uso.");
                return View(usuario);
            }

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            await IniciarSesionAsync(usuario);
            return RedirectToAction("Dashboard");
        }
        return View(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> LoginPost(string email, string password)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Password == password);

        if (usuario != null)
        {
            await IniciarSesionAsync(usuario);

            return RedirectToAction("Dashboard");
        }

        ViewBag.Error = "Credenciales inválidas.";
        return View("Login");
    }

    [Authorize]
    public IActionResult Dashboard()
    {
        ViewBag.Nombre = User.Identity?.Name;
        ViewBag.UsuarioId = User.FindFirst("UserId")?.Value ?? "1"; 
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        return View(usuario);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(
        string nombre,
        string? carrera,
        string email,
        string? passwordActual,
        string? nuevaPassword,
        string? confirmarPassword)
    {
        var usuario = await ObtenerUsuarioActualAsync();
        if (usuario == null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        nombre = nombre?.Trim() ?? "";
        email = email?.Trim() ?? "";
        carrera = carrera?.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            ModelState.AddModelError("", "El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "El correo es obligatorio.");
        }

        var correoEnUso = await _context.Usuarios.AnyAsync(u => u.Email == email && u.Id != usuario.Id);
        if (correoEnUso)
        {
            ModelState.AddModelError("", "El correo ya esta en uso.");
        }

        var quiereCambiarPassword = !string.IsNullOrWhiteSpace(passwordActual)
            || !string.IsNullOrWhiteSpace(nuevaPassword)
            || !string.IsNullOrWhiteSpace(confirmarPassword);

        if (quiereCambiarPassword)
        {
            if (string.IsNullOrWhiteSpace(passwordActual))
            {
                ModelState.AddModelError("", "Ingresa tu contrasena actual.");
            }
            else if (passwordActual != usuario.Password)
            {
                ModelState.AddModelError("", "La contrasena actual no es correcta.");
            }

            if (string.IsNullOrWhiteSpace(nuevaPassword))
            {
                ModelState.AddModelError("", "Ingresa una nueva contrasena.");
            }

            if (string.IsNullOrWhiteSpace(confirmarPassword))
            {
                ModelState.AddModelError("", "Confirma la nueva contrasena.");
            }

            if (!string.IsNullOrWhiteSpace(nuevaPassword) &&
                !string.IsNullOrWhiteSpace(confirmarPassword) &&
                nuevaPassword != confirmarPassword)
            {
                ModelState.AddModelError("", "La nueva contrasena y la confirmacion no coinciden.");
            }
        }

        if (!ModelState.IsValid)
        {
            usuario.Nombre = nombre;
            usuario.Carrera = carrera;
            usuario.Email = email;
            return View(usuario);
        }

        usuario.Nombre = nombre;
        usuario.Carrera = carrera;
        usuario.Email = email;

        if (quiereCambiarPassword && !string.IsNullOrWhiteSpace(nuevaPassword))
        {
            usuario.Password = nuevaPassword;
        }

        await _context.SaveChangesAsync();
        await IniciarSesionAsync(usuario);
        TempData["PerfilMensaje"] = "Perfil actualizado correctamente.";

        return RedirectToAction("Perfil");
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task IniciarSesionAsync(Usuario usuario)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim("Carrera", usuario.Carrera ?? ""),
            new Claim("UserId", usuario.Id.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
    }

    private async Task<Usuario?> ObtenerUsuarioActualAsync()
    {
        var userId = User.FindFirstValue("UserId");
        if (!int.TryParse(userId, out var id))
        {
            return null;
        }

        return await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
    }
}
