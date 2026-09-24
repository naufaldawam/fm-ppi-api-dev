using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Biaya Kesehatan - datatable + form (Pekerja, Bulan Tahun, Periode, TotalBiaya).</summary>
    [ApiController]
    [Route("biaya-kesehatan")]
    [Authorize]
    [Produces("application/json")]
    public class BiayaKesehatanController : ControllerBase
    {
        private readonly IBiayaKesehatanService _biayaKesehatanService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<BiayaKesehatanController> _logger;

        public BiayaKesehatanController(
            IBiayaKesehatanService biayaKesehatanService,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<BiayaKesehatanController> logger)
        {
            _biayaKesehatanService = biayaKesehatanService;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> GetAll([FromQuery] BiayaKesehatanFilterRequest filter)
        {
            var result = await _biayaKesehatanService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Dropdown Pekerja: setelah pilih, FE terisi Rfid + No.Pekerja + Jabatan.
        /// GET /biaya-kesehatan/pekerja-lookup?search=xxx
        /// </summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Dropdown Periode: GET /biaya-kesehatan/periode-lookup?search=xxx</summary>
        [HttpGet("periode-lookup")]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Summary box: total record + grand total biaya, filter by periode.</summary>
        [HttpGet("summary")]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> GetSummary([FromQuery] BiayaKesehatanSummaryRequest filter)
        {
            var result = await _biayaKesehatanService.GetSummaryAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _biayaKesehatanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("biaya-kesehatan.create")]
        public async Task<IActionResult> Create([FromBody] CreateBiayaKesehatanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _biayaKesehatanService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Biaya kesehatan created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("biaya-kesehatan.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateBiayaKesehatanRequest request)
        {
            var result = await _biayaKesehatanService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("biaya-kesehatan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _biayaKesehatanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template: GET /biaya-kesehatan/download-template</summary>
        [HttpGet("download-template")]
        [RequirePermission("biaya-kesehatan.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Biaya Kesehatan.", _currentUser.UserId);

            var result = await _biayaKesehatanService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload Biaya Kesehatan (.xlsx). Kolom: NoPekerja | BulanTahun | Periode | TotalBiaya
        /// POST /biaya-kesehatan/import-excel
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("biaya-kesehatan.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
                return BadRequest(new { success = false, message = "Hanya mendukung file dengan format .xlsx." });

            var userId = _currentUser.UserId!;

            await using var stream = file.OpenReadStream();
            var result = await _biayaKesehatanService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Biaya Kesehatan | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}