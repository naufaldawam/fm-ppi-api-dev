using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.Interfaces;
using ApiService.API.Filters;
using ApiService.Application.Services;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("operasional-tagihan-kwh-umum/bulk-upload")]
    [Authorize]
    [Produces("application/json")]
    public class OperasionalTagihanKwhUmumController(
        IOperasionalTagihanKwhUmumBulkUploadService bulkUploadService,
        ICurrentUser currentUser,
        ILogger<OperasionalTagihanKwhUmumController> logger) : ControllerBase
    {
        private readonly IOperasionalTagihanKwhUmumBulkUploadService _bulkUploadService = bulkUploadService;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<OperasionalTagihanKwhUmumController> _logger = logger;

        [HttpGet("bulk-upload/download-template")]
        [RequirePermission("tagihan-kwh.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            var result = await _bulkUploadService.GetImportTemplateAsync();

            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
        }

        [HttpPost("bulk-upload/import-excel")]
        [RequirePermission("tagihan-kwh.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Hanya mendukung file .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _bulkUploadService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Tagihan KWh Umum | File: {FileName} | Size: {Size} | UserId: {UserId} | Inserted: {Count}",
                file.FileName,
                file.Length,
                userId,
                result.Data?.InsertedTagihanKwh ?? 0);

            return StatusCode(result.StatusCode, result);
        }
    }
}
