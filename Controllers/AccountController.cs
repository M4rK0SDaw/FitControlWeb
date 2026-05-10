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

        var loginResult = await _authService.ValidateLoginAsync(model.Email, model.Password);

        if (!loginResult.Success || loginResult.Data == null)
        {
            if (loginResult.Code == "ACCOUNT_LOCKED")
            {
                var usuarioBloqueado = loginResult.Data;
                var token = usuarioBloqueado?.RefreshToken;
                var resetLink = !string.IsNullOrWhiteSpace(token)
                    ? Url.Action(nameof(ResetPassword), "Account", new { token }, Request.Scheme)
                    : null;

                if (!string.IsNullOrWhiteSpace(resetLink) && usuarioBloqueado != null)
                {
                    var body = $"""
                        <p>Hola {usuarioBloqueado.Nombre},</p>
                        <p>Tu cuenta se ha bloqueado por varios intentos de acceso fallidos.</p>
                        <p>Para recuperarla, restablece tu contrasena aqui:</p>
                        <p><a href="{resetLink}">Recuperar cuenta</a></p>
                        <p>El enlace caduca en 1 hora.</p>
                        """;

                    try
                    {
                        await _emailService.SendAsync(usuarioBloqueado.Email, "Cuenta bloqueada - Recuperacion FitControl", body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo enviar el correo de recuperacion para {Email}", usuarioBloqueado.Email);
                    }
                }

                TempData["Warning"] = "Cuenta bloqueada por seguridad. Revisa tu correo para recuperarla.";
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
            var body = $"""
                <p>Hola {usuario.Nombre},</p>
                <p>Bienvenido a FitControl Web.</p>
                <p>Tu cuenta ya esta lista para reservar clases y gestionar tu actividad.</p>
                """;

            await _emailService.SendAsync(usuario.Email, "Bienvenido a FitControl Web", body);
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
                var body = $"""
                    <p>Hola {result.Data.Nombre},</p>
                    <p>Hemos recibido una solicitud para restablecer tu contrasena en FitControl Web.</p>
                    <p>Pulsa en el siguiente enlace para continuar:</p>
                    <p><a href="{resetLink}">Restablecer contrasena</a></p>
                    <p>Este enlace caduca en 1 hora.</p>
                    <p>Si no solicitaste este cambio, puedes ignorar este mensaje.</p>
                    """;

                try
                {
                    await _emailService.SendAsync(result.Data.Email, "Restablecer contrasena - FitControl Web", body);
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
