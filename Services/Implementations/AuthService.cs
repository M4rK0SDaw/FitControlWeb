using BCrypt.Net;
using FitControlWeb.Data;
using FitControlWeb.Models.Entities;
using FitControlWeb.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

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

    public async Task<Usuario?> ValidateLoginAsync(string email, string password)
    {
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (usuario == null)
            return null;

        if (usuario.Activo != true || usuario.Bloqueado == true)
            return null;

        bool passwordOk = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

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

            if (usuario.IntentosFallidos >= 5)
                usuario.Bloqueado = true;

            await _context.SaveChangesAsync();
            return null;
        }

        usuario.IntentosFallidos = 0;
        usuario.UltimoLogin = DateTime.Now;

        await _context.SaveChangesAsync();

        return usuario;
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

        if (int.TryParse(userIdClaim, out int usuarioId))
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
    //public async Task LogoutAsync()
    //{
    //    if (_httpContextAccessor.HttpContext != null)
    //    {
    //        await _httpContextAccessor.HttpContext.SignOutAsync("Cookies");
    //    }
    //}
}