using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>
    /// Data Kecelakaan - laporan kecelakaan + foto bukti (1 form, submit langsung).
    /// Create/Update pakai multipart: fields + files[] (nama field foto = "files").
    /// </summary>
    [ApiController]
    [Route("data-kecelakaan")]
    [Authorize]
    [Produces("application/json")]
    public class DataKecelakaanController : ControllerBase
    {
        private readonly IDataKecelakaanService _dataKecelakaanService;
        private readonly IKategoriKecelakaanService _kategoriKecelakaanService;
        private readonly IPekerjaService _pekerjaService;
        private readonly IDriverService _driverService;
        private readonly IPeriodeService _periodeService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<DataKecelakaanController> _logger;

        public DataKecelakaanController(
            IDataKecelakaanService dataKecelakaanService,
            IKategoriKecelakaanService kategoriKecelakaanService,
            IPekerjaService pekerjaService,
            IDriverService driverService,
            IPeriodeService periodeService,
            ICurrentUser currentUser,
            ILogger<DataKecelakaanController> logger)
        {
            _dataKecelakaanService = dataKecelakaanService;
            _kategoriKecelakaanService = kategoriKecelakaanService;
            _pekerjaService = pekerjaService;
            _driverService = driverService;
            _periodeService = periodeService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetAll([FromQuery] DataKecelakaanFilterRequest filter)
        {
            var result = await _dataKecelakaanService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Dropdown Kategori Kecelakaan: GET /data-kecelakaan/kategori-lookup?search=xxx</summary>
        [HttpGet("kategori-lookup")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetKategoriLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _kategoriKecelakaanService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        /// <summary>Dropdown Pejabat (Pekerja): GET /data-kecelakaan/pekerja-lookup?search=xxx</summary>
        [HttpGet("pekerja-lookup")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] string? jabatanId, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, jabatanId, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Dropdown Driver: GET /data-kecelakaan/driver-lookup?search=xxx</summary>
        [HttpGet("driver-lookup")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetDriverLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _driverService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        /// <summary>Dropdown Periode: GET /data-kecelakaan/periode-lookup?search=xxx</summary>
        [HttpGet("periode-lookup")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>Summary box: total laporan, split status, total foto bukti.</summary>
        [HttpGet("summary")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetSummary([FromQuery] DataKecelakaanSummaryRequest filter)
        {
            var result = await _dataKecelakaanService.GetSummaryAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _dataKecelakaanService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>
        /// Submit data kecelakaan (satu form multipart):
        /// fields (nomor, judul, kategoriId, tanggalKejadian, waktuKejadian, ...) + files[] (0..N foto).
        /// Status langsung "Published" (submit langsung).
        /// </summary>
        [HttpPost]
        [RequirePermission("data-kecelakaan.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateDataKecelakaanRequest request, [FromForm] List<IFormFile>? files)
        {
            var userId = _currentUser.UserId!;
            var result = await _dataKecelakaanService.CreateAsync(request, files, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Data kecelakaan {Nomor} created by user {UserId}", request.Nomor, userId);
            return Ok(result);
        }

        /// <summary>
        /// Update laporan (multipart): fields + files[] foto BARU yang di-append.
        /// Foto lama di-manage via DELETE /data-kecelakaan/evidence/{evidenceId}.
        /// </summary>
        [HttpPut("{id}")]
        [RequirePermission("data-kecelakaan.update")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(50 * 1024 * 1024)]
        public async Task<IActionResult> Update(string id, [FromForm] UpdateDataKecelakaanRequest request, [FromForm] List<IFormFile>? files)
        {
            var result = await _dataKecelakaanService.UpdateAsync(id, request, files, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("data-kecelakaan.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _dataKecelakaanService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Tambah 1 foto bukti ke laporan (untuk tombol "+ Tambahkan Foto").
        /// POST /data-kecelakaan/{id}/evidence  (multipart, field "file")
        /// </summary>
        [HttpPost("{id}/evidence")]
        [RequirePermission("evidence-kecelakaan.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadEvidence(string id, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            var result = await _dataKecelakaanService.UploadEvidenceAsync(id, file, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// Stream foto bukti (byte[]) sehingga FE bisa load image via API.
        /// GET /data-kecelakaan/evidence/{evidenceId}/image
        /// </summary>
        [HttpGet("evidence/{evidenceId}/image")]
        [RequirePermission("evidence-kecelakaan.read")]
        public async Task<IActionResult> GetEvidenceImage(string evidenceId)
        {
            var result = await _dataKecelakaanService.GetEvidenceImageAsync(evidenceId);
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.Bytes, file.ContentType, file.FileName);
        }

        /// <summary>Generate PDF laporan kecelakaan. GET /data-kecelakaan/{id}/generate-pdf</summary>
        [HttpGet("{id}/generate-pdf")]
        [RequirePermission("data-kecelakaan.read")]
        public async Task<IActionResult> GeneratePdf(string id)
        {
            var result = await _dataKecelakaanService.GeneratePdfAsync(id);
            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            return File(result.Data.FileStream, result.Data.ContentType, result.Data.FileName);
        }

        /// <summary>
        /// Hapus 1 foto bukti (tombol hapus foto di form edit).
        /// DELETE /data-kecelakaan/evidence/{evidenceId}
        /// </summary>
        [HttpDelete("evidence/{evidenceId}")]
        [RequirePermission("evidence-kecelakaan.delete")]
        public async Task<IActionResult> DeleteEvidence(string evidenceId)
        {
            var result = await _dataKecelakaanService.DeleteEvidenceAsync(evidenceId, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}