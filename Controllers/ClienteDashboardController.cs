using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Cliente")]
public class ClienteDashboardController : Controller
{
    private readonly IClienteDashboardService _clienteDashboardService;

    public ClienteDashboardController(IClienteDashboardService clienteDashboardService)
    {
        _clienteDashboardService = clienteDashboardService;
    }

    public async Task<IActionResult> Index()
    {
        var usuarioId = GetUsuarioId();
        var vm = await _clienteDashboardService.GetDashboardAsync(usuarioId);

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Perfil()
    {
        var vm = await _clienteDashboardService.GetPerfilAsync(GetUsuarioId());

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
        var result = await _clienteDashboardService.UpdatePerfilAsync(GetUsuarioId(), model, foto);

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

    public async Task<IActionResult> MisFacturas(
        bool? pagada,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var result = await _clienteDashboardService.GetMisFacturasAsync(
            GetUsuarioId(),
            pagada,
            page,
            pageSize);

        ViewBag.Pagada = pagada;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)result.TotalItems / pageSize);
        ViewBag.TotalFacturas = result.TotalItems;
        ViewBag.TotalPendiente = result.TotalPendiente;
        ViewBag.FacturasPendientes = result.FacturasPendientes;
        ViewBag.FacturasPagadas = result.FacturasPagadas;

        return View(result.Facturas);
    }

    [HttpGet]
    public async Task<IActionResult> ContratarSuscripcion()
    {
        var vm = await _clienteDashboardService.GetContratarSuscripcionAsync(GetUsuarioId());
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ContratarSuscripcion(ClienteContratarSuscripcionViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vm = await _clienteDashboardService.GetContratarSuscripcionAsync(GetUsuarioId());
            vm.TipoSuscripcionId = model.TipoSuscripcionId;
            return View(vm);
        }

        var successUrl = Url.Action(nameof(ContratarSuscripcionSuccess), "ClienteDashboard", null, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(ContratarSuscripcionCancel), "ClienteDashboard", null, Request.Scheme)!;

        var result = await _clienteDashboardService.CrearCheckoutSuscripcionAsync(
            GetUsuarioId(),
            model.TipoSuscripcionId,
            successUrl,
            cancelUrl);

        if (!result.Success || string.IsNullOrWhiteSpace(result.Data))
        {
            TempData["Error"] = result.Message;
            var vm = await _clienteDashboardService.GetContratarSuscripcionAsync(GetUsuarioId());
            vm.TipoSuscripcionId = model.TipoSuscripcionId;
            return View(vm);
        }

        return Redirect(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> ContratarSuscripcionSuccess(string session_id)
    {
        var result = await _clienteDashboardService.ConfirmarCheckoutSuscripcionAsync(GetUsuarioId(), session_id);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(ContratarSuscripcion));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction("Details", "Facturas", new { id = result.Data });
    }

    [HttpGet]
    public IActionResult ContratarSuscripcionCancel()
    {
        TempData["Error"] = "Pago cancelado. No se ha generado ninguna suscripcion ni factura.";
        return RedirectToAction(nameof(ContratarSuscripcion));
    }

    private int GetUsuarioId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
