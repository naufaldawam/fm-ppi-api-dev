using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface IKendaraanService
    {
        Task<ApiResponse<PagedResponse<KendaraanDto>>> GetAllAsync(KendaraanFilterRequest filter);
        Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
        Task<ApiResponse<List<PekerjaLookupDto>>> GetPekerjaLookupByJabatanAsync(string jabatanId, string? search, bool activeOnly = true);
        Task<ApiResponse<KendaraanDto>> GetByIdAsync(string id);
        Task<ApiResponse<KendaraanDto>> CreateAsync(CreateKendaraanRequest request, string userId);
        Task<ApiResponse<KendaraanDto>> UpdateAsync(string id, UpdateKendaraanRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<KendaraanImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class KendaraanService : IKendaraanService
    {
        private readonly IServiceDbContext _context;

        public KendaraanService(IServiceDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // BASE QUERY
        // =========================================================

        private IQueryable<Kendaraan> BaseQuery() =>
            _context.Kendaraans
                .Include(k => k.Tipe)
                .Include(k => k.BahanBakar)
                .Include(k => k.Kepemilikan)
                .Include(k => k.Jabatan)
                .Include(k => k.Pekerja)
                .Where(k => !k.IsDeleted);

        // =========================================================
        // GET ALL (paged + filter)
        // =========================================================

        public async Task<ApiResponse<PagedResponse<KendaraanDto>>> GetAllAsync(KendaraanFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(k =>
                    k.NomorPolisi.Contains(filter.Search) ||
                    k.Merek.Contains(filter.Search));

            if (!string.IsNullOrEmpty(filter.TipeId))
                query = query.Where(k => k.TipeId == filter.TipeId);

            if (!string.IsNullOrEmpty(filter.BahanBakarId))
                query = query.Where(k => k.BahanBakarId == filter.BahanBakarId);

            if (!string.IsNullOrEmpty(filter.KepemilikanId))
                query = query.Where(k => k.KepemilikanId == filter.KepemilikanId);

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(k => k.JabatanId == filter.JabatanId);

            if (filter.IsActive.HasValue)
                query = query.Where(k => k.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(k => k.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<KendaraanDto>>.SuccessResponse(new PagedResponse<KendaraanDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        // =========================================================
        // LOOKUP KENDARAAN (dropdown Nopol)
        // =========================================================

        public async Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Kendaraans.Where(k => !k.IsDeleted);

            if (activeOnly)
                query = query.Where(k => k.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(k => k.NomorPolisi.Contains(search));

            var items = await query
                .OrderBy(k => k.NomorPolisi)
                .Select(k => new KendaraanLookupDto
                {
                    Id = k.Id,
                    NomorPolisi = k.NomorPolisi
                })
                .ToListAsync();

            return ApiResponse<List<KendaraanLookupDto>>.SuccessResponse(items);
        }

        // =========================================================
        // LOOKUP PEJABAT BERDASARKAN JABATAN
        // Digunakan frontend: setelah user pilih Alokasi Jabatan,
        // dropdown Pejabat hanya menampilkan Pekerja yang JabatanId-nya sama.
        // GET /kendaraan/pekerja-lookup?jabatanId=xxx&search=yyy&activeOnly=true
        // =========================================================

        public async Task<ApiResponse<List<PekerjaLookupDto>>> GetPekerjaLookupByJabatanAsync(
            string jabatanId, string? search, bool activeOnly = true)
        {
            if (string.IsNullOrWhiteSpace(jabatanId))
                return ApiResponse<List<PekerjaLookupDto>>.BadRequest("JabatanId wajib diisi.");

            var jabatanExists = await _context.Jabatans
                .AnyAsync(j => j.Id == jabatanId && !j.IsDeleted);

            if (!jabatanExists)
                return ApiResponse<List<PekerjaLookupDto>>.ErrorResponse(
                    "ERR-KENDARAAN-010", "Alokasi jabatan tidak ditemukan.");

            var query = _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(p => !p.IsDeleted && p.JabatanId == jabatanId);

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
                    NamaPekerja = p.NamaPekerja,
                    JabatanId = p.JabatanId,
                    JabatanName = p.Jabatan != null ? p.Jabatan.Name : string.Empty
                })
                .ToListAsync();

            return ApiResponse<List<PekerjaLookupDto>>.SuccessResponse(items);
        }

        // =========================================================
        // GET BY ID
        // =========================================================

        public async Task<ApiResponse<KendaraanDto>> GetByIdAsync(string id)
        {
            var kendaraan = await BaseQuery().FirstOrDefaultAsync(k => k.Id == id);

            if (kendaraan == null)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(kendaraan));
        }

        // =========================================================
        // CREATE
        // =========================================================

        public async Task<ApiResponse<KendaraanDto>> CreateAsync(CreateKendaraanRequest request, string userId)
        {
            var nomorPolisiExists = await _context.Kendaraans
                .AnyAsync(k => k.NomorPolisi == request.NomorPolisi && !k.IsDeleted);

            if (nomorPolisiExists)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-002", "Nomor polisi sudah terdaftar");

            var refError = await ValidateReferencesAsync(
                request.TipeId, request.BahanBakarId,
                request.KepemilikanId, request.JabatanId, request.PekerjaId);

            if (refError != null)
                return ApiResponse<KendaraanDto>.ErrorResponse(refError.Value.code, refError.Value.message);

            // Validasi Pejabat harus punya jabatan yang sama dengan alokasi jabatan kendaraan
            if (!string.IsNullOrEmpty(request.PekerjaId))
            {
                var pekerjaJabatanMatch = await _context.Pekerjas
                    .AnyAsync(p => p.Id == request.PekerjaId && p.JabatanId == request.JabatanId && !p.IsDeleted);

                if (!pekerjaJabatanMatch)
                    return ApiResponse<KendaraanDto>.ErrorResponse(
                        "ERR-KENDARAAN-009",
                        "Pejabat yang dipilih tidak memiliki jabatan yang sesuai dengan alokasi jabatan kendaraan ini.");
            }

            var kendaraan = new Kendaraan
            {
                NomorPolisi = request.NomorPolisi,
                TipeId = request.TipeId,
                BahanBakarId = request.BahanBakarId,
                Merek = request.Merek,
                KepemilikanId = request.KepemilikanId,
                JabatanId = request.JabatanId,
                PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId,
                IsActive = true,
                CreatedBy = userId
            };

            _context.Kendaraans.Add(kendaraan);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(k => k.Id == kendaraan.Id);
            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(created), "Kendaraan berhasil ditambahkan");
        }

        // =========================================================
        // UPDATE
        // =========================================================

        public async Task<ApiResponse<KendaraanDto>> UpdateAsync(string id, UpdateKendaraanRequest request, string userId)
        {
            var kendaraan = await _context.Kendaraans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kendaraan == null)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            var nomorPolisiExists = await _context.Kendaraans
                .AnyAsync(k => k.NomorPolisi == request.NomorPolisi && k.Id != id && !k.IsDeleted);

            if (nomorPolisiExists)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-002", "Nomor polisi sudah terdaftar");

            var refError = await ValidateReferencesAsync(
                request.TipeId, request.BahanBakarId,
                request.KepemilikanId, request.JabatanId, request.PekerjaId);

            if (refError != null)
                return ApiResponse<KendaraanDto>.ErrorResponse(refError.Value.code, refError.Value.message);

            // Validasi Pejabat harus punya jabatan yang sama dengan alokasi jabatan kendaraan
            if (!string.IsNullOrEmpty(request.PekerjaId))
            {
                var pekerjaJabatanMatch = await _context.Pekerjas
                    .AnyAsync(p => p.Id == request.PekerjaId && p.JabatanId == request.JabatanId && !p.IsDeleted);

                if (!pekerjaJabatanMatch)
                    return ApiResponse<KendaraanDto>.ErrorResponse(
                        "ERR-KENDARAAN-009",
                        "Pejabat yang dipilih tidak memiliki jabatan yang sesuai dengan alokasi jabatan kendaraan ini.");
            }

            // Cek referensi sebelum nonaktifkan - Kendaraan bisa dipakai sebagai
            // Kendaraan (Nopol) di data RF.ID
            if (kendaraan.IsActive && !request.IsActive)
            {
                var blocker = await GetDeactivationBlockerAsync(kendaraan.Id);
                if (blocker != null)
                    return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-010", blocker);
            }

            kendaraan.NomorPolisi = request.NomorPolisi;
            kendaraan.TipeId = request.TipeId;
            kendaraan.BahanBakarId = request.BahanBakarId;
            kendaraan.Merek = request.Merek;
            kendaraan.KepemilikanId = request.KepemilikanId;
            kendaraan.JabatanId = request.JabatanId;
            kendaraan.PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId;
            kendaraan.IsActive = request.IsActive;
            kendaraan.ModifiedAt = DateTime.UtcNow;
            kendaraan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(k => k.Id == kendaraan.Id);
            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(updated), "Kendaraan berhasil diperbarui");
        }

        // =========================================================
        // DELETE
        // =========================================================

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var kendaraan = await _context.Kendaraans
                .FirstOrDefaultAsync(k => k.Id == id && !k.IsDeleted);

            if (kendaraan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            kendaraan.IsDeleted = true;
            kendaraan.DeletedAt = DateTime.UtcNow;
            kendaraan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Kendaraan deleted");
        }

        // =========================================================
        // BULK UPLOAD — IMPORT FROM EXCEL
        // Template kolom: Nopol | Merek | Tipe | BahanBakar | Kepemilikan | Jabatan | NoPekerja
        // NoPekerja opsional. Jika diisi, jabatan pekerja HARUS cocok dengan kolom Jabatan.
        // =========================================================

        private const int KendaraanImportHeaderRow = 1;
        private const int KendaraanImportDataStartRow = 2;

        private static readonly string[] KendaraanRequiredImportHeaders =
        {
            "nopol", "merek", "tipe", "bahanbakar", "kepemilikan", "jabatan", "nopekerja"
        };

        public async Task<ApiResponse<KendaraanImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new KendaraanImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<KendaraanImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<KendaraanImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<KendaraanImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildImportHeaderMap(sheet, KendaraanImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = KendaraanRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                    return ApiResponse<KendaraanImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");

                // ---- PARSE ROWS ----
                var parsedRows = new List<KendaraanImportRow>();

                for (var row = KendaraanImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsImportRowEmpty(sheet, row, headerMap)) continue;

                    response.TotalRows++;

                    var parsed = new KendaraanImportRow
                    {
                        RowNumber = row,
                        NomorPolisi = GetCellText(sheet, row, headerMap, "Nopol"),
                        Merek = GetCellText(sheet, row, headerMap, "Merek"),
                        TipeName = GetCellText(sheet, row, headerMap, "Tipe"),
                        BahanBakarName = GetCellText(sheet, row, headerMap, "BahanBakar"),
                        KepemilikanName = GetCellText(sheet, row, headerMap, "Kepemilikan"),
                        JabatanName = GetCellText(sheet, row, headerMap, "Jabatan"),
                        NoPekerja = GetCellText(sheet, row, headerMap, "NoPekerja"),
                    };

                    // Required fields
                    ValidateRequired(response.Errors, row, "Nopol", parsed.NomorPolisi);
                    ValidateRequired(response.Errors, row, "Merek", parsed.Merek);
                    ValidateRequired(response.Errors, row, "Tipe", parsed.TipeName);
                    ValidateRequired(response.Errors, row, "BahanBakar", parsed.BahanBakarName);
                    ValidateRequired(response.Errors, row, "Kepemilikan", parsed.KepemilikanName);
                    ValidateRequired(response.Errors, row, "Jabatan", parsed.JabatanName);
                    // NoPekerja: opsional — tidak divalidate required

                    parsedRows.Add(parsed);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<KendaraanImportResponse>.BadRequest("Tidak ada data Kendaraan pada file Excel.");

                // ---- DUPLIKAT NOPOL DALAM FILE ----
                var duplicateNopolInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NomorPolisi))
                    .GroupBy(x => x.NomorPolisi.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                    !string.IsNullOrWhiteSpace(x.NomorPolisi) &&
                    duplicateNopolInFile.Contains(x.NomorPolisi.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Nopol",
                        Message = $"Nopol '{row.NomorPolisi}' duplikat di dalam file Excel."
                    });
                }

                // ---- NOPOL SUDAH ADA DI DB ----
                var nopolList = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NomorPolisi))
                    .Select(x => x.NomorPolisi.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingNopols = await _context.Kendaraans
                    .Where(x => !x.IsDeleted && nopolList.Contains(x.NomorPolisi))
                    .Select(x => x.NomorPolisi)
                    .ToListAsync();

                var existingNopolSet = existingNopols.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                    !string.IsNullOrWhiteSpace(x.NomorPolisi) &&
                    existingNopolSet.Contains(x.NomorPolisi.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Nopol",
                        Message = $"Nopol '{row.NomorPolisi}' sudah terdaftar."
                    });
                }

                // ---- LOAD SEMUA MASTER DATA SEKALI ----
                var tipes = await _context.Tipes.Where(x => !x.IsDeleted).ToListAsync();
                var bahanBakars = await _context.BahanBakars.Where(x => !x.IsDeleted).ToListAsync();
                var kepemilikans = await _context.Kepemilikans.Where(x => !x.IsDeleted).ToListAsync();
                var jabatans = await _context.Jabatans.Where(x => !x.IsDeleted).ToListAsync();
                var pekerjas = await _context.Pekerjas
                    .Include(p => p.Jabatan)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                // ---- VALIDASI MASTER PER ROW ----
                foreach (var row in parsedRows)
                {
                    if (!string.IsNullOrWhiteSpace(row.TipeName) &&
                        !tipes.Any(x => string.Equals(x.Name.Trim(), row.TipeName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Tipe",
                            Message = $"Tipe '{row.TipeName}' tidak ditemukan pada master Tipe."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(row.BahanBakarName) &&
                        !bahanBakars.Any(x => string.Equals(x.Name.Trim(), row.BahanBakarName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "BahanBakar",
                            Message = $"BahanBakar '{row.BahanBakarName}' tidak ditemukan pada master BahanBakar."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(row.KepemilikanName) &&
                        !kepemilikans.Any(x => string.Equals(x.Name.Trim(), row.KepemilikanName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Kepemilikan",
                            Message = $"Kepemilikan '{row.KepemilikanName}' tidak ditemukan pada master Kepemilikan."
                        });
                    }

                    // Jabatan -- simpan referensi object untuk dipakai di cek pejabat di bawah
                    var jabatan = jabatans.FirstOrDefault(x =>
                        string.Equals(x.Name.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(row.JabatanName) && jabatan == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Jabatan",
                            Message = $"Jabatan '{row.JabatanName}' tidak ditemukan pada master Jabatan."
                        });
                    }

                    // NoPekerja opsional — jika diisi, validasi ada & jabatan cocok
                    if (!string.IsNullOrWhiteSpace(row.NoPekerja))
                    {
                        var pekerja = pekerjas.FirstOrDefault(x =>
                            string.Equals(x.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (pekerja == null)
                        {
                            response.Errors.Add(new ImportRowError
                            {
                                RowNumber = row.RowNumber,
                                Column = "NoPekerja",
                                Message = $"NoPekerja '{row.NoPekerja}' tidak ditemukan pada master Pekerja."
                            });
                        }
                        else if (jabatan != null &&
                                 !string.Equals(pekerja.JabatanId, jabatan.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            // Jabatan pekerja tidak cocok dengan alokasi jabatan kendaraan
                            response.Errors.Add(new ImportRowError
                            {
                                RowNumber = row.RowNumber,
                                Column = "NoPekerja",
                                Message = $"Pejabat '{pekerja.NamaPekerja}' (NoPekerja: {row.NoPekerja}) " +
                                            $"memiliki jabatan '{pekerja.Jabatan?.Name}', " +
                                            $"tidak sesuai dengan alokasi jabatan kendaraan '{row.JabatanName}'."
                            });
                        }
                    }
                }

                // ---- ALL-OR-NOTHING: ada error -> tidak ada yang disimpan ----
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedKendaraan = 0;

                    return ApiResponse<KendaraanImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Kendaraan yang disimpan.");
                }

                // ---- BUILD & SAVE ----
                var now = DateTime.UtcNow;
                var newKendaraans = new List<Kendaraan>();

                foreach (var row in parsedRows)
                {
                    var tipe = tipes.First(x => string.Equals(x.Name.Trim(), row.TipeName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var bahanBakar = bahanBakars.First(x => string.Equals(x.Name.Trim(), row.BahanBakarName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var kepemilikan = kepemilikans.First(x => string.Equals(x.Name.Trim(), row.KepemilikanName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var jabatan = jabatans.First(x => string.Equals(x.Name.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase));

                    var pejabat = string.IsNullOrWhiteSpace(row.NoPekerja)
                        ? null
                        : pekerjas.FirstOrDefault(x =>
                            string.Equals(x.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    var kendaraan = new Kendaraan
                    {
                        NomorPolisi = row.NomorPolisi.Trim(),
                        Merek = row.Merek.Trim(),
                        TipeId = tipe.Id,
                        BahanBakarId = bahanBakar.Id,
                        KepemilikanId = kepemilikan.Id,
                        JabatanId = jabatan.Id,
                        PekerjaId = pejabat?.Id,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newKendaraans.Add(kendaraan);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new KendaraanImportPreviewDto
                        {
                            NomorPolisi = kendaraan.NomorPolisi,
                            Merek = kendaraan.Merek,
                            TipeName = tipe.Name,
                            BahanBakarName = bahanBakar.Name,
                            KepemilikanName = kepemilikan.Name,
                            JabatanName = jabatan.Name,
                            NamaPejabat = pejabat?.NamaPekerja ?? string.Empty
                        });
                    }
                }

                await _context.Kendaraans.AddRangeAsync(newKendaraans);
                await _context.SaveChangesAsync();

                response.InsertedKendaraan = newKendaraans.Count;
                response.SuccessCount = newKendaraans.Count;
                response.ErrorCount = 0;

                return ApiResponse<KendaraanImportResponse>.SuccessResponse(
                    response, $"{response.InsertedKendaraan} Kendaraan berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                var dbMessage = ex.InnerException?.Message ?? ex.Message;

                if (dbMessage.Contains("IX_Kendaraans_NomorPolisi", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "Nopol",
                        Message = "Terdapat Nopol yang sudah terdaftar."
                    });

                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedKendaraan = 0;

                    return ApiResponse<KendaraanImportResponse>.SuccessResponse(
                        response, "Import dibatalkan karena terdapat Nopol duplikat.");
                }

                return ApiResponse<KendaraanImportResponse>.ErrorResponse(
                    "ERR-KENDARAAN-IMPORT-001", "Terjadi kesalahan saat menyimpan data Kendaraan.");
            }
            catch (Exception ex)
            {
                return ApiResponse<KendaraanImportResponse>.ErrorResponse(
                    "ERR-KENDARAAN-IMPORT-002", $"Gagal mengimport data Kendaraan: {ex.Message}");
            }
        }

        // =========================================================
        // BULK UPLOAD — DOWNLOAD TEMPLATE
        // Sheet: Kendaraan | Referensi Tipe | Referensi BahanBakar |
        //        Referensi Kepemilikan | Referensi Jabatan | Referensi Pejabat
        // =========================================================

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Kendaraan");

            var headers = new[] { "Nopol", "Merek", "Tipe", "BahanBakar", "Kepemilikan", "Jabatan", "NoPekerjaAtasan" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(KendaraanImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(KendaraanImportHeaderRow, 1, KendaraanImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (italic abu-abu, boleh ditimpa/dihapus)
            var exampleValues = new[] { "B1234XYZ", "Toyota Innova", "MPV", "Bensin", "Dinas", "Manager", "19280027" };
            for (var i = 0; i < exampleValues.Length; i++)
                sheet.Cell(2, i + 1).Value = exampleValues[i];
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // ---- Sheet referensi Tipe ----
            var tipes = await _context.Tipes
                .Where(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.Name).ToListAsync();
            AddReferenceSheet(workbook, "Referensi Tipe", new[] { "Tipe (Valid)" },
                tipes.Select(x => new[] { x.Name }).ToList());

            // ---- Sheet referensi BahanBakar ----
            var bahanBakars = await _context.BahanBakars
                .Where(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.Name).ToListAsync();
            AddReferenceSheet(workbook, "Referensi BahanBakar", new[] { "BahanBakar (Valid)" },
                bahanBakars.Select(x => new[] { x.Name }).ToList());

            // ---- Sheet referensi Kepemilikan ----
            var kepemilikans = await _context.Kepemilikans
                .Where(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.Name).ToListAsync();
            AddReferenceSheet(workbook, "Referensi Kepemilikan", new[] { "Kepemilikan (Valid)" },
                kepemilikans.Select(x => new[] { x.Name }).ToList());

            // ---- Sheet referensi Jabatan ----
            var jabatans = await _context.Jabatans
                .Where(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.Name).ToListAsync();
            AddReferenceSheet(workbook, "Referensi Jabatan", new[] { "Jabatan (Valid)" },
                jabatans.Select(x => new[] { x.Name }).ToList());

            // ---- Sheet referensi Pejabat (NoPekerja + Nama + Jabatan) ----
            // Diurutkan per Jabatan supaya user mudah mencocokkan
            var pekerjas = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Jabatan != null ? x.Jabatan.Name : string.Empty)
                .ThenBy(x => x.NamaPekerja)
                .ToListAsync();
            AddReferenceSheet(workbook, "Referensi Pejabat",
                new[] { "NoPekerja (Valid)", "Nama Pekerja", "Jabatan" },
                pekerjas.Select(x => new[]
                {
                    x.NoPekerja,
                    x.NamaPekerja,
                    x.Jabatan?.Name ?? string.Empty
                }).ToList());

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_Kendaraan.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = new MemoryStream(bytes)
            });
        }

        // =========================================================
        // VALIDATE REFERENCES (Create & Update)
        // =========================================================

        private async Task<(string code, string message)?> ValidateReferencesAsync(
            string tipeId, string bahanBakarId,
            string kepemilikanId, string jabatanId, string? pekerjaId)
        {
            var tipeOk = await _context.Tipes.AnyAsync(t => t.Id == tipeId && !t.IsDeleted);
            if (!tipeOk)
                return ("ERR-KENDARAAN-003", "Tipe tidak ditemukan");

            var bahanBakarOk = await _context.BahanBakars.AnyAsync(b => b.Id == bahanBakarId && !b.IsDeleted);
            if (!bahanBakarOk)
                return ("ERR-KENDARAAN-004", "Bahan bakar tidak ditemukan");

            var kepemilikanOk = await _context.Kepemilikans.AnyAsync(k => k.Id == kepemilikanId && !k.IsDeleted);
            if (!kepemilikanOk)
                return ("ERR-KENDARAAN-006", "Kepemilikan tidak ditemukan");

            var jabatanOk = await _context.Jabatans.AnyAsync(j => j.Id == jabatanId && !j.IsDeleted);
            if (!jabatanOk)
                return ("ERR-KENDARAAN-007", "Alokasi jabatan tidak ditemukan");

            if (!string.IsNullOrEmpty(pekerjaId))
            {
                var pekerjaOk = await _context.Pekerjas.AnyAsync(p => p.Id == pekerjaId && !p.IsDeleted);
                if (!pekerjaOk)
                    return ("ERR-KENDARAAN-008", "Pejabat (pekerja) tidak ditemukan");
            }

            return null;
        }

        /// <summary>
        /// Cek apakah Kendaraan ini masih dipakai sebagai reference oleh RF.ID
        /// (kolom Nopol) yang masih hidup (belum di-soft-delete), sebelum boleh
        /// dinonaktifkan.
        /// </summary>
        private async Task<string?> GetDeactivationBlockerAsync(string kendaraanId)
        {
            if (await _context.RfIds.AnyAsync(r => r.KendaraanId == kendaraanId && !r.IsDeleted))
                return "Kendaraan masih memiliki RF.ID yang ter-assign, tidak bisa dinonaktifkan.";

            return null;
        }

        // =========================================================
        // MAP TO DTO
        // =========================================================

        private static KendaraanDto MapToDto(Kendaraan k) => new()
        {
            Id = k.Id,
            NomorPolisi = k.NomorPolisi,
            TipeId = k.TipeId,
            TipeName = k.Tipe?.Name ?? string.Empty,
            BahanBakarId = k.BahanBakarId,
            BahanBakarName = k.BahanBakar?.Name ?? string.Empty,
            Merek = k.Merek,
            KepemilikanId = k.KepemilikanId,
            KepemilikanName = k.Kepemilikan?.Name ?? string.Empty,
            JabatanId = k.JabatanId,
            JabatanName = k.Jabatan?.Name ?? string.Empty,
            PekerjaId = k.PekerjaId,
            PekerjaName = k.Pekerja?.NamaPekerja,
            IsActive = k.IsActive,
            CreatedAt = k.CreatedAt,
            ModifiedAt = k.ModifiedAt
        };

        // =========================================================
        // IMPORT HELPERS (private)
        // =========================================================

        private static Dictionary<string, int> BuildImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var col = 1; col <= lastColumn; col++)
            {
                var header = sheet.Cell(headerRow, col).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, col);
            }

            return result;
        }

        private static string NormalizeHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeHeader(header);
            if (!headerMap.TryGetValue(normalized, out var col)) return string.Empty;
            return sheet.Cell(row, col).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var nopol = GetCellText(sheet, row, headerMap, "Nopol");
            var merek = GetCellText(sheet, row, headerMap, "Merek");
            return string.IsNullOrWhiteSpace(nopol) && string.IsNullOrWhiteSpace(merek);
        }

        private static void ValidateRequired(
            List<ImportRowError> errors, int row, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = column,
                    Message = $"Data wajib '{column}' kosong."
                });
        }

        private static void AddReferenceSheet(
            XLWorkbook workbook, string sheetName, string[] headers, List<string[]> rows)
        {
            var refSheet = workbook.Worksheets.Add(sheetName);

            for (var i = 0; i < headers.Length; i++)
            {
                refSheet.Cell(1, i + 1).Value = headers[i];
                refSheet.Cell(1, i + 1).Style.Font.Bold = true;
            }

            for (var r = 0; r < rows.Count; r++)
                for (var c = 0; c < rows[r].Length; c++)
                    refSheet.Cell(r + 2, c + 1).Value = rows[r][c];

            refSheet.Columns().AdjustToContents();
        }
    }
}