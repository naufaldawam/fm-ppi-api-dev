using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Master > Kendaraan. Referensi ke Tipe, Bahan Bakar, Kepemilikan,
    /// Jabatan (alokasi jabatan) dan Pekerja (pejabat pemegang - opsional).
    /// </summary>
    [ApiController]
    [Route("kendaraan")]
    [Authorize]
    [Produces("application/json")]
    public class KendaraanController : ControllerBase
    {
        private readonly IKendaraanService _kendaraanService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<KendaraanController> _logger;

        public KendaraanController(
            IKendaraanService kendaraanService,
            ICurrentUser currentUser,
            ILogger<KendaraanController> logger)
        {
            _kendaraanService = kendaraanService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetAll([FromQuery] KendaraanFilterRequest filter)
        {
            var result = await _kendaraanService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] string? pejabatId, [FromQuery] bool withoutRfid = false, [FromQuery] bool activeOnly = true)
        {
            var result = await _kendaraanService.GetLookupAsync(search, pejabatId, withoutRfid, activeOnly);
            return Ok(result);
        }

        /// <summary>
        /// Lookup Pejabat (Pekerja) yang jabatannya sesuai dengan Alokasi Jabatan yang dipilih.
        /// Digunakan frontend untuk mem-filter dropdown Pejabat setelah user memilih Jabatan kendaraan.
        /// GET /kendaraan/pekerja-lookup?jabatanId=xxx&search=yyy&activeOnly=true
        /// </summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetPekerjaLookup(
            [FromQuery] string jabatanId,
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _kendaraanService.GetPekerjaLookupByJabatanAsync(jabatanId, search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _kendaraanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("kendaraan.create")]
        public async Task<IActionResult> Create([FromBody] CreateKendaraanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _kendaraanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Kendaraan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("kendaraan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateKendaraanRequest request)
        {
            var result = await _kendaraanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("kendaraan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _kendaraanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template Excel untuk bulk upload Kendaraan.</summary>
        [HttpGet("download-template")]
        [RequirePermission("kendaraan.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Kendaraan.", _currentUser.UserId);

            var result = await _kendaraanService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload data Kendaraan dari file Excel (.xlsx).
        /// Kolom wajib: Nopol | Merek | Tipe | BahanBakar | Kepemilikan | Jabatan
        /// Kolom opsional: NoPekerja (pejabat pemegang — jabatannya harus cocok dengan kolom Jabatan)
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("kendaraan.create")]
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
            var result = await _kendaraanService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Kendaraan | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}