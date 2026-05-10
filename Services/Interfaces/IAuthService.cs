using FitControlWeb.Helpers;
using FitControlWeb.Models.Entities;

namespace FitControlWeb.Services.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<Usuario>> ValidateLoginAsync(string email, string password);
    Task SignInAsync(Usuario usuario, bool rememberMe);
    Task RegisterAsync(Usuario usuario, string password);
    Task LogoutAsync();
    Task<ServiceResult<Usuario>> PrepararRecuperacionPasswordAsync(string email);
    Task<bool> TokenRecuperacionValidoAsync(string token);
    Task<ServiceResult> ResetPasswordAsync(string token, string password);
}
