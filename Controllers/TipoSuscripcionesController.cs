using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class TipoSuscripcionesController : Controller
{
    private readonly ITipoSuscripcionService _tipoSuscripcionService;

    public TipoSuscripcionesController(ITipoSuscripcionService tipoSuscripcionService)
    {
        _tipoSuscripcionService = tipoSuscripcionService;
    }

    public async Task<IActionResult> Index(string? search, bool? activo, int page = 1, int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var tipos = await _tipoSuscripcionService.GetFiltradosAsync(search, activo, page, pageSize);
        var totalItems = await _tipoSuscripcionService.CountFiltradosAsync(search, activo);

        ViewBag.Search = search;
        ViewBag.Activo = activo;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalTipos = totalItems;

        return View(tipos);
    }

    public async Task<IActionResult> Details(int id)
    {
        var tipo = await _tipoSuscripcionService.GetByIdAsync(id);

        if (tipo == null)
            return NotFound();

        return View(tipo);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TipoSuscripcionCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tipo = new TipoSuscripcion
        {
            Nombre = model.Nombre,
            Precio = model.Precio,
            DuracionDias = model.DuracionDias
        };

        var result = await _tipoSuscripcionService.CreateAsync(tipo);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var tipo = await _tipoSuscripcionService.GetByIdAsync(id);

        if (tipo == null)
            return NotFound();

        var vm = new TipoSuscripcionEditViewModel
        {
            Id = tipo.Id,
            Nombre = tipo.Nombre,
            Precio = tipo.Precio,
            DuracionDias = tipo.DuracionDias,
            Activo = tipo.Activo ?? true
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TipoSuscripcionEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tipo = await _tipoSuscripcionService.GetByIdAsync(model.Id);

        if (tipo == null)
            return NotFound();

        tipo.Nombre = model.Nombre;
        tipo.Precio = model.Precio;
        tipo.DuracionDias = model.DuracionDias;
        tipo.Activo = model.Activo;

        var result = await _tipoSuscripcionService.UpdateAsync(tipo);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Desactivar(int id, string? returnUrl)
    {
        var result = await _tipoSuscripcionService.DesactivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id, string? returnUrl)
    {
        var result = await _tipoSuscripcionService.ActivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private void AddServiceError(string? code, string message)
    {
        if (code == "NOMBRE_DUPLICADO")
            ModelState.AddModelError("Nombre", message);
        else
            ModelState.AddModelError("", message);
    }
}