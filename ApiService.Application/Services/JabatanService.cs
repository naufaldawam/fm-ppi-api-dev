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
    public interface IJabatanService
    {
        Task<ApiResponse<PagedResponse<JabatanDto>>> GetAllAsync(JabatanFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<JabatanDto>> GetByIdAsync(string id);
        Task<ApiResponse<JabatanDto>> CreateAsync(CreateJabatanRequest request, string userId);
        Task<ApiResponse<JabatanDto>> UpdateAsync(string id, UpdateJabatanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class JabatanService : IJabatanService
    {
        private readonly IServiceDbContext _context;

        public JabatanService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<JabatanDto>>> GetAllAsync(JabatanFilterRequest filter)
        {
            var query = _context.Jabatans
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

            return ApiResponse<PagedResponse<JabatanDto>>.SuccessResponse(new PagedResponse<JabatanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Jabatans.Where(j => !j.IsDeleted);

            if (activeOnly)
                query = query.Where(j => j.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j => j.Name.Contains(search));

            var items = await query
                .OrderBy(j => j.Name)
                .Select(j => new MasterLookupDto { Id = j.Id, Name = j.Name })
                .ToListAsync();

            return ApiResponse<List<MasterLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<JabatanDto>> GetByIdAsync(string id)
        {
            var jabatan = await _context.Jabatans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jabatan == null)
                return ApiResponse<JabatanDto>.ErrorResponse("ERR-JABATAN-001", "Jabatan not found");

            return ApiResponse<JabatanDto>.SuccessResponse(MapToDto(jabatan));
        }

        public async Task<ApiResponse<JabatanDto>> CreateAsync(CreateJabatanRequest request, string userId)
        {
            var exists = await _context.Jabatans
                .AnyAsync(j => j.Name == request.Name && !j.IsDeleted);

            if (exists)
                return ApiResponse<JabatanDto>.ErrorResponse("ERR-JABATAN-002", "Nama jabatan sudah ada");

            var jabatan = new MasterJabatan
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Jabatans.Add(jabatan);
            await _context.SaveChangesAsync();

            return ApiResponse<JabatanDto>.SuccessResponse(MapToDto(jabatan), "Jabatan berhasil ditambahkan");
        }

        public async Task<ApiResponse<JabatanDto>> UpdateAsync(string id, UpdateJabatanRequest request, string userId)
        {
            var jabatan = await _context.Jabatans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jabatan == null)
                return ApiResponse<JabatanDto>.ErrorResponse("ERR-JABATAN-001", "Jabatan not found");

            var duplicate = await _context.Jabatans
                .AnyAsync(j => j.Name == request.Name && j.Id != id && !j.IsDeleted);

            if (duplicate)
                return ApiResponse<JabatanDto>.ErrorResponse("ERR-JABATAN-002", "Nama jabatan sudah ada");

            jabatan.Name = request.Name;
            jabatan.IsActive = request.IsActive;
            jabatan.ModifiedAt = DateTime.UtcNow;
            jabatan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<JabatanDto>.SuccessResponse(MapToDto(jabatan), "Jabatan berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var jabatan = await _context.Jabatans
                .FirstOrDefaultAsync(j => j.Id == id && !j.IsDeleted);

            if (jabatan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-JABATAN-001", "Jabatan not found");

            var isUsed = await _context.Pekerjas
                .AnyAsync(p => p.JabatanId == id && !p.IsDeleted);

            if (isUsed)
                return ApiResponse<bool>.ErrorResponse("ERR-JABATAN-003", "Jabatan masih dipakai oleh pekerja, tidak bisa dihapus");

            jabatan.IsDeleted = true;
            jabatan.DeletedAt = DateTime.UtcNow;
            jabatan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Jabatan deleted");
        }

        private static JabatanDto MapToDto(MasterJabatan j) => new()
        {
            Id = j.Id,
            Name = j.Name,
            IsActive = j.IsActive,
            CreatedAt = j.CreatedAt,
            ModifiedAt = j.ModifiedAt
        };
    }
}