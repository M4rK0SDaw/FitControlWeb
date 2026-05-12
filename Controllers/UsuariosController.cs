using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        IUsuarioService usuarioService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<UsuariosController> logger)
    {
        _usuarioService = usuarioService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, int? rolId, bool? activo, int page = 1, int pageSize = 10)
    {
        var vm = await _usuarioService.GetIndexViewModelAsync(search, rolId, activo, page, pageSize);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario == null)
            return NotFound();

        return View(usuario);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await _usuarioService.GetCreateViewModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model)
    {
        ModelState.Remove(nameof(model.Foto));

        if (!ModelState.IsValid)
        {
            model.Roles = (await _usuarioService.GetCreateViewModelAsync()).Roles;
            return View(model);
        }

        var foto = model.Foto ?? Request.Form.Files.FirstOrDefault(f => f.Name == "Foto");
        var result = await _usuarioService.CreateFromViewModelAsync(model, foto);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            model.Roles = (await _usuarioService.GetCreateViewModelAsync()).Roles;
            return View(model);
        }

        try
        {
            var template = _emailTemplateService.EmailBienvenida(result.Data!.Nombre);
            await _emailService.SendAsync(result.Data.Email, template.Subject, template.HtmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el correo de bienvenida al usuario {UserId}", result.Data!.Id);
            TempData["Warning"] = "El usuario se ha creado correctamente, pero no se pudo enviar el email de bienvenida.";
        }

        TempData["Success"] = result.Message;
        return model.RolId == 3
            ? RedirectToAction("Create", "Suscripciones", new { usuarioId = result.Data!.Id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _usuarioService.GetEditViewModelAsync(id);

        if (vm == null)
            return NotFound();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UsuarioEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var existingVm = await _usuarioService.GetEditViewModelAsync(model.Id);
            model.Roles = existingVm?.Roles ?? new();
            return View(model);
        }

        var result = await _usuarioService.UpdateFromViewModelAsync(model);

        if (!result.Success)
        {
            AddServiceError(result.Code, result.Message);
            var existingVm = await _usuarioService.GetEditViewModelAsync(model.Id);
            model.Roles = existingVm?.Roles ?? new();
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var result = await _usuarioService.SoftDeleteAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reactivar(int id, string? returnUrl)
    {
        var result = await _usuarioService.ActivarAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectLocalOrIndex(returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirFoto(int id, IFormFile foto)
    {
        var result = await _usuarioService.GuardarFotoAsync(id, foto);
        TempData[result.Success ? "Success" : "Error"] = result.Message;

        if (result.Code == "USUARIO")
            return NotFound();

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarEmail(int id, string asunto, string mensaje)
    {
        var usuario = await _usuarioService.GetByIdAsync(id);

        if (usuario == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(mensaje))
        {
            TempData["Error"] = "Debes indicar asunto y mensaje para enviar el email.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var template = _emailTemplateService.EmailAdminDirecto(usuario.Nombre, asunto, mensaje);

        await _emailService.SendAsync(usuario.Email, template.Subject, template.HtmlBody);
        TempData["Success"] = "Email enviado correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> ExportCsv(string? search, int? rolId, bool? activo)
    {
        var file = await _usuarioService.ExportCsvAsync(search, rolId, activo);
        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> ExportExcel(string? search, int? rolId, bool? activo)
    {
        var file = await _usuarioService.ExportExcelAsync(search, rolId, activo);
        return File(file.Content, file.ContentType, file.FileName);
    }

    public async Task<IActionResult> ExportPdf(string? search, int? rolId, bool? activo)
    {
        try
        {
            var file = await _usuarioService.ExportPdfAsync(search, rolId, activo);
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error al generar PDF: {ex.Message}";
            return RedirectToAction(nameof(Index), new { search, rolId, activo });
        }
    }

    private void AddServiceError(string? code, string message)
    {
        switch (code)
        {
            case "EMAIL":
            case "EMAIL_DUPLICADO":
                ModelState.AddModelError("Email", message);
                break;
            default:
                ModelState.AddModelError(string.Empty, message);
                break;
        }
    }

    private IActionResult RedirectLocalOrIndex(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction(nameof(Index));
    }
}
