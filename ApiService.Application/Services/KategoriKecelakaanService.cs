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
    public interface IKategoriKecelakaanService
    {
        Task<ApiResponse<PagedResponse<KategoriKecelakaanDto>>> GetAllAsync(KategoriKecelakaanFilterRequest filter);
        Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<KategoriKecelakaanDto>> GetByIdAsync(string id);
        Task<ApiResponse<KategoriKecelakaanDto>> CreateAsync(CreateKategoriKecelakaanRequest request, string userId);
        Task<ApiResponse<KategoriKecelakaanDto>> UpdateAsync(string id, UpdateKategoriKecelakaanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class KategoriKecelakaanService : IKategoriKecelakaanService
    {
        private readonly IServiceDbContext _context;

        public KategoriKecelakaanService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<KategoriKecelakaanDto>>> GetAllAsync(KategoriKecelakaanFilterRequest filter)
        {
            var query = _context.KategoriKecelakaans
                .Where(k => !k.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(k => k.Name.Contains(filter.Search));

            if (filter.IsActive.HasValue)
                query = query.Where(k => k.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(k => k.Name)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<KategoriKecelakaanDto>>.SuccessResponse(new PagedResponse<KategoriKecelakaanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        /// <summary>Lookup dropdown: GET /kategori-kecelakaan/lookup?search=xxx&amp;activeOnly=true</summary>
        public async Task<ApiResponse<List<MasterLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.KategoriKecelakaans.Where(k => !k.IsDeleted);

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

        public async Task<ApiResponse<KategoriKecelakaanDto>> GetByIdAsync(string id)
        {
            var kategori = await _context.KategoriKecelakaans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kategori == null)
                return ApiResponse<KategoriKecelakaanDto>.ErrorResponse("ERR-KATEGORIKECELAKAAN-001", "Kategori kecelakaan not found");

            return ApiResponse<KategoriKecelakaanDto>.SuccessResponse(MapToDto(kategori));
        }

        public async Task<ApiResponse<KategoriKecelakaanDto>> CreateAsync(CreateKategoriKecelakaanRequest request, string userId)
        {
            var exists = await _context.KategoriKecelakaans
                .AnyAsync(k => k.Name == request.Name && !k.IsDeleted);

            if (exists)
                return ApiResponse<KategoriKecelakaanDto>.ErrorResponse("ERR-KATEGORIKECELAKAAN-002", "Nama kategori kecelakaan sudah ada");

            var kategori = new MasterKategoriKecelakaan
            {
                Name = request.Name,
                IsActive = true,
                CreatedBy = userId
            };

            _context.KategoriKecelakaans.Add(kategori);
            await _context.SaveChangesAsync();

            return ApiResponse<KategoriKecelakaanDto>.SuccessResponse(MapToDto(kategori), "Kategori kecelakaan berhasil ditambahkan");
        }

        public async Task<ApiResponse<KategoriKecelakaanDto>> UpdateAsync(string id, UpdateKategoriKecelakaanRequest request, string userId)
        {
            var kategori = await _context.KategoriKecelakaans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kategori == null)
                return ApiResponse<KategoriKecelakaanDto>.ErrorResponse("ERR-KATEGORIKECELAKAAN-001", "Kategori kecelakaan not found");

            var duplicate = await _context.KategoriKecelakaans
                .AnyAsync(k => k.Name == request.Name && k.Id != id && !k.IsDeleted);

            if (duplicate)
                return ApiResponse<KategoriKecelakaanDto>.ErrorResponse("ERR-KATEGORIKECELAKAAN-002", "Nama kategori kecelakaan sudah ada");

            kategori.Name = request.Name;
            kategori.IsActive = request.IsActive;
            kategori.ModifiedAt = DateTime.UtcNow;
            kategori.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<KategoriKecelakaanDto>.SuccessResponse(MapToDto(kategori), "Kategori kecelakaan berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var kategori = await _context.KategoriKecelakaans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kategori == null)
                return ApiResponse<bool>.ErrorResponse("ERR-KATEGORIKECELAKAAN-001", "Kategori kecelakaan not found");

            // TODO: kalau nanti dinakain oleh form, tambah isUsed check (sama pattern Tipe/JenisBbm)

            kategori.IsDeleted = true;
            kategori.DeletedAt = DateTime.UtcNow;
            kategori.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Kategori kecelakaan deleted");
        }

        private static KategoriKecelakaanDto MapToDto(MasterKategoriKecelakaan k) => new()
        {
            Id = k.Id,
            Name = k.Name,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ModifiedAt = k.ModifiedAt
        };
    }
}