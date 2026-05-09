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
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IUsuarioService usuarioService,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _emailService = emailService;
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

        var usuarioValidado = await _authService.ValidateLoginAsync(model.Email, model.Password);

        if (usuarioValidado == null)
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(model);
        }

        await _authService.SignInAsync(usuarioValidado, model.RememberMe);

        return RedirectByRole(usuarioValidado.Rol.Nombre);
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
                var body = $"""
                    <p>Hola {result.Data.Nombre},</p>
                    <p>Hemos recibido una solicitud para restablecer tu contraseña en FitControl Web.</p>
                    <p>Pulsa en el siguiente enlace para continuar:</p>
                    <p><a href="{resetLink}">Restablecer contraseña</a></p>
                    <p>Este enlace caduca en 1 hora.</p>
                    <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    """;

                try
                {
                    await _emailService.SendAsync(result.Data.Email, "Restablecer contraseña - FitControl Web", body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar email de recuperación para usuario {UserId}", result.Data.Id);
                    TempData["Error"] = "No se pudo enviar el correo de recuperación. Inténtalo de nuevo.";
                    return View(model);
                }
            }
        }

        TempData["Success"] = "Si el email existe, recibirás un enlace para restablecer tu contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (!await _authService.TokenRecuperacionValidoAsync(token))
        {
            TempData["Error"] = "El enlace de recuperación es inválido o ha caducado.";
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
