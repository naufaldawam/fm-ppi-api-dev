using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Master > RF.ID. Assignment RF.ID ke Pekerja dan Kendaraan (Nopol).
    /// Create/Update/Delete otomatis mensinkronkan ke Pekerja.RfIds.
    /// </summary>
    [ApiController]
    [Route("rfid")]
    [Authorize]
    [Produces("application/json")]
    public class RfIdController : ControllerBase
    {
        private readonly IRfIdService _rfIdService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<RfIdController> _logger;

        public RfIdController(
            IRfIdService rfIdService,
            ICurrentUser currentUser,
            ILogger<RfIdController> logger)
        {
            _rfIdService = rfIdService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("rfid.read")]
        public async Task<IActionResult> GetAll([FromQuery] RfIdFilterRequest filter)
        {
            var result = await _rfIdService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("rfid.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _rfIdService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("rfid.create")]
        public async Task<IActionResult> Create([FromBody] CreateRfIdRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _rfIdService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("RF.ID created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("rfid.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRfIdRequest request)
        {
            var result = await _rfIdService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("rfid.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _rfIdService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template Excel untuk bulk upload RF.ID.</summary>
        [HttpGet("download-template")]
        [RequirePermission("rfid.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template RF.ID.", _currentUser.UserId);

            var result = await _rfIdService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>Bulk upload data RF.ID dari file Excel (.xlsx). Otomatis sync ke Pekerja.RfIds.</summary>
        [HttpPost("import-excel")]
        [RequirePermission("rfid.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            const long maxFileSize = 20 * 1024 * 1024;
            if (file.Length > maxFileSize)
                return BadRequest(new { success = false, message = "Ukuran file maksimal 20 MB." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { success = false, message = "Hanya mendukung file dengan format .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _rfIdService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import RF.ID | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}