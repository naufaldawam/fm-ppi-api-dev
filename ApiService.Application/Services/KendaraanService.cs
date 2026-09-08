using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using System.IO;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace ApiService.Application.Services
{
    public interface IKendaraanService
    {
        Task<ApiResponse<PagedResponse<KendaraanDto>>> GetAllAsync(KendaraanFilterRequest filter);
        Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true);
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

        private IQueryable<Kendaraan> BaseQuery() =>
            _context.Kendaraans
                .Include(k => k.Tipe)
                .Include(k => k.BahanBakar)
                .Include(k => k.Kepemilikan)
                .Include(k => k.Jabatan)
                .Include(k => k.Pekerja)
                .Where(k => !k.IsDeleted);

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

        public async Task<ApiResponse<List<KendaraanLookupDto>>> GetLookupAsync(string? search, bool activeOnly = true)
        {
            var query = _context.Kendaraans
                .Where(k => !k.IsDeleted);

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
        public async Task<ApiResponse<KendaraanDto>> GetByIdAsync(string id)
        {
            var kendaraan = await BaseQuery().FirstOrDefaultAsync(k => k.Id == id);

            if (kendaraan == null)
                return ApiResponse<KendaraanDto>.ErrorResponse("ERR-KENDARAAN-001", "Kendaraan not found");

            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(kendaraan));
        }

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

            var kendaraan = new Kendaraan
            {
                NomorPolisi = request.NomorPolisi,
                TipeId = request.TipeId,
                BahanBakarId = request.BahanBakarId,
                Merek = request.Merek,
                KepemilikanId = request.KepemilikanId,
                JabatanId = request.JabatanId,
                PekerjaId = string.IsNullOrEmpty(request.PekerjaId) ? null : request.PekerjaId,
                IsActive = request.IsActive,
                CreatedBy = userId
            };

            _context.Kendaraans.Add(kendaraan);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(k => k.Id == kendaraan.Id);
            return ApiResponse<KendaraanDto>.SuccessResponse(MapToDto(created), "Kendaraan berhasil ditambahkan");
        }

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

        private const int KendaraanImportHeaderRow = 1;
        private const int KendaraanImportDataStartRow = 2;
        
        private static readonly string[] KendaraanRequiredImportHeaders =
        {
            "nopol", "merek", "tipe", "bahanbakar", "kepemilikan", "jabatan"
        };
        
        // =========================================================
        // BULK UPLOAD KENDARAAN
        // Template kolom: Nopol | Merek | Tipe | BahanBakar | Kepemilikan | Jabatan
        // Semua lookup via Name (bukan Id) karena user tidak tahu Id master data
        // =========================================================
        
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
        
                var headerMap = BuildKendaraanImportHeaderMap(sheet, KendaraanImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();
        
                var missingHeaders = KendaraanRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();
        
                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<KendaraanImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }
        
                var parsedRows = new List<KendaraanImportRow>();
        
                for (var row = KendaraanImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsKendaraanImportRowEmpty(sheet, row, headerMap))
                        continue;
        
                    response.TotalRows++;
        
                    var parsedRow = new KendaraanImportRow
                    {
                        RowNumber = row,
                        NomorPolisi    = GetKendaraanImportCellText(sheet, row, headerMap, "Nopol"),
                        Merek          = GetKendaraanImportCellText(sheet, row, headerMap, "Merek"),
                        TipeName       = GetKendaraanImportCellText(sheet, row, headerMap, "Tipe"),
                        BahanBakarName = GetKendaraanImportCellText(sheet, row, headerMap, "BahanBakar"),
                        KepemilikanName = GetKendaraanImportCellText(sheet, row, headerMap, "Kepemilikan"),
                        JabatanName    = GetKendaraanImportCellText(sheet, row, headerMap, "Jabatan"),
                    };
        
                    ValidateKendaraanRequiredField(response.Errors, row, "Nopol",       parsedRow.NomorPolisi);
                    ValidateKendaraanRequiredField(response.Errors, row, "Merek",       parsedRow.Merek);
                    ValidateKendaraanRequiredField(response.Errors, row, "Tipe",        parsedRow.TipeName);
                    ValidateKendaraanRequiredField(response.Errors, row, "BahanBakar",  parsedRow.BahanBakarName);
                    ValidateKendaraanRequiredField(response.Errors, row, "Kepemilikan", parsedRow.KepemilikanName);
                    ValidateKendaraanRequiredField(response.Errors, row, "Jabatan",     parsedRow.JabatanName);
        
                    parsedRows.Add(parsedRow);
                }
        
                if (parsedRows.Count == 0)
                    return ApiResponse<KendaraanImportResponse>.BadRequest("Tidak ada data Kendaraan pada file Excel.");
        
                // =========================
                // VALIDASI DUPLIKAT NOPOL DI DALAM FILE
                // =========================
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
        
                // =========================
                // VALIDASI NOPOL SUDAH TERDAFTAR DI DB
                // =========================
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
        
                // =========================
                // LOAD SEMUA MASTER DATA SEKALIGUS (efisien, 1 query per master)
                // =========================
                var tipes        = await _context.Tipes.Where(x => !x.IsDeleted).ToListAsync();
                var bahanBakars  = await _context.BahanBakars.Where(x => !x.IsDeleted).ToListAsync();
                var kepemilikans = await _context.Kepemilikans.Where(x => !x.IsDeleted).ToListAsync();
                var jabatans     = await _context.Jabatans.Where(x => !x.IsDeleted).ToListAsync();
        
                // =========================
                // VALIDASI REFERENSI MASTER DATA PER BARIS
                // =========================
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
        
                    if (!string.IsNullOrWhiteSpace(row.JabatanName) &&
                        !jabatans.Any(x => string.Equals(x.Name.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Jabatan",
                            Message = $"Jabatan '{row.JabatanName}' tidak ditemukan pada master Jabatan."
                        });
                    }
                }
        
                // =========================
                // JIKA ADA ERROR VALIDASI -> TIDAK ADA YANG DISIMPAN
                // =========================
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedKendaraan = 0;
        
                    return ApiResponse<KendaraanImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Kendaraan yang disimpan.");
                }
        
                // =========================
                // BUILD & SAVE
                // =========================
                var now = DateTime.UtcNow;
                var newKendaraans = new List<Kendaraan>();
        
                foreach (var row in parsedRows)
                {
                    var tipe       = tipes.First(x => string.Equals(x.Name.Trim(), row.TipeName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var bahanBakar = bahanBakars.First(x => string.Equals(x.Name.Trim(), row.BahanBakarName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var kepemilikan = kepemilikans.First(x => string.Equals(x.Name.Trim(), row.KepemilikanName.Trim(), StringComparison.OrdinalIgnoreCase));
                    var jabatan    = jabatans.First(x => string.Equals(x.Name.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase));
        
                    var kendaraan = new Kendaraan
                    {
                        NomorPolisi  = row.NomorPolisi.Trim(),
                        Merek        = row.Merek.Trim(),
                        TipeId       = tipe.Id,
                        BahanBakarId = bahanBakar.Id,
                        KepemilikanId = kepemilikan.Id,
                        JabatanId    = jabatan.Id,
                        PekerjaId    = null,   // bulk upload tidak mengisi pejabat pemegang
                        IsActive     = true,
                        CreatedBy    = userId,
                        CreatedAt    = now
                    };
        
                    newKendaraans.Add(kendaraan);
        
                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new KendaraanImportPreviewDto
                        {
                            NomorPolisi    = kendaraan.NomorPolisi,
                            Merek          = kendaraan.Merek,
                            TipeName       = tipe.Name,
                            BahanBakarName = bahanBakar.Name,
                            KepemilikanName = kepemilikan.Name,
                            JabatanName    = jabatan.Name
                        });
                    }
                }
        
                await _context.Kendaraans.AddRangeAsync(newKendaraans);
                await _context.SaveChangesAsync();
        
                response.InsertedKendaraan = newKendaraans.Count;
                response.SuccessCount = newKendaraans.Count;
                response.ErrorCount = 0;
        
                return ApiResponse<KendaraanImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedKendaraan} Kendaraan berhasil diimport.");
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
                        response,
                        "Import dibatalkan karena terdapat Nopol duplikat.");
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
        
        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Kendaraan");
        
            var headers = new[] { "Nopol", "Merek", "Tipe", "BahanBakar", "Kepemilikan", "Jabatan" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(KendaraanImportHeaderRow, i + 1).Value = headers[i];
        
            var headerRange = sheet.Range(KendaraanImportHeaderRow, 1, KendaraanImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        
            // Baris contoh pengisian (italic abu-abu — harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "B1234XYZ";
            sheet.Cell(2, 2).Value = "Toyota Innova";
            sheet.Cell(2, 3).Value = "MPV";
            sheet.Cell(2, 4).Value = "Bensin";
            sheet.Cell(2, 5).Value = "PT Astra";
            sheet.Cell(2, 6).Value = "Dinas";
            sheet.Cell(2, 7).Value = "Manager";
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;
        
            sheet.Columns().AdjustToContents();
        
            // Sheet referensi Tipe
            var tipes = await _context.Tipes
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        
            var tipeRefSheet = workbook.Worksheets.Add("Referensi Tipe");
            tipeRefSheet.Cell(1, 1).Value = "Tipe (Valid)";
            tipeRefSheet.Cell(1, 1).Style.Font.Bold = true;
            for (var i = 0; i < tipes.Count; i++)
                tipeRefSheet.Cell(i + 2, 1).Value = tipes[i].Name;
            tipeRefSheet.Columns().AdjustToContents();
        
            // Sheet referensi BahanBakar
            var bahanBakars = await _context.BahanBakars
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        
            var bahanBakarRefSheet = workbook.Worksheets.Add("Referensi BahanBakar");
            bahanBakarRefSheet.Cell(1, 1).Value = "BahanBakar (Valid)";
            bahanBakarRefSheet.Cell(1, 1).Style.Font.Bold = true;
            for (var i = 0; i < bahanBakars.Count; i++)
                bahanBakarRefSheet.Cell(i + 2, 1).Value = bahanBakars[i].Name;
            bahanBakarRefSheet.Columns().AdjustToContents();
        
            // Sheet referensi Kepemilikan
            var kepemilikans = await _context.Kepemilikans
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        
            var kepemilikanRefSheet = workbook.Worksheets.Add("Referensi Kepemilikan");
            kepemilikanRefSheet.Cell(1, 1).Value = "Kepemilikan (Valid)";
            kepemilikanRefSheet.Cell(1, 1).Style.Font.Bold = true;
            for (var i = 0; i < kepemilikans.Count; i++)
                kepemilikanRefSheet.Cell(i + 2, 1).Value = kepemilikans[i].Name;
            kepemilikanRefSheet.Columns().AdjustToContents();
        
            // Sheet referensi Jabatan
            var jabatans = await _context.Jabatans
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();
        
            var jabatanRefSheet = workbook.Worksheets.Add("Referensi Jabatan");
            jabatanRefSheet.Cell(1, 1).Value = "Jabatan (Valid)";
            jabatanRefSheet.Cell(1, 1).Style.Font.Bold = true;
            for (var i = 0; i < jabatans.Count; i++)
                jabatanRefSheet.Cell(i + 2, 1).Value = jabatans[i].Name;
            jabatanRefSheet.Columns().AdjustToContents();
        
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }
        
            var resultStream = new MemoryStream(bytes);
        
            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_Kendaraan.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }
        
        // =========================================================
        // HELPERS (private, khusus import Kendaraan)
        // =========================================================
        
        private static Dictionary<string, int> BuildKendaraanImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;
        
            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;
        
                var normalized = NormalizeKendaraanImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }
        
            return result;
        }
        
        private static string NormalizeKendaraanImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        
        private static string GetKendaraanImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeKendaraanImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;
        
            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }
        
        private static bool IsKendaraanImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var nopol = GetKendaraanImportCellText(sheet, row, headerMap, "Nopol");
            var merek = GetKendaraanImportCellText(sheet, row, headerMap, "Merek");
            return string.IsNullOrWhiteSpace(nopol) && string.IsNullOrWhiteSpace(merek);
        }
        
        private static void ValidateKendaraanRequiredField(
            List<ImportRowError> errors, int row, string column, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = column,
                    Message = $"Data wajib '{column}' kosong."
                });
            }
        }

        /// <summary>
        /// Memastikan semua referensi master data (Tipe, BahanBakar, Kepemilikan,
        /// Jabatan) valid dan tidak terhapus, dan Pekerja (jika diisi) juga valid.
        /// </summary>
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
    }
}