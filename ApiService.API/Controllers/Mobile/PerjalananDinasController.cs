using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.API.Filters;
using ApiService.Application.DTOs.Mobile;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.Application.Services.Mobile;

namespace ApiService.API.Controllers.Mobile
{
    [Tags("Mobile Perjalanan Dinas")]
    [ApiController]
    [Route("mobile/perjalanan-dinas")]
    [Authorize]
    [Produces("application/json")]
    public class PerjalananDinasController(IPerjalananDinasMobileService perjalananDinasService, IPekerjaService pekerjaService, IPeriodeService periodeService, ICurrentUser currentUser, ILogger<PerjalananDinasController> logger) : ControllerBase
    {
        private readonly IPerjalananDinasMobileService _perjalananDinasService = perjalananDinasService;
        private readonly IPekerjaService _pekerjaService = pekerjaService;
        private readonly IPeriodeService _periodeService = periodeService;
        private readonly ICurrentUser _currentUser = currentUser;
        private readonly ILogger<PerjalananDinasController> _logger = logger;

        [HttpGet]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetAll([FromQuery] PerjalananDinasMobileFilterRequest filter)
        {
            var result = await _perjalananDinasService.GetAllAsync(filter);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("pekerja-lookup")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetPekerjaLookup([FromQuery] string? search, [FromQuery] string? jabatanId, [FromQuery] bool activeOnly = true)
        {
            var result = await _pekerjaService.GetLookupAsync(search, jabatanId, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("periode-lookup")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetPeriodeLookup([FromQuery] string? search, [FromQuery] bool activeOnly = false)
        {
            var result = await _periodeService.GetLookupAsync(search, activeOnly);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("{id}")]
        [RequirePermission("perjalanan-dinas.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _perjalananDinasService.GetByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        [HttpPost]
        [RequirePermission("perjalanan-dinas.create")]
        public async Task<IActionResult> Create([FromBody] CreatePerjalananDinasMobileRequest request)
        {
            var userId = _currentUser.UserId!;

            var result = await _perjalananDinasService.CreateAsync(request, userId);

            _logger.LogInformation(
                "Perjalanan Dinas mobile created. PekerjaId: {PekerjaId} | PeriodeId: {PeriodeId} | UserId: {UserId}",
                request.PekerjaId,
                request.PeriodeId,
                userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpPut("{id}")]
        [RequirePermission("perjalanan-dinas.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdatePerjalananDinasMobileRequest request)
        {
            var userId = _currentUser.UserId!;

            var result = await _perjalananDinasService.UpdateAsync(id, request, userId);

            _logger.LogInformation(
                "Perjalanan Dinas mobile updated. Id: {Id} | PekerjaId: {PekerjaId} | PeriodeId: {PeriodeId} | UserId: {UserId}",
                id,
                request.PekerjaId,
                request.PeriodeId,
                userId);

            return StatusCode(result.StatusCode, result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("perjalanan-dinas.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = _currentUser.UserId!;
            var result = await _perjalananDinasService.DeleteAsync(id, userId);

            _logger.LogInformation(
                "Perjalanan Dinas mobile deleted. Id: {Id} | UserId: {UserId}",
                id,
                userId);

            return StatusCode(result.StatusCode, result);
        }
    }
}
