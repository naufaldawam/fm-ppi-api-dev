using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using System.Collections.Generic;

namespace ApiService.Application.Services
{
    public interface IVendorService
    {
        Task<ApiResponse<PagedResponse<VendorDto>>> GetAllAsync(VendorFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<VendorDto>> GetByIdAsync(string id);
        Task<ApiResponse<VendorDto>> CreateAsync(CreateVendorRequest request, string userId);
        Task<ApiResponse<VendorDto>> UpdateAsync(string id, UpdateVendorRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class VendorService : IVendorService
    {
        private readonly IServiceDbContext _context;

        public VendorService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<VendorDto>>> GetAllAsync(VendorFilterRequest filter)
        {
            var query = _context.Vendors
                .Where(j => !j.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(j => j.Name.Contains(filter.Search));

            if (filter.IsActive.HasValue)
                query = query.Where(j => j.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(j => j.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<VendorDto>>.SuccessResponse(new PagedResponse<VendorDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Vendors.Where(v => !v.IsDeleted);

            if (activeOnly)
                query = query.Where(v => v.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(v => v.Name.Contains(search));

            var items = await query
                .OrderBy(v => v.Name)
                .Select(v => new MasterLookupDto { Id = v.Id, Name = v.Name })
                .ToListAsync();

            return ApiResponse<List<MasterLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<VendorDto>> GetByIdAsync(string id)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (vendor == null)
                return ApiResponse<VendorDto>.ErrorResponse("ERR-VENDOR-001", "Vendor not found");

            return ApiResponse<VendorDto>.SuccessResponse(MapToDto(vendor));
        }

        public async Task<ApiResponse<VendorDto>> CreateAsync(CreateVendorRequest request, string userId)
        {
            var exists = await _context.Vendors
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<VendorDto>.ErrorResponse("ERR-VENDOR-002", "Nama vendor sudah ada");

            var vendor = new MasterVendor
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Vendors.Add(vendor);
            await _context.SaveChangesAsync();

            return ApiResponse<VendorDto>.SuccessResponse(MapToDto(vendor), "Vendor berhasil ditambahkan");
        }

        public async Task<ApiResponse<VendorDto>> UpdateAsync(string id, UpdateVendorRequest request, string userId)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (vendor == null)
                return ApiResponse<VendorDto>.ErrorResponse("ERR-VENDOR-001", "Vendor not found");

            var duplicate = await _context.Vendors
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<VendorDto>.ErrorResponse("ERR-VENDOR-002", "Nama vendor sudah ada");

            vendor.Name = request.Name;
            vendor.IsActive = request.IsActive;
            vendor.ModifiedAt = DateTime.UtcNow;
            vendor.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<VendorDto>.SuccessResponse(MapToDto(vendor), "Vendor berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (vendor == null)
                return ApiResponse<bool>.ErrorResponse("ERR-VENDOR-001", "Vendor not found");

            // var isUsed = await _context.Pekerjas
            //     .AnyAsync(p => p.VendorId == id && !p.IsDeleted);

            // if (isUsed)
            //     return ApiResponse<bool>.ErrorResponse("ERR-VENDOR-003", "Vendor masih dipakai oleh pekerja, tidak bisa dihapus");

            vendor.IsDeleted = true;
            vendor.DeletedAt = DateTime.UtcNow;
            vendor.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Vendor deleted");
        }

        private static VendorDto MapToDto(MasterVendor j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}