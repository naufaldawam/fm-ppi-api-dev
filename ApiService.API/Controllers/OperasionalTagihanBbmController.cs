using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiService.API.Filters;
using ApiService.Application.DTOs;
using ApiService.Application.Services;
using ApiService.Application.Interfaces;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("operasional-tagihan-bbm-reconciliation")]
    [Authorize]
    [Produces("application/json")]
    public class OperasionalTagihanBbmController(IOperasionalTagihanBbmBulkUploadService service, ICurrentUser currentUser, ILogger<OperasionalTagihanBbmController> logger) : ControllerBase
    {
        private readonly IOperasionalTagihanBbmBulkUploadService _service = service;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<OperasionalTagihanBbmController> _logger = logger;

        [HttpGet]
        [RequirePermission("operasional-tagihan-bbm-reconciliation.read")]
        public async Task<IActionResult> GetAll([FromQuery] OperasionalTagihanBbmFilterRequest filter)
        {
            var result = await _service.GetAllAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("operasional-tagihan-bbm-reconciliation.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("bulk-upload/template")]
        [RequirePermission("operasional-tagihan-bbm-reconciliation.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation(
                "Download template BBM reconciliation dimulai. UserId: {UserId}",
                _currentUser.UserId);

            var result = await _service.GetImportTemplateAsync();

            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            _logger.LogInformation(
                "Download template BBM reconciliation berhasil. UserId: {UserId}, FileName: {FileName}",
                _currentUser.UserId,
                result.Data.FileName);

            return File(
                result.Data.FileStream,
                result.Data.ContentType,
                result.Data.FileName);
        }

        [HttpPost("bulk-upload/import-excel")]
        [RequirePermission("operasional-tagihan-bbm-reconciliation.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            _logger.LogInformation(
                "Bulk upload BBM reconciliation dimulai. UserId: {UserId}, FileName: {FileName}, Size: {Size}",
                _currentUser.UserId,
                file?.FileName,
                file?.Length ?? 0);

            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", System.StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "Hanya mendukung file .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _service.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Bulk upload BBM reconciliation selesai. File: {FileName} | Size: {Size} | UserId: {UserId} | Inserted: {Count} | Matched: {Matched} | Mismatch: {Mismatch} | NotMatched: {NotMatched}",
                file.FileName,
                file.Length,
                userId,
                result.Data?.InsertedBbmReconciliation ?? 0,
                result.Data?.MatchedCount ?? 0,
                result.Data?.MismatchCount ?? 0,
                result.Data?.NotMatchedCount ?? 0);

            return StatusCode(result.StatusCode, result);
        }
    }
}
