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
    public interface IKepemilikanService
    {
        Task<ApiResponse<PagedResponse<KepemilikanDto>>> GetAllAsync(KepemilikanFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<KepemilikanDto>> GetByIdAsync(string id);
        Task<ApiResponse<KepemilikanDto>> CreateAsync(CreateKepemilikanRequest request, string userId);
        Task<ApiResponse<KepemilikanDto>> UpdateAsync(string id, UpdateKepemilikanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class KepemilikanService : IKepemilikanService
    {
        private readonly IServiceDbContext _context;

        public KepemilikanService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<KepemilikanDto>>> GetAllAsync(KepemilikanFilterRequest filter)
        {
            var query = _context.Kepemilikans
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

            return ApiResponse<PagedResponse<KepemilikanDto>>.SuccessResponse(new PagedResponse<KepemilikanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Kepemilikans.Where(k => !k.IsDeleted);

            if (activeOnly)
                query = query.Where(k => k.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(k => k.Name.Contains(search));

            var items = await query
                .OrderBy(k => k.Name)
                .Select(k => new MasterLookupDto { Id = k.Id, Name = k.Name })
                .ToListAsync();

            return ApiResponse<List<MasterLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<KepemilikanDto>> GetByIdAsync(string id)
        {
            var kepemilikan = await _context.Kepemilikans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (kepemilikan == null)
                return ApiResponse<KepemilikanDto>.ErrorResponse("ERR-KEPEMILIKAN-001", "Kepemilikan not found");

            return ApiResponse<KepemilikanDto>.SuccessResponse(MapToDto(kepemilikan));
        }

        public async Task<ApiResponse<KepemilikanDto>> CreateAsync(CreateKepemilikanRequest request, string userId)
        {
            var exists = await _context.Kepemilikans
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<KepemilikanDto>.ErrorResponse("ERR-KEPEMILIKAN-002", "Nama kepemilikan sudah ada");

            var kepemilikan = new MasterKepemilikan
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Kepemilikans.Add(kepemilikan);
            await _context.SaveChangesAsync();

            return ApiResponse<KepemilikanDto>.SuccessResponse(MapToDto(kepemilikan), "Kepemilikan berhasil ditambahkan");
        }

        public async Task<ApiResponse<KepemilikanDto>> UpdateAsync(string id, UpdateKepemilikanRequest request, string userId)
        {
            var kepemilikan = await _context.Kepemilikans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (kepemilikan == null)
                return ApiResponse<KepemilikanDto>.ErrorResponse("ERR-KEPEMILIKAN-001", "Kepemilikan not found");

            var duplicate = await _context.Kepemilikans
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<KepemilikanDto>.ErrorResponse("ERR-KEPEMILIKAN-002", "Nama kepemilikan sudah ada");

            kepemilikan.Name = request.Name;
            kepemilikan.IsActive = request.IsActive;
            kepemilikan.ModifiedAt = DateTime.UtcNow;
            kepemilikan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<KepemilikanDto>.SuccessResponse(MapToDto(kepemilikan), "Kepemilikan berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var kepemilikan = await _context.Kepemilikans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (kepemilikan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-KEPEMILIKAN-001", "Kepemilikan not found");

            // var isUsed = await _context.Pekerjas
            //     .AnyAsync(p => p.KepemilikanId == id && !p.IsDeleted);

            // if (isUsed)
            //     return ApiResponse<bool>.ErrorResponse("ERR-KEPEMILIKAN-003", "Kepemilikan masih dipakai oleh pekerja, tidak bisa dihapus");

            kepemilikan.IsDeleted = true;
            kepemilikan.DeletedAt = DateTime.UtcNow;
            kepemilikan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Kepemilikan deleted");
        }

        private static KepemilikanDto MapToDto(MasterKepemilikan j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}