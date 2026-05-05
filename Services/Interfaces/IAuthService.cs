using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IAuthService
{
    Task<Usuario?> ValidateLoginAsync(string email, string password);
    Task RegisterAsync(Usuario usuario, string password);
    Task LogoutAsync();
}