using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using FitControlWeb.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitControlWeb.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUsuarioService _usuarioService;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IUsuarioService usuarioService,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
            return RedirectByRole();

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var loginResult = await _authService.ValidateLoginAsync(model.Email, model.Password);

        if (!loginResult.Success || loginResult.Data == null)
        {
            if (loginResult.Code is "ACCOUNT_LOCKED" or "ACCOUNT_BLOCKED")
            {
                var usuarioBloqueado = loginResult.Data;
                var token = usuarioBloqueado?.RefreshToken;
                var resetLink = !string.IsNullOrWhiteSpace(token)
                    ? Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme)
                    : null;

                if (!string.IsNullOrWhiteSpace(resetLink) && usuarioBloqueado != null)
                {
                    var template = _emailTemplateService.EmailCuentaBloqueada(usuarioBloqueado.Nombre, resetLink);

                    try
                    {
                        await _emailService.SendAsync(usuarioBloqueado.Email, template.Subject, template.HtmlBody);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo enviar el correo de recuperacion para {Email}", usuarioBloqueado.Email);
                    }
                }

                TempData["Warning"] = loginResult.Code == "ACCOUNT_LOCKED"
                    ? "Cuenta bloqueada por seguridad. Revisa tu correo para recuperarla."
                    : "Tu cuenta sigue bloqueada. Te hemos reenviado el correo de recuperacion.";
                return RedirectToAction(nameof(Login));
            }

            ModelState.AddModelError("", loginResult.Message);
            return View(model);
        }

        await _authService.SignInAsync(loginResult.Data, model.RememberMe);
        return RedirectByRole(loginResult.Data.Rol.Nombre);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        if (await _usuarioService.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con ese email.");
            return View(model);
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre,
            Apellidos = model.Apellidos,
            Email = model.Email,
            Telefono = model.Telefono,
            RolId = 3
        };

        await _authService.RegisterAsync(usuario, model.Password);

        try
        {
            var template = _emailTemplateService.EmailBienvenida(usuario.Nombre);

            await _emailService.SendAsync(usuario.Email, template.Subject, template.HtmlBody);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar el correo de bienvenida a {Email}", usuario.Email);
        }

        TempData["Success"] = "Registro completado. Ya puedes iniciar sesion.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Denied()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.PrepararRecuperacionPasswordAsync(model.Email);

        if (result.Success && result.Data != null)
        {
            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token = result.Data.RefreshToken },
                Request.Scheme);

            if (!string.IsNullOrWhiteSpace(resetLink))
            {
                var template = _emailTemplateService.EmailRestablecerContrasenya(result.Data.Nombre, resetLink);

                try
                {
                    await _emailService.SendAsync(result.Data.Email, template.Subject, template.HtmlBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de recuperacion para usuario {UserId}", result.Data.Id);
                    TempData["Error"] = "No se pudo enviar el correo de recuperacion. Intentalo de nuevo.";
                    return View(model);
                }
            }
        }

        TempData["Success"] = "Si el email existe, recibiras un enlace para restablecer tu contrasena.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (!await _authService.TokenRecuperacionValidoAsync(token))
        {
            TempData["Error"] = "El enlace de recuperacion es invalido o ha caducado.";
            return RedirectToAction(nameof(Login));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.ResetPasswordAsync(model.Token, model.Password);

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(Login));
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectByRole(string? rol = null)
    {
        rol ??= User.IsInRole("Administrador") ? "Administrador" :
            User.IsInRole("Cliente") ? "Cliente" :
            User.IsInRole("Entrenador") ? "Entrenador" : null;

        return rol switch
        {
            "Administrador" => RedirectToAction("Index", "Dashboard"),
            "Cliente" => RedirectToAction("Index", "ClienteDashboard"),
            "Entrenador" => RedirectToAction("Index", "EntrenadorDashboard"),
            _ => RedirectToAction("Index", "Home")
        };
    }
}
