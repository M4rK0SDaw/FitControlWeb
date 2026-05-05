using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class EspecialidadesController : Controller
{
    private readonly IEspecialidadService _especialidadService;

    public EspecialidadesController(IEspecialidadService especialidadService)
    {
        _especialidadService = especialidadService;
    }

    public async Task<IActionResult> Index(
        string? search,
        bool? activo,
        int page = 1,
        int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is 10 or 25 or 50 ? pageSize : 10;

        var especialidades = await _especialidadService.GetFiltradasAsync(
            search,
            activo,
            page,
            pageSize);

        var totalItems = await _especialidadService.CountFiltradasAsync(
            search,
            activo);

        ViewBag.Search = search;
        ViewBag.Activo = activo;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.TotalEspecialidades = totalItems;

        return View(especialidades);
    }

    public async Task<IActionResult> Details(int id)
    {
        var especialidad = await _especialidadService.GetByIdAsync(id);

        if (especialidad == null)
            return NotFound();

        return View(especialidad);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EspecialidadCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var especialidad = new Especialidad
        {
            Nombre = model.Nombre
        };

        var result = await _especialidadService.CreateAsync(especialidad);

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
        var especialidad = await _especialidadService.GetByIdAsync(id);

        if (especialidad == null)
            return NotFound();

        var vm = new EspecialidadEditViewModel
        {
            Id = especialidad.Id,
            Nombre = especialidad.Nombre,
            Activo = especialidad.Activo ?? true
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EspecialidadEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var especialidad = await _especialidadService.GetByIdAsync(model.Id);

        if (especialidad == null)
            return NotFound();

        especialidad.Nombre = model.Nombre;
        especialidad.Activo = model.Activo;

        var result = await _especialidadService.UpdateAsync(especialidad);

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
        var result = await _especialidadService.SoftDeleteAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _especialidadService.ActivarAsync(id);

        TempData[result.Success ? "Success" : "Error"] = result.Message;

        return !string.IsNullOrWhiteSpace(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "NOMBRE":
                ModelState.AddModelError("Nombre", message);
                break;
            case "ESPECIALIDAD":
                ModelState.AddModelError("Nombre", message);
                break;            
            default:
                ModelState.AddModelError("", message);
                break;
        }
    }
}