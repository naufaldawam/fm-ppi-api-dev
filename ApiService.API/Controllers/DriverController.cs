using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;
using System.IO;

namespace ApiService.API.Controllers
{
    /// <summary>Data Master > Driver. Terhubung ke Vendor dan Atasan (Pekerja).</summary>
    [ApiController]
    [Route("driver")]
    [Authorize]
    [Produces("application/json")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DriverController> _logger;

        public DriverController(
            IDriverService driverService,
            ICurrentUser currentUser,
            ILogger<DriverController> logger)
        {
            _driverService = driverService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("driver.read")]
        public async Task<IActionResult> GetAll([FromQuery] DriverFilterRequest filter)
        {
            var result = await _driverService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("driver.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _driverService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("driver.create")]
        public async Task<IActionResult> Create([FromBody] CreateDriverRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _driverService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Driver created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("driver.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateDriverRequest request)
        {
            var result = await _driverService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("driver.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _driverService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template Excel untuk bulk upload Driver.</summary>
        [HttpGet("download-template")]
        [RequirePermission("driver.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Driver.", _currentUser.UserId);

            var result = await _driverService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload data Driver dari file Excel (.xlsx).
        /// Kolom wajib: NoPekerja | NamaDriver | NoHp | Email | Vendor | NoPekerjaAtasan.
        /// Gunakan endpoint download-template untuk mendapatkan file template beserta sheet referensi.
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("driver.create")]
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
            var result = await _driverService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Driver | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}