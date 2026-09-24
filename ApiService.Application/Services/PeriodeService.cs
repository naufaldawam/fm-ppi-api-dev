using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IPeriodeService
    {
        Task<ApiResponse<PagedResponse<PeriodeDto>>> GetAllAsync(PeriodeFilterRequest filter);
        Task<ApiResponse<PeriodeDto>> GetByIdAsync(string id);
        Task<ApiResponse<PeriodeDto>> CreateAsync(CreatePeriodeRequest request, string userId);
        Task<ApiResponse<PeriodeDto>> UpdateAsync(string id, UpdatePeriodeRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);

        // Lookup & status
        Task<ApiResponse<List<PeriodeLookupDto>>> GetLookupAsync(string? search, bool activeOnly = false);
        Task<ApiResponse<PeriodeStatusDto>> GetStatusAsync(string id);
        Task<ApiResponse<PeriodeStatusDto>> GetCurrentActiveAsync();
    }

    public class PeriodeService : IPeriodeService
    {
        private readonly IServiceDbContext _context;

        public PeriodeService(IServiceDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedResponse<PeriodeDto>>> GetAllAsync(PeriodeFilterRequest filter)
        {
            var query = _context.Periodes
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(p => p.NamaPeriode.Contains(filter.Search));

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.TanggalAwal)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<PeriodeDto>>.SuccessResponse(new PagedResponse<PeriodeDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<PeriodeDto>> GetByIdAsync(string id)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode));
        }

        public async Task<ApiResponse<PeriodeDto>> CreateAsync(CreatePeriodeRequest request, string userId)
        {
            var exists = await _context.Periodes
                .AnyAsync(p => p.NamaPeriode == request.NamaPeriode && !p.IsDeleted);

            if (exists)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-002", "Nama periode sudah ada");

            var overlapping = await FindOverlappingPeriodeAsync(request.TanggalAwal, request.TanggalAkhir, excludeId: null);
            if (overlapping != null)
            {
                return ApiResponse<PeriodeDto>.Conflict(
                    $"Periode bertabrakan (overlap) dengan periode '{overlapping.NamaPeriode}' " +
                    $"({overlapping.TanggalAwal:dd-MM-yyyy} s/d {overlapping.TanggalAkhir:dd-MM-yyyy}).");
            }

            var periode = new Periode
            {
                NamaPeriode = request.NamaPeriode,
                TanggalAwal = request.TanggalAwal,
                TanggalAkhir = request.TanggalAkhir,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.Periodes.Add(periode);
            await _context.SaveChangesAsync();

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode), "Periode berhasil ditambahkan");
        }

        public async Task<ApiResponse<PeriodeDto>> UpdateAsync(string id, UpdatePeriodeRequest request, string userId)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            var duplicate = await _context.Periodes
                .AnyAsync(p => p.NamaPeriode == request.NamaPeriode && p.Id != id && !p.IsDeleted);

            if (duplicate)
                return ApiResponse<PeriodeDto>.ErrorResponse("ERR-PERIODE-002", "Nama periode sudah ada");

            var overlapping = await FindOverlappingPeriodeAsync(request.TanggalAwal, request.TanggalAkhir, excludeId: id);
            if (overlapping != null)
            {
                return ApiResponse<PeriodeDto>.Conflict(
                    $"Periode bertabrakan (overlap) dengan periode '{overlapping.NamaPeriode}' " +
                    $"({overlapping.TanggalAwal:dd-MM-yyyy} s/d {overlapping.TanggalAkhir:dd-MM-yyyy}).");
            }

            periode.NamaPeriode = request.NamaPeriode;
            periode.TanggalAwal = request.TanggalAwal;
            periode.TanggalAkhir = request.TanggalAkhir;
            periode.IsActive = request.IsActive;
            periode.ModifiedAt = DateTime.UtcNow;
            periode.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<PeriodeDto>.SuccessResponse(MapToDto(periode), "Periode berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<bool>.ErrorResponse("ERR-PERIODE-001", "Periode not found");

            periode.IsDeleted = true;
            periode.DeletedAt = DateTime.UtcNow;
            periode.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Periode deleted");
        }

        // =========================================================
        // LOOKUP & STATUS
        // =========================================================

        public async Task<ApiResponse<List<PeriodeLookupDto>>> GetLookupAsync(string? search, bool activeOnly = false)
        {
            var query = _context.Periodes.Where(p => !p.IsDeleted);

            if (activeOnly)
                query = query.Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.NamaPeriode.Contains(search));

            var items = await query
                .OrderByDescending(p => p.TanggalAwal)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var result = items.Select(p => new PeriodeLookupDto
            {
                Id = p.Id,
                NamaPeriode = p.NamaPeriode,
                TanggalAwal = p.TanggalAwal,
                TanggalAkhir = p.TanggalAkhir,
                IsActive = p.IsActive,
                IsCurrentlyActive = ComputeIsCurrentlyActive(p, now)
            }).ToList();

            return ApiResponse<List<PeriodeLookupDto>>.SuccessResponse(result);
        }

        /// <summary>Cek status satu periode: aktif hanya jika IsActive true DAN tanggal sekarang ada di dalam rentang.</summary>
        public async Task<ApiResponse<PeriodeStatusDto>> GetStatusAsync(string id)
        {
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<PeriodeStatusDto>.NotFound("Periode not found");

            return ApiResponse<PeriodeStatusDto>.SuccessResponse(BuildStatusDto(periode));
        }

        /// <summary>Ambil periode yang sedang aktif saat ini (IsActive true DAN tanggal sekarang di dalam rentang), kalau ada.</summary>
        public async Task<ApiResponse<PeriodeStatusDto>> GetCurrentActiveAsync()
        {
            var today = DateTime.UtcNow.Date;

            var periode = await _context.Periodes
                .Where(p => !p.IsDeleted &&
                            p.IsActive &&
                            p.TanggalAwal.Date <= today &&
                            p.TanggalAkhir.Date >= today)
                .OrderByDescending(p => p.TanggalAwal)
                .FirstOrDefaultAsync();

            if (periode == null)
                return ApiResponse<PeriodeStatusDto>.NotFound("Tidak ada periode yang sedang aktif saat ini.");

            return ApiResponse<PeriodeStatusDto>.SuccessResponse(BuildStatusDto(periode));
        }

        // =========================================================
        // HELPERS
        // =========================================================

        /// <summary>
        /// Aktif hanya jika IsActive == true DAN tanggal `asOf` berada di antara
        /// TanggalAwal - TanggalAkhir (inklusif, dibandingkan per-tanggal saja tanpa jam).
        /// Salah satu syarat saja gagal -> dianggap tidak aktif.
        /// </summary>
        private static bool ComputeIsCurrentlyActive(Periode p, DateTime asOf)
        {
            if (!p.IsActive) return false;

            var today = asOf.Date;
            return today >= p.TanggalAwal.Date && today <= p.TanggalAkhir.Date;
        }

        private static PeriodeStatusDto BuildStatusDto(Periode p)
        {
            var now = DateTime.UtcNow;
            var isCurrentlyActive = ComputeIsCurrentlyActive(p, now);

            string reason;
            if (!p.IsActive)
                reason = "Status IsActive periode ini sedang non-aktif (off).";
            else if (now.Date < p.TanggalAwal.Date)
                reason = $"Belum memasuki periode (mulai {p.TanggalAwal:dd-MM-yyyy}).";
            else if (now.Date > p.TanggalAkhir.Date)
                reason = $"Periode sudah berakhir ({p.TanggalAkhir:dd-MM-yyyy}).";
            else
                reason = "Periode aktif: status IsActive menyala dan tanggal sekarang berada dalam rentang periode.";

            return new PeriodeStatusDto
            {
                Id = p.Id,
                NamaPeriode = p.NamaPeriode,
                TanggalAwal = p.TanggalAwal,
                TanggalAkhir = p.TanggalAkhir,
                IsActive = p.IsActive,
                IsCurrentlyActive = isCurrentlyActive,
                Reason = reason
            };
        }

        /// <summary>
        /// Cari periode lain (selain <paramref name="excludeId"/>, kalau diisi) yang rentang
        /// tanggalnya bertabrakan dengan rentang baru. Dua rentang overlap jika:
        /// awalA &lt;= akhirB DAN awalB &lt;= akhirA. Dibandingkan per-tanggal (tanpa jam).
        /// </summary>
        private async Task<Periode?> FindOverlappingPeriodeAsync(DateTime tanggalAwal, DateTime tanggalAkhir, string? excludeId)
        {
            var awal = tanggalAwal.Date;
            var akhir = tanggalAkhir.Date;

            var query = _context.Periodes.Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(excludeId))
                query = query.Where(p => p.Id != excludeId);

            return await query.FirstOrDefaultAsync(p =>
                p.TanggalAwal.Date <= akhir && awal <= p.TanggalAkhir.Date);
        }

        private static PeriodeDto MapToDto(Periode p) => new()
        {
            Id = p.Id,
            NamaPeriode = p.NamaPeriode,
            TanggalAwal = p.TanggalAwal,
            TanggalAkhir = p.TanggalAkhir,
            IsActive = p.IsActive,
            IsCurrentlyActive = ComputeIsCurrentlyActive(p, DateTime.UtcNow),
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt
        };
    }
}