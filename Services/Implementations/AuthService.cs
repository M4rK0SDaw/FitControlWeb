using FitControlWeb.Data;
using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FitControlWeb.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly FitControlDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(FitControlDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ServiceResult<Usuario>> ValidateLoginAsync(string email, string password)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null)
            return ServiceResult<Usuario>.Fail("Email o contrasena incorrectos.", "INVALID_CREDENTIALS");

        if (usuario.Activo != true)
            return ServiceResult<Usuario>.Fail("La cuenta no esta activa.", "ACCOUNT_INACTIVE");

        if (usuario.Bloqueado == true)
            return ServiceResult<Usuario>.Fail("La cuenta esta bloqueada. Revisa tu email para recuperarla.", "ACCOUNT_BLOCKED");

        var passwordOk = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

        _context.UsuarioLoginLogs.Add(new UsuarioLoginLog
        {
            UsuarioId = usuario.Id,
            FechaLogin = DateTime.Now,
            Exitoso = passwordOk,
            Ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });

        if (!passwordOk)
        {
            usuario.IntentosFallidos = (usuario.IntentosFallidos ?? 0) + 1;

            var cuentaBloqueada = false;
            if (usuario.IntentosFallidos >= 5)
            {
                usuario.Bloqueado = true;
                cuentaBloqueada = true;
                usuario.RefreshToken = CrearToken();
                usuario.RefreshTokenExpiryTime = DateTime.Now.AddHours(1);
            }

            await _context.SaveChangesAsync();
            if (cuentaBloqueada)
                return ServiceResult<Usuario>.Fail("Tu cuenta ha sido bloqueada por seguridad.", "ACCOUNT_LOCKED", usuario);

            return ServiceResult<Usuario>.Fail("Email o contrasena incorrectos.", "INVALID_CREDENTIALS");
        }

        usuario.IntentosFallidos = 0;
        usuario.UltimoLogin = DateTime.Now;

        await _context.SaveChangesAsync();

        return ServiceResult<Usuario>.Ok(usuario);
    }

    public async Task SignInAsync(Usuario usuario, bool rememberMe)
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            return;

        var refreshToken = CrearToken();

        usuario.RefreshToken = refreshToken;
        usuario.RefreshTokenExpiryTime = rememberMe
            ? DateTime.Now.AddDays(7)
            : DateTime.Now.AddHours(8);

        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
            new("NombreCompleto", $"{usuario.Nombre} {usuario.Apellidos}"),
            new("RefreshToken", refreshToken)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(7)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    public async Task RegisterAsync(Usuario usuario, string password)
    {
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        usuario.FechaRegistro = DateTime.Now;
        usuario.Activo = true;
        usuario.Bloqueado = false;
        usuario.IntentosFallidos = 0;

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null)
            return;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userIdClaim, out var usuarioId))
        {
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);

            if (usuario != null)
            {
                usuario.RefreshToken = null;
                usuario.RefreshTokenExpiryTime = null;

                await _context.SaveChangesAsync();
            }
        }

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public async Task<ServiceResult<Usuario>> PrepararRecuperacionPasswordAsync(string email)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo == true);

        if (usuario == null)
            return ServiceResult<Usuario>.Fail("Usuario no encontrado.", "USUARIO_NO_EXISTE");

        usuario.RefreshToken = CrearToken();
        usuario.RefreshTokenExpiryTime = DateTime.Now.AddHours(1);

        await _context.SaveChangesAsync();

        return ServiceResult<Usuario>.Ok(usuario);
    }

    public async Task<bool> TokenRecuperacionValidoAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        return await _context.Usuarios.AnyAsync(u =>
            u.RefreshToken == token &&
            u.RefreshTokenExpiryTime != null &&
            u.RefreshTokenExpiryTime > DateTime.Now &&
            u.Activo == true);
    }

    public async Task<ServiceResult> ResetPasswordAsync(string token, string password)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u =>
            u.RefreshToken == token &&
            u.RefreshTokenExpiryTime != null &&
            u.RefreshTokenExpiryTime > DateTime.Now &&
            u.Activo == true);

        if (usuario == null)
            return ServiceResult.Fail("El enlace de recuperación es inválido o ha caducado.", "TOKEN_INVALIDO");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        usuario.RefreshToken = null;
        usuario.RefreshTokenExpiryTime = null;
        usuario.IntentosFallidos = 0;
        usuario.Bloqueado = false;

        await _context.SaveChangesAsync();

        return ServiceResult.Ok("Contraseña actualizada correctamente. Ya puedes iniciar sesión.");
    }

    private static string CrearToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }
}
