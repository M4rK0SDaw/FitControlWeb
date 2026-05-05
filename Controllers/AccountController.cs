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

    public AccountController(IAuthService authService,
                             IUsuarioService usuarioService,
                             FitControlDbContext context)
    {
        _authService = authService;
        _usuarioService = usuarioService;
        _context = context;
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
    public async Task<IActionResult> Register(RegisterViewModel VMmodel)
    {
        if (!ModelState.IsValid)
            return View(VMmodel);

        if (await _usuarioService.EmailExistsAsync(VMmodel.Email))
        {
            ModelState.AddModelError("Email", "Ya existe un usuario con ese email.");
            return View(VMmodel);
        }

        var usuario = new Usuario
        {
            Nombre = VMmodel.Nombre,
            Apellidos = VMmodel.Apellidos,
            Email = VMmodel.Email,
            Telefono = VMmodel.Telefono,
            RolId = 3 
        };

        await _authService.RegisterAsync(usuario, VMmodel.Password);

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
    public IActionResult Register()
    {
        return View();
    }
}
