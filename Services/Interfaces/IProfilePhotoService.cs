using FitControlWeb.Helpers;

namespace FitControlWeb.Services.Interfaces;

public interface IProfilePhotoService
{
    Task<ServiceResult> GuardarFotoUsuarioAsync(int usuarioId, IFormFile? foto);
}
