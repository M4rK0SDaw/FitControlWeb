using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Entrenador")]
public class EntrenadorDashboardController : Controller
{
    private readonly IEntrenadorDashboardService _entrenadorDashboardService;

    public EntrenadorDashboardController(IEntrenadorDashboardService entrenadorDashboardService)
    {
        _entrenadorDashboardService = entrenadorDashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _entrenadorDashboardService.GetDashboardAsync(GetUsuarioId());

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var vm = await _entrenadorDashboardService.GetPerfilAsync(GetUsuarioId());

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(EntrenadorPerfilViewModel model)
    {
        ModelState.Remove(nameof(model.Foto));

        if (!ModelState.IsValid)
            return View(model);

        var foto = model.Foto ?? Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");
        var result = await _entrenadorDashboardService.UpdatePerfilAsync(GetUsuarioId(), model, foto);

        if (result.Code == "FORBID")
            return Forbid();

        if (result.Code == "NOT_FOUND")
            return NotFound();

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Perfil));
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
