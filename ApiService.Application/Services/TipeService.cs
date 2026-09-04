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
    public interface ITipeService
    {
        Task<ApiResponse<PagedResponse<TipeDto>>> GetAllAsync(TipeFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<TipeDto>> GetByIdAsync(string id);
        Task<ApiResponse<TipeDto>> CreateAsync(CreateTipeRequest request, string userId);
        Task<ApiResponse<TipeDto>> UpdateAsync(string id, UpdateTipeRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class TipeService : ITipeService
    {
        private readonly IServiceDbContext _context;

        public TipeService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<TipeDto>>> GetAllAsync(TipeFilterRequest filter)
        {
            var query = _context.Tipes
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

            return ApiResponse<PagedResponse<TipeDto>>.SuccessResponse(new PagedResponse<TipeDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Tipes.Where(t => !t.IsDeleted);

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Name.Contains(search));

            var items = await query
                .OrderBy(t => t.Name)
                .Select(t => new MasterLookupDto { Id = t.Id, Name = t.Name })
                .ToListAsync();

            return ApiResponse<List<MasterLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<TipeDto>> GetByIdAsync(string id)
        {
            var tipe = await _context.Tipes
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (tipe == null)
                return ApiResponse<TipeDto>.ErrorResponse("ERR-TIPE-001", "Tipe not found");

            return ApiResponse<TipeDto>.SuccessResponse(MapToDto(tipe));
        }

        public async Task<ApiResponse<TipeDto>> CreateAsync(CreateTipeRequest request, string userId)
        {
            var exists = await _context.Tipes
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<TipeDto>.ErrorResponse("ERR-TIPE-002", "Nama tipe sudah ada");

            var tipe = new MasterTipe
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Tipes.Add(tipe);
            await _context.SaveChangesAsync();

            return ApiResponse<TipeDto>.SuccessResponse(MapToDto(tipe), "Tipe berhasil ditambahkan");
        }

        public async Task<ApiResponse<TipeDto>> UpdateAsync(string id, UpdateTipeRequest request, string userId)
        {
            var tipe = await _context.Tipes
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (tipe == null)
                return ApiResponse<TipeDto>.ErrorResponse("ERR-TIPE-001", "Tipe not found");

            var duplicate = await _context.Tipes
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<TipeDto>.ErrorResponse("ERR-TIPE-002", "Nama tipe sudah ada");

            tipe.Name = request.Name;
            tipe.IsActive = request.IsActive;
            tipe.ModifiedAt = DateTime.UtcNow;
            tipe.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<TipeDto>.SuccessResponse(MapToDto(tipe), "Tipe berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var tipe = await _context.Tipes
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (tipe == null)
                return ApiResponse<bool>.ErrorResponse("ERR-TIPE-001", "Tipe not found");

            // var isUsed = await _context.Pekerjas
            //     .AnyAsync(p => p.TipeId == id && !p.IsDeleted);

            // if (isUsed)
            //     return ApiResponse<bool>.ErrorResponse("ERR-TIPE-003", "Tipe masih dipakai oleh pekerja, tidak bisa dihapus");

            tipe.IsDeleted = true;
            tipe.DeletedAt = DateTime.UtcNow;
            tipe.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Tipe deleted");
        }

        private static TipeDto MapToDto(MasterTipe j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}