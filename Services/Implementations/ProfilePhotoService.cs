using FitControlWeb.Helpers;
using FitControlWeb.Services.Interfaces;

namespace FitControlWeb.Services.Implementations;

public class ProfilePhotoService : IProfilePhotoService
{
    private readonly IWebHostEnvironment _environment;

    public ProfilePhotoService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ServiceResult> GuardarFotoUsuarioAsync(int usuarioId, IFormFile? foto)
    {
        if (foto == null || foto.Length == 0)
            return ServiceResult.Ok();

        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(foto.FileName ?? "").ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
            return ServiceResult.Fail("La imagen no tiene una extensión válida.");

        if (!extensionesPermitidas.Contains(extension))
            return ServiceResult.Fail("Formato de imagen no válido. Usa JPG, PNG o WEBP.");

        if (foto.Length > 5 * 1024 * 1024)
            return ServiceResult.Fail("La imagen no puede superar los 5MB.");

        var carpeta = Path.Combine(_environment.WebRootPath, "uploads", "usuarios");

        Directory.CreateDirectory(carpeta);

        foreach (var archivo in Directory.GetFiles(carpeta, $"usuario-{usuarioId}.*"))
        {
            File.Delete(archivo);
        }

        var nombreArchivo = $"usuario-{usuarioId}{extension}";
        var rutaArchivo = Path.Combine(carpeta, nombreArchivo);

        await using var stream = new FileStream(rutaArchivo, FileMode.Create);
        await foto.CopyToAsync(stream);

        return ServiceResult.Ok("Foto guardada correctamente.");
    }
}
