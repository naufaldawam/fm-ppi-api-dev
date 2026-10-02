using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.API.Filters;
using ApiService.Application.DTOs.Mobile;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;

namespace ApiService.API.Controllers.Mobile
{
    [Tags("Mobile Inspeksi Kendaraan")]
    [ApiController]
    [Route("mobile/inspeksi")]
    [Authorize]
    [Produces("application/json")]
    public class InspeksiKendaraController(
        IInspeksiKendaraanService inspeksiService,
        IDriverService driverService,
        IKendaraanService kendaraanService,
        ICurrentUser currentUser,
        ILogger<InspeksiKendaraController> logger) : ControllerBase
    {
        private readonly IInspeksiKendaraanService _inspeksiService = inspeksiService;
        private readonly IDriverService _driverService = driverService;
        private readonly IKendaraanService _kendaraanService = kendaraanService;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<InspeksiKendaraController> _logger = logger;

        [HttpGet]
        [RequirePermission("inspeksi.read")]
        public async Task<IActionResult> GetAll([FromQuery] InspeksiKendaraanFilterRequest filter)
        {
            var result = await _inspeksiService.GetAllAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("driver-lookup")]
        [RequirePermission("inspeksi.read")]
        public async Task<IActionResult> GetDriverLookup(
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _driverService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("kendaraan-lookup")]
        [RequirePermission("inspeksi.read")]
        public async Task<IActionResult> GetKendaraanLookup(
            [FromQuery] string? search,
            [FromQuery] bool activeOnly = true)
        {
            var result = await _kendaraanService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("inspeksi.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _inspeksiService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [RequirePermission("inspeksi.create")]
        public async Task<IActionResult> Create([FromBody] CreateInspeksiKendaraanRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _inspeksiService.CreateAsync(request, userId);

            _logger.LogInformation(
                "Inspeksi kendaraan created. KendaraanId: {KendaraanId} | DriverId: {DriverId} | Tanggal: {Tanggal} | Kelayakan: {Kelayakan} | UserId: {UserId}",
                request.KendaraanId,
                request.DriverId,
                request.TanggalInspeksi,
                result.Data?.KelayakanJalan,
                userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [RequirePermission("inspeksi.update")]
        public async Task<IActionResult> Update(
            string id,
            [FromBody] UpdateInspeksiKendaraanRequest request)
        {
            var result = await _inspeksiService.UpdateAsync(
                id,
                request,
                _currentUser.UserId!);

            _logger.LogInformation(
                "Inspeksi kendaraan updated. Id: {Id} | KendaraanId: {KendaraanId} | DriverId: {DriverId} | UserId: {UserId}",
                id,
                request.KendaraanId,
                request.DriverId,
                _currentUser.UserId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("inspeksi.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _inspeksiService.DeleteAsync(id, _currentUser.UserId!);
            return StatusCode(result.StatusCode, result);
        }
    }
}
