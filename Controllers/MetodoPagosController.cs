using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class MetodoPagosController : Controller
{
    private readonly IMetodoPagoService _metodoPagoService;

    public MetodoPagosController(IMetodoPagoService metodoPagoService)
    {
        _metodoPagoService = metodoPagoService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var metodos = await _metodoPagoService.GetFiltradosAsync(search, page, pageSize);
        var totalItems = await _metodoPagoService.CountFiltradosAsync(search);

        ViewBag.Search = search;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalMetodos = totalItems;

        return View(metodos);
    }

    public async Task<IActionResult> Details(int id)
    {
        var metodo = await _metodoPagoService.GetByIdAsync(id);

        if (metodo == null)
            return NotFound();

        return View(metodo);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MetodoPagoCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var metodo = new MetodoPago
        {
            Nombre = model.Nombre
        };

        var result = await _metodoPagoService.CreateAsync(metodo);

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
        var metodo = await _metodoPagoService.GetByIdAsync(id);

        if (metodo == null)
            return NotFound();

        var vm = new MetodoPagoEditViewModel
        {
            Id = metodo.Id,
            Nombre = metodo.Nombre
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MetodoPagoEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var metodo = await _metodoPagoService.GetByIdAsync(model.Id);

        if (metodo == null)
            return NotFound();

        metodo.Nombre = model.Nombre;

        var result = await _metodoPagoService.UpdateAsync(metodo);

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
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var result = await _metodoPagoService.DeleteAsync(id);

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