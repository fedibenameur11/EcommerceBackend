using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceAPI.Controllers
{
    /// <summary>
    /// Upload d'images (réservé à l'administrateur). Les fichiers sont enregistrés dans
    /// wwwroot/uploads/products et servis par UseStaticFiles ; l'URL renvoyée est stockée dans Product.ImageUrl.
    /// </summary>
    [ApiController]
    [Route("api/uploads")]
    [Authorize(Roles = "Admin,Seller")]
    public class UploadsController : ControllerBase
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly IWebHostEnvironment _environment;

        public UploadsController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost("images")]
        [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
        public async Task<IActionResult> UploadImage(IFormFile? file, [FromQuery] string? folder = null)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Aucun fichier n'a été envoyé." });
            if (file.Length > MaxFileSize)
                return BadRequest(new { message = "L'image ne doit pas dépasser 5 Mo." });

            // Le type est déduit du contenu du fichier (et non de son nom) pour refuser tout fichier déguisé en image.
            var header = new byte[12];
            await using (var input = file.OpenReadStream())
            {
                var read = await input.ReadAsync(header.AsMemory(0, header.Length));
                if (read < header.Length)
                    return BadRequest(new { message = "Le fichier n'est pas une image valide." });
            }

            var extension = DetectImageExtension(header);
            if (extension == null)
                return BadRequest(new { message = "Format d'image non pris en charge (JPG, PNG, GIF ou WebP)." });

            var webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
            // Sous-dossier limité à une liste blanche (jamais construit à partir de la saisie)
            var subFolder = folder == "categories" ? "categories" : "products";
            var targetFolder = Path.Combine(webRoot, "uploads", subFolder);
            Directory.CreateDirectory(targetFolder);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using (var output = System.IO.File.Create(Path.Combine(targetFolder, fileName)))
            {
                await file.CopyToAsync(output);
            }

            var url = $"{Request.Scheme}://{Request.Host}/uploads/{subFolder}/{fileName}";
            return Ok(new { url });
        }

        private static string? DetectImageExtension(byte[] h)
        {
            if (h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF) return ".jpg";
            if (h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47) return ".png";
            if (h[0] == 0x47 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x38) return ".gif";
            if (h[0] == 0x52 && h[1] == 0x49 && h[2] == 0x46 && h[3] == 0x46
                && h[8] == 0x57 && h[9] == 0x45 && h[10] == 0x42 && h[11] == 0x50) return ".webp";
            return null;
        }
    }
}
