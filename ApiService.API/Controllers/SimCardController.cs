using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>SIM Card - datatable + form (Pekerja, Periode, Biaya SIM Card).</summary>
    [ApiController]
    [Route("sim-card")]
    [Authorize]
    [Produces("application/json")]
    public class SimCardController : ControllerBase
    {
        private readonly ISimCardService _simCardService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<SimCardController> _logger;

        public SimCardController(
            ISimCardService simCardService,
            IPekerjaService pekerjaService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<SimCardController> logger)
        {
            _simCardService = simCardService;
            _pekerjaService = pekerjaService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> GetAll([FromQuery] SimCardFilterRequest filter)
        {
            var result = await _simCardService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Dropdown Pekerja: GET /sim-card/pekerja-lookup?search=xxx</summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] string? jabatanId, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, jabatanId, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Dropdown Periode: GET /sim-card/periode-lookup?search=xxx</summary>
        [HttpGet("periode-lookup")]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Summary box: total SIM card pekerja + total biaya.</summary>
        [HttpGet("summary")]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> GetSummary([FromQuery] SimCardSummaryRequest filter)
        {
            var result = await _simCardService.GetSummaryAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _simCardService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("sim-card.create")]
        public async Task<IActionResult> Create([FromBody] CreateSimCardRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _simCardService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Sim card created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("sim-card.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateSimCardRequest request)
        {
            var result = await _simCardService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("sim-card.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _simCardService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>Download template: GET /sim-card/download-template</summary>
        [HttpGet("download-template")]
        [RequirePermission("sim-card.read")]
        public async Task<IActionResult> DownloadTemplate()
        {
            _logger.LogInformation("User {UserId} download template SIM Card.", _currentUser.UserId);

            var result = await _simCardService.GetImportTemplateAsync();
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.FileStream, file.ContentType, file.FileName);
        }

        /// <summary>
        /// Bulk upload SIM Card (.xlsx). Kolom: NoPekerja | BiayaSimCard
        /// POST /sim-card/import-excel
        /// </summary>
        [HttpPost("import-excel")]
        [RequirePermission("sim-card.create")]
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
            var result = await _simCardService.ImportFromExcelAsync(stream, userId);

            _logger.LogInformation(
                "Import SIM Card | File: {FileName} | Size: {FileSize} | UserId: {UserId} | Success: {Success}",
                file.FileName, file.Length, userId, result.Success);

            return StatusCode(result.StatusCode, result);
        }
    }
}