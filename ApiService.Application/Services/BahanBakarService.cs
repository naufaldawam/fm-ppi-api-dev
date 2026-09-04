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
    public interface IBahanBakarService
    {
        Task<ApiResponse<PagedResponse<BahanBakarDto>>> GetAllAsync(BahanBakarFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<BahanBakarDto>> GetByIdAsync(string id);
        Task<ApiResponse<BahanBakarDto>> CreateAsync(CreateBahanBakarRequest request, string userId);
        Task<ApiResponse<BahanBakarDto>> UpdateAsync(string id, UpdateBahanBakarRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class BahanBakarService : IBahanBakarService
    {
        private readonly IServiceDbContext _context;

        public BahanBakarService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<BahanBakarDto>>> GetAllAsync(BahanBakarFilterRequest filter)
        {
            var query = _context.BahanBakars
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

            return ApiResponse<PagedResponse<BahanBakarDto>>.SuccessResponse(new PagedResponse<BahanBakarDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.BahanBakars.Where(b => !b.IsDeleted);

            if (activeOnly)
                query = query.Where(b => b.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Name.Contains(search));

            var items = await query
                .OrderBy(b => b.Name)
                .Select(b => new MasterLookupDto { Id = b.Id, Name = b.Name })
                .ToListAsync();

            return ApiResponse<List<MasterLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<BahanBakarDto>> GetByIdAsync(string id)
        {
            var bahanBakar = await _context.BahanBakars
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (bahanBakar == null)
                return ApiResponse<BahanBakarDto>.ErrorResponse("ERR-BAHANBAKAR-001", "BahanBakar not found");

            return ApiResponse<BahanBakarDto>.SuccessResponse(MapToDto(bahanBakar));
        }

        public async Task<ApiResponse<BahanBakarDto>> CreateAsync(CreateBahanBakarRequest request, string userId)
        {
            var exists = await _context.BahanBakars
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<BahanBakarDto>.ErrorResponse("ERR-BAHANBAKAR-002", "Nama bahanBakar sudah ada");

            var bahanBakar = new MasterBahanBakar
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.BahanBakars.Add(bahanBakar);
            await _context.SaveChangesAsync();

            return ApiResponse<BahanBakarDto>.SuccessResponse(MapToDto(bahanBakar), "BahanBakar berhasil ditambahkan");
        }

        public async Task<ApiResponse<BahanBakarDto>> UpdateAsync(string id, UpdateBahanBakarRequest request, string userId)
        {
            var bahanBakar = await _context.BahanBakars
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (bahanBakar == null)
                return ApiResponse<BahanBakarDto>.ErrorResponse("ERR-BAHANBAKAR-001", "BahanBakar not found");

            var duplicate = await _context.BahanBakars
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<BahanBakarDto>.ErrorResponse("ERR-BAHANBAKAR-002", "Nama bahanBakar sudah ada");

            bahanBakar.Name = request.Name;
            bahanBakar.IsActive = request.IsActive;
            bahanBakar.ModifiedAt = DateTime.UtcNow;
            bahanBakar.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<BahanBakarDto>.SuccessResponse(MapToDto(bahanBakar), "BahanBakar berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var bahanBakar = await _context.BahanBakars
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (bahanBakar == null)
                return ApiResponse<bool>.ErrorResponse("ERR-BAHANBAKAR-001", "BahanBakar not found");

            // var isUsed = await _context.Pekerjas
            //     .AnyAsync(p => p.BahanBakarId == id && !p.IsDeleted);

            // if (isUsed)
            //     return ApiResponse<bool>.ErrorResponse("ERR-BAHANBAKAR-003", "BahanBakar masih dipakai oleh pekerja, tidak bisa dihapus");

            bahanBakar.IsDeleted = true;
            bahanBakar.DeletedAt = DateTime.UtcNow;
            bahanBakar.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "BahanBakar deleted");
        }

        private static BahanBakarDto MapToDto(MasterBahanBakar j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}