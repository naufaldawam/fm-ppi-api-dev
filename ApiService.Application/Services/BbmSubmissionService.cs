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
    public interface IBbmSubmissionService
    {
        /// <summary>
        /// Prefill data Step 1 form mobile: info driver (nama, nopek, nama VP),
        /// periode aktif, dan daftar kendaraan aktif (nomor plat + jenis BBM).
        /// Satu request ini menggantikan 3-4 round-trip dari mobile.
        /// </summary>
        Task<ApiResponse<BbmSubmissionFormInitDto>> GetFormInitAsync(string driverId);

        Task<ApiResponse<PagedResponse<BbmSubmissionDto>>> GetAllAsync(BbmSubmissionFilterRequest filter);
        Task<ApiResponse<BbmSubmissionDto>> GetByIdAsync(string id);

        /// <summary>
        /// Submit pengajuan BBM baru (dari wizard 4 langkah mobile).
        /// fotoOdometerUrl dan fotoNotaUrl sudah berupa URL hasil upload storage
        /// (diproses di Controller sebelum memanggil method ini).
        /// </summary>
        Task<ApiResponse<BbmSubmissionDto>> CreateAsync(
            CreateBbmSubmissionRequest request,
            string fotoOdometerUrl,
            string fotoNotaUrl,
            string userId);

        /// <summary>Approve submission - hanya bisa dilakukan oleh Atasan/VP driver ybs.</summary>
        Task<ApiResponse<BbmSubmissionDto>> ApproveAsync(string id, string approverUserId);

        /// <summary>Reject submission dengan alasan.</summary>
        Task<ApiResponse<BbmSubmissionDto>> RejectAsync(string id, RejectBbmSubmissionRequest request, string approverUserId);

        /// <summary>
        /// Hapus submission - hanya bisa dihapus selama masih Pending
        /// (belum diputuskan oleh atasan).
        /// </summary>
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
    }

    public class BbmSubmissionService : IBbmSubmissionService
    {
        private readonly IServiceDbContext _context;

        public BbmSubmissionService(IServiceDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // FORM INIT (mobile prefill)
        // ─────────────────────────────────────────────────────────────────────────

        public async Task<ApiResponse<BbmSubmissionFormInitDto>> GetFormInitAsync(string driverId)
        {
            var driver = await _context.Drivers
                .Include(d => d.Atasan)
                .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted && d.IsActive);

            if (driver == null)
                return ApiResponse<BbmSubmissionFormInitDto>.ErrorResponse(
                    "ERR-BBM-001", "Driver tidak ditemukan atau tidak aktif");

            // Periode aktif = periode yang mencakup tanggal hari ini
            var today = DateTime.UtcNow.Date;
            var periodeAktif = await _context.Periodes
                .Where(p => !p.IsDeleted && p.TanggalAwal.Date <= today && p.TanggalAkhir.Date >= today)
                .OrderByDescending(p => p.TanggalAwal)
                .FirstOrDefaultAsync();

            // Daftar kendaraan aktif beserta jenis BBM-nya
            var kendaraanAktif = await _context.Kendaraans
                .Include(k => k.BahanBakar)
                .Where(k => !k.IsDeleted && k.IsActive)
                .OrderBy(k => k.NomorPolisi)
                .Select(k => new BbmSubmissionKendaraanOptionDto
                {
                    Id = k.Id,
                    NomorPolisi = k.NomorPolisi,
                    JenisBbmName = k.BahanBakar != null ? k.BahanBakar.Name : string.Empty
                })
                .ToListAsync();

            var dto = new BbmSubmissionFormInitDto
            {
                DriverId        = driver.Id,
                NamaDriver      = driver.NamaDriver,
                NoPekerjaDriver = driver.NoPekerja,
                NamaVP          = driver.Atasan != null ? driver.Atasan.NamaPekerja : string.Empty,
                PeriodeAktifId      = periodeAktif?.Id,
                NamaPeriodeAktif    = periodeAktif?.NamaPeriode,
                KendaraanAktif  = kendaraanAktif
            };

            return ApiResponse<BbmSubmissionFormInitDto>.SuccessResponse(dto);
        }

        // LIST

        public async Task<ApiResponse<PagedResponse<BbmSubmissionDto>>> GetAllAsync(BbmSubmissionFilterRequest filter)
        {
            var query = _context.BbmSubmissions
                .Include(b => b.Driver)
                .Include(b => b.AtasanPekerja)
                .Include(b => b.Periode)
                .Include(b => b.Kendaraan)
                    .ThenInclude(k => k!.BahanBakar)
                .Where(b => !b.IsDeleted)
                .AsQueryable();

            // Filter
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(b =>
                    (b.Driver != null && (b.Driver.NoPekerja.Contains(s) || b.Driver.NamaDriver.Contains(s))) ||
                    (b.Kendaraan != null && b.Kendaraan.NomorPolisi.Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(filter.DriverId))
                query = query.Where(b => b.DriverId == filter.DriverId);

            if (!string.IsNullOrWhiteSpace(filter.AtasanPekerjaId))
                query = query.Where(b => b.AtasanPekerjaId == filter.AtasanPekerjaId);

            if (!string.IsNullOrWhiteSpace(filter.PeriodeId))
                query = query.Where(b => b.PeriodeId == filter.PeriodeId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(b => b.Status == filter.Status);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(b => b.TanggalPenggunaan)
                .ThenByDescending(b => b.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<BbmSubmissionDto>>.SuccessResponse(new PagedResponse<BbmSubmissionDto>
            {
                Items       = items.Select(MapToDto).ToList(),
                TotalCount  = totalCount,
                PageNumber  = filter.Page,
                PageSize    = filter.PageSize
            });
        }

        // GET BY ID

        public async Task<ApiResponse<BbmSubmissionDto>> GetByIdAsync(string id)
        {
            var submission = await _context.BbmSubmissions
                .Include(b => b.Driver)
                .Include(b => b.AtasanPekerja)
                .Include(b => b.Periode)
                .Include(b => b.Kendaraan)
                    .ThenInclude(k => k!.BahanBakar)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (submission == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse("ERR-BBM-002", "Submission tidak ditemukan");

            return ApiResponse<BbmSubmissionDto>.SuccessResponse(MapToDto(submission));
        }

        // CREATE

        public async Task<ApiResponse<BbmSubmissionDto>> CreateAsync(
            CreateBbmSubmissionRequest request,
            string fotoOdometerUrl,
            string fotoNotaUrl,
            string userId)
        {
            // Validasi driver
            var driver = await _context.Drivers
                .Include(d => d.Atasan)
                .FirstOrDefaultAsync(d => d.Id == request.DriverId && !d.IsDeleted && d.IsActive);

            if (driver == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-001", "Driver tidak ditemukan atau tidak aktif");

            // Validasi periode
            var periode = await _context.Periodes
                .FirstOrDefaultAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (periode == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-003", "Periode tidak ditemukan");

            // Validasi kendaraan
            var kendaraan = await _context.Kendaraans
                .Include(k => k.BahanBakar)
                .FirstOrDefaultAsync(k => k.Id == request.KendaraanId && !k.IsDeleted && k.IsActive);

            if (kendaraan == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-004", "Kendaraan tidak ditemukan atau tidak aktif");

            // Cegah duplikat submission (driver + kendaraan + tanggal yang sama di periode sama)
            var duplicate = await _context.BbmSubmissions.AnyAsync(b =>
                !b.IsDeleted &&
                b.DriverId == request.DriverId &&
                b.KendaraanId == request.KendaraanId &&
                b.PeriodeId == request.PeriodeId &&
                b.TanggalPenggunaan.Date == request.TanggalPenggunaan.Date &&
                b.Status != BbmSubmission.StatusRejected);

            if (duplicate)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-005",
                    "Sudah ada pengajuan BBM untuk driver, kendaraan, dan tanggal yang sama di periode ini");

            var submission = new BbmSubmission
            {
                DriverId              = request.DriverId,
                AtasanPekerjaId       = driver.AtasanId,
                PeriodeId             = request.PeriodeId,
                TanggalPenggunaan     = request.TanggalPenggunaan,
                KendaraanId           = request.KendaraanId,
                JumlahPenggunaanBbm   = request.JumlahPenggunaanBbm,
                NilaiOdometer         = request.NilaiOdometer,
                NilaiNota             = request.NilaiNota,
                FotoOdometerUrl       = fotoOdometerUrl,
                FotoNotaUrl           = fotoNotaUrl,
                CatatanTambahan       = request.CatatanTambahan,
                Status                = BbmSubmission.StatusPending,
                CreatedBy             = userId
            };

            _context.BbmSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            var created = await _context.BbmSubmissions
                .Include(b => b.Driver)
                .Include(b => b.AtasanPekerja)
                .Include(b => b.Periode)
                .Include(b => b.Kendaraan)
                    .ThenInclude(k => k!.BahanBakar)
                .FirstAsync(b => b.Id == submission.Id);

            return ApiResponse<BbmSubmissionDto>.Created(MapToDto(created), "Pengajuan BBM berhasil dikirim");
        }

        // APPROVE

        public async Task<ApiResponse<BbmSubmissionDto>> ApproveAsync(string id, string approverUserId)
        {
            var submission = await GetSubmissionWithIncludes(id);
            if (submission == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse("ERR-BBM-002", "Submission tidak ditemukan");

            if (submission.Status != BbmSubmission.StatusPending)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-006",
                    $"Submission tidak dapat di-approve karena statusnya sudah '{submission.Status}'");

            submission.Status       = BbmSubmission.StatusApproved;
            submission.ApprovedBy   = approverUserId;
            submission.ApprovedAt   = DateTime.UtcNow;
            submission.ModifiedAt   = DateTime.UtcNow;
            submission.ModifiedBy   = approverUserId;

            await _context.SaveChangesAsync();

            return ApiResponse<BbmSubmissionDto>.SuccessResponse(MapToDto(submission), "Pengajuan BBM berhasil di-approve");
        }

        // REJECT

        public async Task<ApiResponse<BbmSubmissionDto>> RejectAsync(
            string id,
            RejectBbmSubmissionRequest request,
            string approverUserId)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-007", "Alasan penolakan wajib diisi");

            var submission = await GetSubmissionWithIncludes(id);
            if (submission == null)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse("ERR-BBM-002", "Submission tidak ditemukan");

            if (submission.Status != BbmSubmission.StatusPending)
                return ApiResponse<BbmSubmissionDto>.ErrorResponse(
                    "ERR-BBM-006",
                    $"Submission tidak dapat di-reject karena statusnya sudah '{submission.Status}'");

            submission.Status           = BbmSubmission.StatusRejected;
            submission.ApprovedBy       = approverUserId;
            submission.ApprovedAt       = DateTime.UtcNow;
            submission.RejectedReason   = request.Reason.Trim();
            submission.ModifiedAt       = DateTime.UtcNow;
            submission.ModifiedBy       = approverUserId;

            await _context.SaveChangesAsync();

            return ApiResponse<BbmSubmissionDto>.SuccessResponse(MapToDto(submission), "Pengajuan BBM ditolak");
        }

        // DELETE (soft delete, hanya selagi Pending)

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var submission = await _context.BbmSubmissions
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

            if (submission == null)
                return ApiResponse<bool>.ErrorResponse("ERR-BBM-002", "Submission tidak ditemukan");

            if (submission.Status != BbmSubmission.StatusPending)
                return ApiResponse<bool>.ErrorResponse(
                    "ERR-BBM-008",
                    "Hanya submission berstatus Pending yang dapat dihapus");

            submission.IsDeleted    = true;
            submission.DeletedAt    = DateTime.UtcNow;
            submission.DeletedBy    = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Submission berhasil dihapus");
        }

        // HELPERS

        private async Task<BbmSubmission?> GetSubmissionWithIncludes(string id)
            => await _context.BbmSubmissions
                .Include(b => b.Driver)
                .Include(b => b.AtasanPekerja)
                .Include(b => b.Periode)
                .Include(b => b.Kendaraan)
                    .ThenInclude(k => k!.BahanBakar)
                .FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted);

        private static BbmSubmissionDto MapToDto(BbmSubmission b) => new()
        {
            Id                  = b.Id,
            DriverId            = b.DriverId,
            NoPekerjaDriver     = b.Driver?.NoPekerja ?? string.Empty,
            NamaDriver          = b.Driver?.NamaDriver ?? string.Empty,
            AtasanPekerjaId     = b.AtasanPekerjaId,
            NamaVP              = b.AtasanPekerja?.NamaPekerja ?? string.Empty,
            PeriodeId           = b.PeriodeId,
            NamaPeriode         = b.Periode?.NamaPeriode ?? string.Empty,
            TanggalPenggunaan   = b.TanggalPenggunaan,
            KendaraanId         = b.KendaraanId,
            NomorPolisi         = b.Kendaraan?.NomorPolisi ?? string.Empty,
            JenisBbmName        = b.Kendaraan?.BahanBakar?.Name ?? string.Empty,
            JumlahPenggunaanBbm = b.JumlahPenggunaanBbm,
            NilaiOdometer       = b.NilaiOdometer,
            NilaiNota           = b.NilaiNota,
            FotoOdometerUrl     = b.FotoOdometerUrl,
            FotoNotaUrl         = b.FotoNotaUrl,
            CatatanTambahan     = b.CatatanTambahan,
            Status              = b.Status,
            ApprovedBy          = b.ApprovedBy,
            ApprovedAt          = b.ApprovedAt,
            RejectedReason      = b.RejectedReason,
            CreatedAt           = b.CreatedAt,
            ModifiedAt          = b.ModifiedAt
        };
    }
}