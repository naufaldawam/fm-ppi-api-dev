using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Perjalanan Dinas - datatable + form (Pekerja, Bulan Tahun, Periode, Total Biaya).</summary>
    [ApiController]
    [Route("perjalanan-dinas")]
    [Authorize]
    [Produces("application/json")]
    public class PerjalananDinasController : ControllerBase
    {
        private readonly IPerjalananDinasService _perjalananDinasService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<PerjalananDinasController> _logger;

        public PerjalananDinasController(
            IPerjalananDinasService perjalananDinasService,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<PerjalananDinasController> logger)
        {
            _perjalananDinasService = perjalananDinasService;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetAll([FromQuery] PerjalananDinasFilterRequest filter)
        {
            var result = await _perjalananDinasService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Dropdown Pekerja: GET /perjalanan-dinas/pekerja-lookup?search=xxx</summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] string? jabatanId, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, jabatanId, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Dropdown Periode: GET /perjalanan-dinas/periode-lookup?search=xxx</summary>
        [HttpGet("periode-lookup")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Summary box: total dinas pekerja (count) + total perjalanan dinas (sum), filter by periode.</summary>
        [HttpGet("summary")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetSummary([FromQuery] PerjalananDinasSummaryRequest filter)
        {
            var result = await _perjalananDinasService.GetSummaryAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _perjalananDinasService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("perjalanan-dinas.create")]
        public async Task<IActionResult> Create([FromBody] CreatePerjalananDinasRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _perjalananDinasService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Perjalanan dinas created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("perjalanan-dinas.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePerjalananDinasRequest request)
        {
            var result = await _perjalananDinasService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("perjalanan-dinas.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _perjalananDinasService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template: GET /perjalanan-dinas/download-template</summary>
        [HttpGet("download-template")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template Perjalanan Dinas.", _currentUser.UserId);

            var result = await _perjalananDinasService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload Perjalanan Dinas (.xlsx).
        /// Kolom: NoPekerja | BulanTahun | Periode | TotalBiayaDinas
        /// POST /perjalanan-dinas/import-excel
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("perjalanan-dinas.create")]
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
            var result = await _perjalananDinasService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import Perjalanan Dinas | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}