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
    public interface IJenisBbmService
    {
        Task<ApiResponse<PagedResponse<JenisBbmDto>>> GetAllAsync(JenisBbmFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<JenisBbmDto>> GetByIdAsync(string id);
        Task<ApiResponse<JenisBbmDto>> CreateAsync(CreateJenisBbmRequest request, string userId);
        Task<ApiResponse<JenisBbmDto>> UpdateAsync(string id, UpdateJenisBbmRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class JenisBbmService : IJenisBbmService
    {
        private readonly IServiceDbContext _context;

        public JenisBbmService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<JenisBbmDto>>> GetAllAsync(JenisBbmFilterRequest filter)
        {
            var query = _context.JenisBbms
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

            return ApiResponse<PagedResponse<JenisBbmDto>>.SuccessResponse(new PagedResponse<JenisBbmDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        /// <summary>Lookup dropdown: GET /jenis-bbm/lookup?search=xxx&amp;activeOnly=true</summary>
        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.JenisBbms.Where(t => !t.IsDeleted);

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

        public async Task<ApiResponse<JenisBbmDto>> GetByIdAsync(string id)
        {
            var jenisBbm = await _context.JenisBbms
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jenisBbm == null)
                return ApiResponse<JenisBbmDto>.ErrorResponse("ERR-JENISBBM-001", "Jenis BBM not found");

            return ApiResponse<JenisBbmDto>.SuccessResponse(MapToDto(jenisBbm));
        }

        public async Task<ApiResponse<JenisBbmDto>> CreateAsync(CreateJenisBbmRequest request, string userId)
        {
            var exists = await _context.JenisBbms
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<JenisBbmDto>.ErrorResponse("ERR-JENISBBM-002", "Nama Jenis BBM sudah ada");

            var jenisBbm = new MasterJenisBbm
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.JenisBbms.Add(jenisBbm);
            await _context.SaveChangesAsync();

            return ApiResponse<JenisBbmDto>.SuccessResponse(MapToDto(jenisBbm), "Jenis BBM berhasil ditambahkan");
        }

        public async Task<ApiResponse<JenisBbmDto>> UpdateAsync(string id, UpdateJenisBbmRequest request, string userId)
        {
            var jenisBbm = await _context.JenisBbms
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jenisBbm == null)
                return ApiResponse<JenisBbmDto>.ErrorResponse("ERR-JENISBBM-001", "Jenis BBM not found");

            var duplicate = await _context.JenisBbms
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<JenisBbmDto>.ErrorResponse("ERR-JENISBBM-002", "Nama Jenis BBM sudah ada");

            jenisBbm.Name = request.Name;
            jenisBbm.IsActive = request.IsActive;
            jenisBbm.ModifiedAt = DateTime.UtcNow;
            jenisBbm.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<JenisBbmDto>.SuccessResponse(MapToDto(jenisBbm), "Jenis BBM berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var jenisBbm = await _context.JenisBbms
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jenisBbm == null)
                return ApiResponse<bool>.ErrorResponse("ERR-JENISBBM-001", "Jenis BBM not found");

            // TODO: kalau nanti dinakain oleh form (scan Kendaraan/BbmSubmission),
            // tambah check yang sama dengan Tipe (commented isUsed in TipeService).

            jenisBbm.IsDeleted = true;
            jenisBbm.DeletedAt = DateTime.UtcNow;
            jenisBbm.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Jenis BBM deleted");
        }

        private static JenisBbmDto MapToDto(MasterJenisBbm j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}