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
    public interface IPekerjaService
    {
        Task<ApiResponse<PagedResponse<PekerjaDto>>> GetAllAsync(PekerjaFilterRequest filter);
        Task<ApiResponse<List<PekerjaLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<PekerjaDto>> GetByIdAsync(string id);
        Task<ApiResponse<PekerjaDto>> CreateAsync(CreatePekerjaRequest request, string userId);
        Task<ApiResponse<PekerjaDto>> UpdateAsync(string id, UpdatePekerjaRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class PekerjaService : IPekerjaService
    {
        private readonly IServiceDbContext _context;

        public PekerjaService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<PekerjaDto>>> GetAllAsync(PekerjaFilterRequest filter)
        {
            var query = _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p =>
                    p.NamaPekerja.Contains(filter.Search) ||
                    p.NoPekerja.Contains(filter.Search) ||
                    p.NopekHome.Contains(filter.Search) ||
                    p.NopekHost.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(p => p.JabatanId == filter.JabatanId);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<PekerjaDto>>.SuccessResponse(new PagedResponse<PekerjaDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<List<PekerjaLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Pekerjas.Where(p => !p.IsDeleted);

            if (activeOnly)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p =>
                    p.NamaPekerja.Contains(search) ||
                    p.NoPekerja.Contains(search));

            var items = await query
                .OrderBy(p => p.NamaPekerja)
                .Select(p => new PekerjaLookupDto
                {
                    Id = p.Id,
                    NoPekerja = p.NoPekerja,
                    NamaPekerja = p.NamaPekerja
                })
                .ToListAsync();

            return ApiResponse<List<PekerjaLookupDto>>.SuccessResponse(items);
        }

        public async Task<ApiResponse<PekerjaDto>> GetByIdAsync(string id)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-001", "Pekerja not found");

            return ApiResponse<PekerjaDto>.SuccessResponse(MapToDto(pekerja));
        }

        public async Task<ApiResponse<PekerjaDto>> CreateAsync(CreatePekerjaRequest request, string userId)
        {
            var noPekerjaExists = await _context.Pekerjas
                .AnyAsync(p => p.NoPekerja == request.NoPekerja && !p.IsDeleted);

            if (noPekerjaExists)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-002", "No. Pekerja sudah terdaftar");

            var jabatan = await _context.Jabatans
                .FirstOrDefaultAsync(j => j.Id == request.JabatanId && !j.IsDeleted);

            if (jabatan == null)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-003", "Jabatan tidak ditemukan");

            var pekerja = new Pekerja
            {
                NoPekerja = request.NoPekerja,
                NopekHome = request.NopekHome,
                NopekHost = request.NopekHost,
                NamaPekerja = request.NamaPekerja,
                JabatanId = request.JabatanId,
                // RfIds sengaja tidak di-set di sini -> di-assign belakangan lewat menu RF.ID
                IsActive = true,
                CreatedBy = userId
            };

            _context.Pekerjas.Add(pekerja);
            await _context.SaveChangesAsync();

            pekerja.Jabatan = jabatan;
            return ApiResponse<PekerjaDto>.SuccessResponse(MapToDto(pekerja), "Pekerja berhasil ditambahkan");
        }

        public async Task<ApiResponse<PekerjaDto>> UpdateAsync(string id, UpdatePekerjaRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-001", "Pekerja not found");

            var noPekerjaExists = await _context.Pekerjas
                .AnyAsync(p => p.NoPekerja == request.NoPekerja && p.Id != id && !p.IsDeleted);

            if (noPekerjaExists)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-002", "No. Pekerja sudah terdaftar");

            var jabatan = await _context.Jabatans
                .FirstOrDefaultAsync(j => j.Id == request.JabatanId && !j.IsDeleted);

            if (jabatan == null)
                return ApiResponse<PekerjaDto>.ErrorResponse("ERR-PEKERJA-003", "Jabatan tidak ditemukan");

            pekerja.NoPekerja = request.NoPekerja;
            pekerja.NopekHome = request.NopekHome;
            pekerja.NopekHost = request.NopekHost;
            pekerja.NamaPekerja = request.NamaPekerja;
            pekerja.JabatanId = request.JabatanId;
            // RfIds sengaja TIDAK diubah di sini - read-only, dikelola dari menu RF.ID terpisah
            pekerja.IsActive = request.IsActive;
            pekerja.ModifiedAt = DateTime.UtcNow;
            pekerja.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            pekerja.Jabatan = jabatan;
            return ApiResponse<PekerjaDto>.SuccessResponse(MapToDto(pekerja), "Pekerja berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var pekerja = await _context.Pekerjas
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<bool>.ErrorResponse("ERR-PEKERJA-001", "Pekerja not found");

            pekerja.IsDeleted = true;
            pekerja.DeletedAt = DateTime.UtcNow;
            pekerja.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Pekerja deleted");
        }

        private static PekerjaDto MapToDto(Pekerja p) => new()
        {
            Id = p.Id,
            NoPekerja = p.NoPekerja,
            NopekHome = p.NopekHome,
            NopekHost = p.NopekHost,
            NamaPekerja = p.NamaPekerja,
            JabatanId = p.JabatanId,
            JabatanName = p.Jabatan?.Name ?? string.Empty,
            RfIds = p.RfIds,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt
        };
    }
}