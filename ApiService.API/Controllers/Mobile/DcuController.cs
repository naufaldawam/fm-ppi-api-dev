using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ApiService.API.Filters;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services.Mobile;
using ApiService.Application.Services;
using ApiService.Application.DTOs.Mobile;

namespace ApiService.API.Controllers.Mobile
{
    [Tags("Mobile Daily Check Up (DCU)")]
    [ApiController]
    [Route("mobile/dcu")]
    [Authorize]
    [Produces("application/json")]
    public class DcuController(
        IDcuService dcuService, 
        IDriverService driverService,
        ICurrentUser currentUser,
        ILogger<DcuController> logger) : ControllerBase
    {
        private readonly IDcuService _dcuService = dcuService;
        private readonly IDriverService _driverService = driverService;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<DcuController> _logger = logger;

        [HttpGet]
        [RequirePermission("dcu.read")]
        public async Task<IActionResult> GetAll([FromQuery] DataDcuFilterRequest filter)
        {
            var result = await _dcuService.GetAllAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("driver-lookup")]
        [RequirePermission("dcu.read")]
        public async Task<IActionResult> GetDriverLookup(
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _driverService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("dcu.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _dcuService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [RequirePermission("dcu.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Create(
            [FromForm] CreateDcuRequest request,
            [FromForm] IFormFile photo)
        {
            var userId = _currentUser.UserId!;

            var result = await _dcuService.CreateAsync(request, photo, userId);

            _logger.LogInformation(
                "DCU created. DriverId: {DriverId} | TanggalDcu: {TanggalDcu} | StatusKesehatan: {StatusKesehatan} | UserId: {UserId}",
                request.DriverId,
                request.TanggalDcu,
                request.StatusKesehatan,
                userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [RequirePermission("dcu.update")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Update(
            string id,
            [FromForm] UpdateDcuRequest request,
            [FromForm] IFormFile? photo)
        {
            var result = await _dcuService.UpdateAsync(id, request, photo, _currentUser.UserId!);
            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("dcu.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _dcuService.DeleteAsync(id, _currentUser.UserId!);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost("{id}/evidence")]
        [RequirePermission("evidence-dcu.create")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadEvidence(
            string id,
            [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File tidak boleh kosong." });

            var result = await _dcuService.UploadEvidenceAsync(
                id,
                file,
                _currentUser.UserId!);

            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("evidence/{evidenceId}/image")]
        [RequirePermission("evidence-dcu.read")]
        public async Task<IActionResult> GetEvidenceImage(string evidenceId)
        {
            var result = await _dcuService.GetEvidenceImageAsync(evidenceId);

            if (!result.Success || result.Data == null)
                return StatusCode(result.StatusCode, result);

            var file = result.Data;
            return File(file.Bytes, file.ContentType, file.FileName);
        }

        [HttpDelete("evidence/{evidenceId}")]
        [RequirePermission("evidence-dcu.delete")]
        public async Task<IActionResult> DeleteEvidence(string evidenceId)
        {
            var result = await _dcuService.DeleteEvidenceAsync(
                evidenceId,
                _currentUser.UserId!);

            return StatusCode(result.StatusCode, result);
        }
    }
}
