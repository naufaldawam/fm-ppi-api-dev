using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Application.Services;
using ApiService.API.Filters;

namespace ApiService.API.Controllers
{
    /// <summary>Master data Vendor (dropdown di form Kendaraan)</summary>
    [ApiController]
    [Route("api/vendor")]
    [Authorize]
    [Produces("application/json")]
    public class VendorController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<VendorController> _logger;

        public VendorController(
            IVendorService vendorService,
            ICurrentUser currentUser,
            ILogger<VendorController> logger)
        {
            _vendorService = vendorService;
            _currentUser = currentUser;
            _logger = logger;
        }

        [HttpGet]
        [RequirePermission("vendor.read")]
        public async Task<IActionResult> GetAll([FromQuery] VendorFilterRequest filter)
        {
            var result = await _vendorService.GetAllAsync(filter);
            return Ok(result);
        }

        [HttpGet("lookup")]
        [RequirePermission("vendor.read")]
        public async Task<IActionResult> GetLookup([FromQuery] string? search, [FromQuery] bool activeOnly = true)
        {
            var result = await _vendorService.GetLookupAsync(search, activeOnly);
            return Ok(result);
        }

        [HttpGet("{id}")]
        [RequirePermission("vendor.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _vendorService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpPost]
        [RequirePermission("vendor.create")]
        public async Task<IActionResult> Create([FromBody] CreateVendorRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _vendorService.CreateAsync(request, userId);
            if (!result.Success) return BadRequest(result);

            _logger.LogInformation("Vendor created by user {UserId}", userId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [RequirePermission("vendor.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateVendorRequest request)
        {
            var result = await _vendorService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [RequirePermission("vendor.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _vendorService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}