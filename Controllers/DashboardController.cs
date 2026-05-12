using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await _dashboardService.GetAdminDashboardAsync();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var vm = await _dashboardService.GetPerfilAsync(GetUsuarioId());

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(ClientePerfilViewModel model)
    {
        ModelState.Remove(nameof(model.Foto));

        if (!ModelState.IsValid)
            return View(model);

        var foto = model.Foto ?? Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");
        var result = await _dashboardService.UpdatePerfilAsync(GetUsuarioId(), model, foto);

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
