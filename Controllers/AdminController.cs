using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using trabfinal.Data;
using trabfinal.Models;
using ClosedXML.Excel;
using System.Text;

namespace trabfinal.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────
    // DASHBOARD
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsuarios = await _context.Usuarios.CountAsync();
        ViewBag.TotalEventos = await _context.Eventos.CountAsync();
        ViewBag.TotalLugares = await _context.Lugares.CountAsync();
        ViewBag.TotalTareas = await _context.Tareas.CountAsync();
        ViewBag.TotalTips = await _context.TipsEstudio.CountAsync();
        
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ChartData()
    {
        // Usuarios por mes (últimos 6 meses)
        var hace6Meses = DateTime.UtcNow.AddMonths(-6);
        var usuariosPorMes = await _context.Usuarios
            .Where(u => u.FechaCreacion >= hace6Meses)
            .ToListAsync();

        var mesesAgrupados = usuariosPorMes
            .GroupBy(u => new { u.FechaCreacion.Year, u.FechaCreacion.Month })
            .Select(g => new
            {
                Mes = $"{g.Key.Year}-{g.Key.Month:D2}",
                Cantidad = g.Count()
            })
            .OrderBy(x => x.Mes)
            .ToList();

        // Si no hay datos en los últimos 6 meses, generar meses vacíos
        if (!mesesAgrupados.Any())
        {
            for (int i = 5; i >= 0; i--)
            {
                var fecha = DateTime.UtcNow.AddMonths(-i);
                mesesAgrupados.Add(new { Mes = $"{fecha.Year}-{fecha.Month:D2}", Cantidad = 0 });
            }
            // Poner todos los usuarios en el mes actual
            var totalActual = await _context.Usuarios.CountAsync();
            if (mesesAgrupados.Any())
            {
                var ultimoMes = mesesAgrupados.Last();
                mesesAgrupados[mesesAgrupados.Count - 1] = new { Mes = ultimoMes.Mes, Cantidad = totalActual };
            }
        }

        // Distribución de carreras
        var distribucionCarreras = await _context.Usuarios
            .GroupBy(u => u.Carrera ?? "Sin carrera")
            .Select(g => new
            {
                Carrera = g.Key,
                Cantidad = g.Count()
            })
            .OrderByDescending(x => x.Cantidad)
            .Take(10)
            .ToListAsync();

        return Json(new
        {
            usuariosPorMes = mesesAgrupados,
            distribucionCarreras
        });
    }

    // ─────────────────────────────────────────────
    // GESTIÓN DE USUARIOS
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await _context.Usuarios.OrderByDescending(u => u.FechaCreacion).ToListAsync();
        return View(usuarios);
    }

    [HttpGet]
    public async Task<IActionResult> EditarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();
        return View(usuario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarUsuario(int id, string nombre, string email, string? carrera, string rol)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Nombre y Email son obligatorios.");
            return View(usuario);
        }

        usuario.Nombre = nombre;
        usuario.Email = email;
        usuario.Carrera = carrera;
        usuario.Rol = rol;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario != null)
        {
            _context.Usuarios.Remove(usuario);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Usuarios));
    }

    // ─────────────────────────────────────────────
    // EXPORTACIÓN EXCEL / CSV
    // ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> ExportarUsuariosExcel()
    {
        var usuarios = await _context.Usuarios.OrderBy(u => u.Id).ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Usuarios");

        // Encabezados
        var headers = new[] { "ID", "Nombre", "Email", "Carrera", "Rol", "Fecha Registro" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.DarkBlue;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Datos
        for (int row = 0; row < usuarios.Count; row++)
        {
            var u = usuarios[row];
            worksheet.Cell(row + 2, 1).Value = u.Id;
            worksheet.Cell(row + 2, 2).Value = u.Nombre;
            worksheet.Cell(row + 2, 3).Value = u.Email;
            worksheet.Cell(row + 2, 4).Value = u.Carrera ?? "—";
            worksheet.Cell(row + 2, 5).Value = u.Rol;
            worksheet.Cell(row + 2, 6).Value = u.FechaCreacion.ToString("dd/MM/yyyy");
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Usuarios_CampusGo_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarUsuariosCsv()
    {
        var usuarios = await _context.Usuarios.OrderBy(u => u.Id).ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ID,Nombre,Email,Carrera,Rol,FechaRegistro");

        foreach (var u in usuarios)
        {
            sb.AppendLine($"{u.Id},\"{u.Nombre}\",\"{u.Email}\",\"{u.Carrera ?? ""}\",\"{u.Rol}\",\"{u.FechaCreacion:dd/MM/yyyy}\"");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"Usuarios_CampusGo_{DateTime.Now:yyyyMMdd}.csv");
    }

    // ─────────────────────────────────────────────
    // GESTIÓN DE TIPS DE ESTUDIO
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Tips()
    {
        var tips = await _context.TipsEstudio.ToListAsync();
        return View(tips);
    }

    [HttpGet]
    public IActionResult CrearTip()
    {
        return View(new TipEstudio());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearTip(TipEstudio tip)
    {
        if (ModelState.IsValid)
        {
            _context.TipsEstudio.Add(tip);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Tips));
        }
        return View(tip);
    }

    [HttpGet]
    public async Task<IActionResult> EditarTip(int id)
    {
        var tip = await _context.TipsEstudio.FindAsync(id);
        if (tip == null) return NotFound();
        return View(tip);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarTip(int id, TipEstudio tipModel)
    {
        if (id != tipModel.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(tipModel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TipEstudioExists(tipModel.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Tips));
        }
        return View(tipModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarTip(int id)
    {
        var tip = await _context.TipsEstudio.FindAsync(id);
        if (tip != null)
        {
            _context.TipsEstudio.Remove(tip);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Tips));
    }

    // ─────────────────────────────────────────────
    // GESTIÓN DE EVENTOS
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Eventos()
    {
        var eventos = await _context.Eventos.OrderByDescending(e => e.FechaInicio).ToListAsync();
        return View(eventos);
    }

    [HttpGet]
    public IActionResult CrearEvento()
    {
        return View(new Evento { FechaInicio = DateTime.Now, FechaFin = DateTime.Now.AddHours(3) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearEvento(Evento evento)
    {
        if (ModelState.IsValid)
        {
            evento.Activo = true;
            _context.Eventos.Add(evento);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Eventos));
        }
        return View(evento);
    }

    [HttpGet]
    public async Task<IActionResult> EditarEvento(int id)
    {
        var evento = await _context.Eventos.FindAsync(id);
        if (evento == null) return NotFound();
        return View(evento);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarEvento(int id, Evento eventoModel)
    {
        if (id != eventoModel.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(eventoModel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Eventos.AnyAsync(e => e.Id == eventoModel.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Eventos));
        }
        return View(eventoModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarEvento(int id)
    {
        var evento = await _context.Eventos.FindAsync(id);
        if (evento != null)
        {
            _context.Eventos.Remove(evento);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Eventos));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleEventoActivo(int id)
    {
        var evento = await _context.Eventos.FindAsync(id);
        if (evento != null)
        {
            evento.Activo = !evento.Activo;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Eventos));
    }

    // ─────────────────────────────────────────────
    // GESTIÓN DE LUGARES
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Lugares()
    {
        var model = new AdminLugaresViewModel
        {
            Lugares = await _context.Lugares.OrderBy(l => l.Nombre).ToListAsync(),
            Restaurantes = await _context.RestaurantesCercanos.OrderBy(r => r.Nombre).ToListAsync()
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult CrearLugar()
    {
        return View(new Lugar());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearLugar(Lugar lugar)
    {
        if (ModelState.IsValid)
        {
            lugar.Activo = true;
            _context.Lugares.Add(lugar);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Lugares));
        }
        return View(lugar);
    }

    [HttpGet]
    public async Task<IActionResult> EditarLugar(int id)
    {
        var lugar = await _context.Lugares.FindAsync(id);
        if (lugar == null) return NotFound();
        return View(lugar);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarLugar(int id, Lugar lugarModel)
    {
        if (id != lugarModel.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(lugarModel);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Lugares.AnyAsync(l => l.Id == lugarModel.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Lugares));
        }
        return View(lugarModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarLugar(int id)
    {
        var lugar = await _context.Lugares.FindAsync(id);
        if (lugar != null)
        {
            _context.Lugares.Remove(lugar);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Lugares));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLugarActivo(int id)
    {
        var lugar = await _context.Lugares.FindAsync(id);
        if (lugar != null)
        {
            lugar.Activo = !lugar.Activo;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Lugares));
    }

    // ─────────────────────────────────────────────
    // GESTIÓN DE RESTAURANTES (solo toggle visibilidad)
    // ─────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> ToggleRestauranteActivo(int id)
    {
        var restaurante = await _context.RestaurantesCercanos.FindAsync(id);
        if (restaurante != null)
        {
            restaurante.Activo = !restaurante.Activo;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Lugares));
    }

    // ─────────────────────────────────────────────
    // AUDITORÍA Y LOGS
    // ─────────────────────────────────────────────

    public async Task<IActionResult> Auditoria()
    {
        var model = new AuditoriaViewModel
        {
            Logs = await _context.SincronizacionLogs.OrderByDescending(l => l.UltimaSincronizacion).ToListAsync(),
            TotalUsuarios = await _context.Usuarios.CountAsync(),
            TotalEventos = await _context.Eventos.CountAsync(),
            EventosActivos = await _context.Eventos.CountAsync(e => e.Activo),
            TotalLugares = await _context.Lugares.CountAsync(),
            LugaresActivos = await _context.Lugares.CountAsync(l => l.Activo),
            TotalRestaurantes = await _context.RestaurantesCercanos.CountAsync(),
            RestaurantesActivos = await _context.RestaurantesCercanos.CountAsync(r => r.Activo),
            TotalTips = await _context.TipsEstudio.CountAsync(),
            TotalTareas = await _context.Tareas.CountAsync(),
            TotalExamenes = await _context.ExamenesPlanes.CountAsync(),
            TotalInscripciones = await _context.InscripcionesEventos.CountAsync(),
            TotalComentariosLugares = await _context.ComentariosLugares.CountAsync(),
            TotalComentariosRestaurantes = await _context.ComentariosRestaurantes.CountAsync()
        };
        return View(model);
    }

    // ─────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────

    private bool TipEstudioExists(int id)
    {
        return _context.TipsEstudio.Any(e => e.Id == id);
    }
}
