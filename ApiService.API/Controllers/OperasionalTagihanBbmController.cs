using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("operasional-tagihan-bbm/bulk-upload")]
    [Authorize]
    [Produces("application/json")]
    public class OperasionalTagihanBbmController(
        IOperasionalTagihanBbmBulkUploadService bulkUploadService,
        ICurrentUser currentUser,
        ILogger<OperasionalTagihanBbmController> logger) : ControllerBase
    {
        private readonly IOperasionalTagihanBbmBulkUploadService _bulkUploadService = bulkUploadService;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<OperasionalTagihanBbmController> _logger = logger;

        [HttpGet("bulk-upload/template")]
        [RequirePermission("operasional-tagihan-bbm.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            var result = await _bulkUploadService.GetImportTemplateAsync();

            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
        }

        [HttpPost("bulk-upload/import-excel")]
        [RequirePermission("operasional-tagihan-bbm.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Hanya mendukung file .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _bulkUploadService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Bulk import Tagihan BBM | File: {FileName} | Size: {Size} | UserId: {UserId} | Inserted: {Count}",
                file.FileName,
                file.Length,
                userId,
                result.Data?.InsertedBbmSubmission ?? 0);

            return StatusCode(result.StatusCode, result);
        }
    }
}
