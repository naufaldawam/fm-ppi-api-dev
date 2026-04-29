using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyService.Application.DTOs;
using MyService.Application.Interfaces;
using MyService.Application.Services;
using MyService.API.Filters;

namespace MyService.API.Controllers
{
    /// <summary>
    /// EXAMPLE CONTROLLER — Copy this pattern for any new resource.
    ///
    /// Access control options (use any combination):
    ///   [Authorize]                             — must be logged in
    ///   [Authorize(Roles = "Admin")]            — must have Admin role
    ///   [RequirePermission("products.read")]    — must have specific permission from AuthService JWT
    /// </summary>
    [ApiController]
    [Route("api/products")]
    [Authorize]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ICurrentUser _currentUser;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            IProductService productService,
            ICurrentUser currentUser,
            ILogger<ProductsController> logger)
        {
            _productService = productService;
            _currentUser = currentUser;
            _logger = logger;
        }

        /// <summary>Get all products (paginated + filtered)</summary>
        [HttpGet]
        [RequirePermission("products.read")]
        public async Task<IActionResult> GetAll([FromQuery] ProductFilterRequest filter)
        {
            var result = await _productService.GetAllAsync(filter);
            return Ok(result);
        }

        /// <summary>Get product by ID</summary>
        [HttpGet("{id}")]
        [RequirePermission("products.read")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _productService.GetByIdAsync(id);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>Create a new product</summary>
        [HttpPost]
        [RequirePermission("products.create")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            var userId = _currentUser.UserId!;
            var result = await _productService.CreateAsync(request, userId);
            _logger.LogInformation("Product created by user {UserId}", userId);
            return Ok(result);
        }

        /// <summary>Update a product</summary>
        [HttpPut("{id}")]
        [RequirePermission("products.update")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateProductRequest request)
        {
            var result = await _productService.UpdateAsync(id, request, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        /// <summary>Delete a product (soft delete)</summary>
        [HttpDelete("{id}")]
        [RequirePermission("products.delete")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _productService.DeleteAsync(id, _currentUser.UserId!);
            if (!result.Success) return NotFound(result);
            return Ok(result);
        }

        // ===================================
        // EXAMPLE: WHO AM I?
        // Shows the claims extracted from AuthService JWT
        // Useful for debugging — remove in production
        // ===================================
        /// <summary>Show current user info from JWT (debug)</summary>
        [HttpGet("_whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(new
            {
                userId = _currentUser.UserId,
                email = _currentUser.Email,
                roles = _currentUser.Roles,
                permissions = _currentUser.Permissions,
                isAuthenticated = _currentUser.IsAuthenticated
            });
        }
    }
}
