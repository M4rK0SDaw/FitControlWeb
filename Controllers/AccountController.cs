using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using FitControlWeb.ViewModels.Auth;
using Microsoft.EntityFrameworkCore;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using FitControlWeb.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;



namespace FitControlWeb.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUsuarioService _usuarioService;
    private readonly FitControlDbContext _context;    
    private readonly IConfiguration _configuration;

    public AccountController(IAuthService authService,
                             IUsuarioService usuarioService,
                             FitControlDbContext context,
                             IConfiguration configuration)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _context = context;
        _configuration = configuration;

    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            if (User.IsInRole("Administrador"))
                return RedirectToAction("Index", "Dashboard");

            if (User.IsInRole("Cliente"))
                return RedirectToAction("Index", "ClienteDashboard");

            if (User.IsInRole("Entrenador"))
                return RedirectToAction("Index", "Clases");

            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _context.Usuarios   
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (usuario == null)
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(model);
        }

        var usuarioValidado = await _authService.ValidateLoginAsync(model.Email, model.Password);

        if (usuarioValidado == null)
        {
            ModelState.AddModelError("", "Email o contraseña incorrectos.");
            return View(model);
        }

        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        var refreshTokenExpiry = model.RememberMe
            ? DateTime.Now.AddDays(7)
            : DateTime.Now.AddHours(8);

        usuarioValidado.RefreshToken = refreshToken;
        usuarioValidado.RefreshTokenExpiryTime = refreshTokenExpiry;

        await _context.SaveChangesAsync();

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuarioValidado.Id.ToString()),
        new(ClaimTypes.Name, usuarioValidado.Email),
        new(ClaimTypes.Role, usuarioValidado.Rol.Nombre),
        new("NombreCompleto", $"{usuarioValidado.Nombre} {usuarioValidado.Apellidos}"),
        new("RefreshToken", refreshToken)
    };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        if (usuarioValidado.Rol.Nombre == "Administrador")
            return RedirectToAction("Index", "Dashboard");

        if (usuarioValidado.Rol.Nombre == "Cliente")
            return RedirectToAction("Index", "ClienteDashboard");

        if (usuarioValidado.Rol.Nombre == "Entrenador")
            return RedirectToAction("Index", "EntrenadorDashboard");

        return RedirectToAction("Index", "Home");
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
    public IActionResult Register()
    {
        return View();
    }   

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim().ToLower();

        var existeEmail = await _context.Usuarios
            .AnyAsync(u => u.Email.ToLower() == email);

        if (existeEmail)
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con este email.");
            return View(model);
        }

        var rolCliente = await _context.Rols
            .FirstOrDefaultAsync(r => r.Nombre == "Cliente");
        
        int rolClienteId = rolCliente != null ? rolCliente.Id : 0;
        // auditoria se creaun registro par qaue el Adminsitardor vea que hay que manejar un error 
        

        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Apellidos = model.Apellidos.Trim(),
            Email = email,
            Telefono = model.Telefono,
            RolId = rolClienteId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            FechaRegistro = DateTime.Now,
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 0
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cuenta creada correctamente. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim().ToLower();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == email &&
                u.Activo == true &&
                u.Bloqueado != true);

        // Respuesta genérica por seguridad
        if (usuario == null)
        {
            TempData["Success"] = "Si el email existe, recibirás instrucciones para restablecer la contraseña.";
            return RedirectToAction(nameof(Login));
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        usuario.RefreshToken = token;
        usuario.RefreshTokenExpiryTime = DateTime.Now.AddMinutes(30);

        await _context.SaveChangesAsync();

        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = usuario.Email, token },
            Request.Scheme);

        var body = $@"
        <h2>Restablecer contraseña</h2>
        <p>Has solicitado cambiar tu contraseña en FitControl Web.</p>
        <p>Haz clic en el siguiente enlace:</p>
        <p><a href='{resetLink}'>Restablecer contraseña</a></p>
        <p>Este enlace caduca en 30 minutos.</p>
        <p>Si no solicitaste este cambio, ignora este mensaje.</p>
    ";

        await EnviarEmailAsync(
            usuario.Email,
            "Restablecer contraseña - FitControl Web",
            body);

        TempData["Success"] = "Si el email existe, recibirás instrucciones para restablecer la contraseña.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return RedirectToAction(nameof(Login));

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.RefreshToken == token &&
                u.RefreshTokenExpiryTime > DateTime.Now);

        if (usuario == null)
        {
            TempData["Error"] = "El enlace de recuperación no es válido o ha caducado.";
            return RedirectToAction(nameof(Login));
        }

        var vm = new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        };

        return View(vm);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u =>
                u.Email == model.Email &&
                u.RefreshToken == model.Token &&
                u.RefreshTokenExpiryTime > DateTime.Now);

        if (usuario == null)
        {
            TempData["Error"] = "El enlace de recuperación no es válido o ha caducado.";
            return RedirectToAction(nameof(Login));
        }

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiryTime = null;
        usuario.IntentosFallidos = 0;
        usuario.Bloqueado = false;

        await _context.SaveChangesAsync();

        TempData["Success"] = "Contraseña actualizada correctamente. Ya puedes iniciar sesión.";
        return RedirectToAction(nameof(Login));
    }

    private async Task EnviarEmailAsync(string to, string subject, string htmlBody)
    {
        var from = _configuration["Email:From"];
        var user = _configuration["Email:User"];
        var password = _configuration["Email:Password"];
        var smtp = _configuration["Email:Smtp"];
        var port = int.Parse(_configuration["Email:Port"] ?? "587");

        using var client = new SmtpClient(smtp, port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(user, password)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(from!),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(to);

        //await client.SendMailAsync(message);
        try
        {
            await client.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            throw new Exception("Error enviando email: " + ex.Message, ex);
        }

    }

}
