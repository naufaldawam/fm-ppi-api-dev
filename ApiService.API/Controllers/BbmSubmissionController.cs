using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    [ApiController]
    [Route("mobile/bbm-submission")]
    [Authorize]
    [Produces("application/json")]
    public class BbmSubmissionController : ControllerBase
    {
        private readonly IBbmSubmissionService _service;
        private readonly IFileService _fileService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<BbmSubmissionController> _logger;

        private const long MaxPhotoBytes = 5 * 1024 * 1024;
        private static readonly string[] AllowedPhotoTypes = ["image/jpeg", "image/png"];

        public BbmSubmissionController(
            IBbmSubmissionService service,
            IFileService fileService,
            ICurrentUser currentUser,
            ILogger<BbmSubmissionController> logger)
        {
            _service = service;
            _fileService = fileService;
            _currentUser = currentUser;
            _logger = logger;
        }

        // GET /mobile/bbm-submission/form-init/{driverId}
        // Prefill data Step 1 + dropdown kendaraan untuk Step 2.
        // Dipanggil mobile saat pertama kali buka form wizard.
        [HttpGet("form-init/{driverId}")]
        [RequirePermission("bbm-submission.read")]
        public async Task<IActionResult> GetFormInit(string driverId)
        {
            var result = await _service.GetFormInitAsync(driverId);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // GET /mobile/bbm-submission
        // Daftar semua submission dengan filter (admin / atasan).
        [HttpGet]
        [RequirePermission("bbm-submission.read")]
        public async Task<IActionResult> GetAll([FromQuery] BbmSubmissionFilterRequest filter)
        {
            var result = await _service.GetAllAsync(filter);
            return Ok(result);
        }

        // GET /bbm-submission/{id}
        [HttpGet("{id}")]
        [RequirePermission("bbm-submission.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _service.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("bbm-submission.create")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create(
            [FromForm] CreateBbmSubmissionRequest request,
            IFormFile fotoOdometer,
            IFormFile fotoNota)
        {
            // ── Validasi foto ────────────────────────────────────────────────────
            var validationError = ValidatePhoto(fotoOdometer, "Foto Odometer")
                                  ?? ValidatePhoto(fotoNota, "Foto Nota");
            if (validationError != null) return BadRequest(validationError);

            var userId = _currentUser.UserId!;

            var fotoOdometerResult = await _fileService.UploadImageAsync(fotoOdometer, userId);
            if (!fotoOdometerResult.Success) return BadRequest(fotoOdometerResult);

            var fotoNotaResult = await _fileService.UploadImageAsync(fotoNota, userId);
            if (!fotoNotaResult.Success) return BadRequest(fotoNotaResult);

            var result = await _service.CreateAsync(
                request,
                fotoOdometerResult.Data!.Url,
                fotoNotaResult.Data!.Url,
                userId);

            if (!result.Success) return BadRequest(result);

            _logger.LogInformation(
                "BbmSubmission created for driver {DriverId} by user {UserId}",
                request.DriverId, userId);

            return Ok(result);
        }

        // POST /bbm-submission/{id}/approve
        [HttpPost("{id}/approve")]
        [RequirePermission("bbm-submission.approve")]
        public async Task<IActionResult> Approve(string id)
        {
            var approverUserId = _currentUser.UserId!;
            var result = await _service.ApproveAsync(id, approverUserId);

            if (!result.Success) return BadRequest(result);

            _logger.LogInformation(
                "BbmSubmission {Id} approved by user {UserId}", id, approverUserId);

            return Ok(result);
        }

        // POST /bbm-submission/{id}/reject
        [HttpPost("{id}/reject")]
        [RequirePermission("bbm-submission.approve")]
        public async Task<IActionResult> Reject(string id, [FromBody] RejectBbmSubmissionRequest request)
        {
            var approverUserId = _currentUser.UserId!;
            var result = await _service.RejectAsync(id, request, approverUserId);

            if (!result.Success) return BadRequest(result);

            _logger.LogInformation(
                "BbmSubmission {Id} rejected by user {UserId}. Reason: {Reason}",
                id, approverUserId, request.Reason);

            return Ok(result);
        }

        // DELETE /bbm-submission/{id}
        [HttpDelete("{id}")]
        [RequirePermission("bbm-submission.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _service.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // HELPERS

        /// <summary>Validasi ukuran dan tipe file foto.</summary>
        private static ApiResponse<object>? ValidatePhoto(IFormFile? file, string fieldName)
        {
            if (file == null || file.Length == 0)
                return ApiResponse<object>.ErrorResponse(
                    "ERR-BBM-UPLOAD-001", $"{fieldName} wajib diunggah");

            if (file.Length > MaxPhotoBytes)
                return ApiResponse<object>.ErrorResponse(
                    "ERR-BBM-UPLOAD-002", $"{fieldName} melebihi batas ukuran 5 MB");

            if (!System.Array.Exists(AllowedPhotoTypes, t => t == file.ContentType.ToLower()))
                return ApiResponse<object>.ErrorResponse(
                    "ERR-BBM-UPLOAD-003", $"{fieldName} harus berformat JPG atau PNG");

            return null;
        }

    }
}