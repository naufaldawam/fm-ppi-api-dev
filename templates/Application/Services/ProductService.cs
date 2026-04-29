using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyService.Application.DTOs;
using MyService.Application.Interfaces;
using MyService.Domain.Entities;

namespace MyService.Application.Services
{
    // ===================================
    // EXAMPLE SERVICE INTERFACE
    // Copy this pattern for each new resource.
    // Rename "Product" to your entity name.
    // ===================================
    public interface IProductService
    {
        Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(ProductFilterRequest filter);
        Task<ApiResponse<ProductDto>> GetByIdAsync(string id);
        Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, string userId);
        Task<ApiResponse<ProductDto>> UpdateAsync(string id, UpdateProductRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    // ===================================
    // EXAMPLE SERVICE IMPLEMENTATION
    // ===================================
    public class ProductService : IProductService
    {
        private readonly IServiceDbContext _context;

        public ProductService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(ProductFilterRequest filter)
        {
            var query = _context.Products
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p =>
                    p.Name.Contains(filter.Search) ||
                    (p.Description != null && p.Description.Contains(filter.Search)));

            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(p => p.Category == filter.Category);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<ProductDto>>.SuccessResponse(new PagedResponse<ProductDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<ProductDto>> GetByIdAsync(string id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                return ApiResponse<ProductDto>.ErrorResponse("ERR-PROD-001", "Product not found");

            return ApiResponse<ProductDto>.SuccessResponse(MapToDto(product));
        }

        public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductRequest request, string userId)
        {
            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                Category = request.Category,
                IsActive = true,
                OwnerId = userId,
                CreatedBy = userId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return ApiResponse<ProductDto>.SuccessResponse(MapToDto(product), "Product created");
        }

        public async Task<ApiResponse<ProductDto>> UpdateAsync(string id, UpdateProductRequest request, string userId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                return ApiResponse<ProductDto>.ErrorResponse("ERR-PROD-001", "Product not found");

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.Category = request.Category;
            product.IsActive = request.IsActive;
            product.ModifiedAt = DateTime.UtcNow;
            product.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<ProductDto>.SuccessResponse(MapToDto(product), "Product updated");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
                return ApiResponse<bool>.ErrorResponse("ERR-PROD-001", "Product not found");

            // Soft delete
            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;
            product.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Product deleted");
        }

        private static ProductDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            IsActive = p.IsActive,
            Category = p.Category,
            OwnerId = p.OwnerId,
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt
        };
    }
}
