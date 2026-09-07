using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;

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
        Task<ApiResponse<PekerjaImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
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
                    NamaPekerja = p.NamaPekerja,
                    JabatanId = p.JabatanId,
                    JabatanName = p.Jabatan != null ? p.Jabatan.Name : string.Empty
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

        // =========================================================
        // BULK UPLOAD PEKERJA
        // =========================================================

        private const int ImportHeaderRow = 1;
        private const int ImportDataStartRow = 2;

        private static readonly string[] RequiredImportHeaders =
        {
            "nopekerja", "nopekhome", "nopekhost", "namapekerja", "jabatan"
        };

        public async Task<ApiResponse<PekerjaImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new PekerjaImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<PekerjaImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<PekerjaImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<PekerjaImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildImportHeaderMap(sheet, ImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<PekerjaImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<PekerjaImportRow>();

                for (var row = ImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new PekerjaImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetImportCellText(sheet, row, headerMap, "No.Pekerja"),
                        NopekHome = GetImportCellText(sheet, row, headerMap, "Nopek Home"),
                        NopekHost = GetImportCellText(sheet, row, headerMap, "Nopek Host"),
                        NamaPekerja = GetImportCellText(sheet, row, headerMap, "Nama Pekerja"),
                        JabatanName = GetImportCellText(sheet, row, headerMap, "Jabatan")
                    };

                    ValidateRequiredImportField(response.Errors, row, "No.Pekerja", parsedRow.NoPekerja);
                    ValidateRequiredImportField(response.Errors, row, "Nopek Home", parsedRow.NopekHome);
                    ValidateRequiredImportField(response.Errors, row, "Nopek Host", parsedRow.NopekHost);
                    ValidateRequiredImportField(response.Errors, row, "Nama Pekerja", parsedRow.NamaPekerja);
                    ValidateRequiredImportField(response.Errors, row, "Jabatan", parsedRow.JabatanName);

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<PekerjaImportResponse>.BadRequest("Tidak ada data pekerja pada file Excel.");

                // =========================
                // VALIDASI DUPLIKAT NO.PEKERJA DI DALAM FILE
                // =========================
                var duplicateNoPekerjaInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja))
                    .GroupBy(x => x.NoPekerja.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja) && 
                            duplicateNoPekerjaInFile.Contains(x.NoPekerja.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "No.Pekerja",
                        Message = $"No.Pekerja '{row.NoPekerja}' duplikat di dalam file Excel."
                    });
                }

                // =========================
                // VALIDASI NO.PEKERJA SUDAH TERDAFTAR DI DB
                // =========================
                var noPekerjaList = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja))
                    .Select(x => x.NoPekerja.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existingNoPekerja = await _context.Pekerjas
                    .Where(x => !x.IsDeleted && noPekerjaList.Contains(x.NoPekerja))
                    .Select(x => x.NoPekerja)
                    .ToListAsync();

                var existingNoPekerjaSet = existingNoPekerja.ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                             existingNoPekerjaSet.Contains(x.NoPekerja.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "No.Pekerja",
                        Message = $"No.Pekerja '{row.NoPekerja}' sudah terdaftar."
                    });
                }

                // =========================
                // VALIDASI JABATAN (harus sudah ada di master, tidak auto-create)
                // =========================
                var jabatans = await _context.Jabatans
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.JabatanName))
                        continue; // sudah ditangani oleh ValidateRequiredImportField

                    var match = jabatans.FirstOrDefault(x =>
                        string.Equals(x.Name?.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (match == null)
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
                    response.InsertedPekerja = 0;

                    return ApiResponse<PekerjaImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data pekerja yang disimpan.");
                }

                // =========================
                // BUILD & SAVE
                // =========================
                var now = DateTime.UtcNow;
                var newPekerja = new List<Pekerja>();

                foreach (var row in parsedRows)
                {
                    var jabatan = jabatans.First(x =>
                        string.Equals(x.Name?.Trim(), row.JabatanName.Trim(), StringComparison.OrdinalIgnoreCase));

                    var pekerja = new Pekerja
                    {
                        NoPekerja = row.NoPekerja.Trim(),
                        NopekHome = row.NopekHome.Trim(),
                        NopekHost = row.NopekHost.Trim(),
                        NamaPekerja = row.NamaPekerja.Trim(),
                        JabatanId = jabatan.Id,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newPekerja.Add(pekerja);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new PekerjaImportPreviewDto
                        {
                            NoPekerja = pekerja.NoPekerja,
                            NamaPekerja = pekerja.NamaPekerja,
                            JabatanName = jabatan.Name
                        });
                    }
                }

                await _context.Pekerjas.AddRangeAsync(newPekerja);
                await _context.SaveChangesAsync();

                response.InsertedPekerja = newPekerja.Count;
                response.SuccessCount = newPekerja.Count;
                response.ErrorCount = 0;

                return ApiResponse<PekerjaImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedPekerja} pekerja berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                var dbMessage = ex.InnerException?.Message ?? ex.Message;

                if (dbMessage.Contains("IX_Pekerjas_NoPekerja", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "No.Pekerja",
                        Message = "Terdapat No.Pekerja yang sudah terdaftar."
                    });

                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedPekerja = 0;

                    return ApiResponse<PekerjaImportResponse>.SuccessResponse(
                        response,
                        "Import dibatalkan karena terdapat No.Pekerja duplikat.");
                }

                return ApiResponse<PekerjaImportResponse>.ErrorResponse(
                    "ERR-PEKERJA-IMPORT-001", "Terjadi kesalahan saat menyimpan data pekerja.");
            }
            catch (Exception ex)
            {
                return ApiResponse<PekerjaImportResponse>.ErrorResponse(
                    "ERR-PEKERJA-IMPORT-002", $"Gagal mengimport data pekerja: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Pekerja");

            var headers = new[] { "No.Pekerja", "Nopek Home", "Nopek Host", "Nama Pekerja", "Jabatan" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(ImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(ImportHeaderRow, 1, ImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh pengisian (bukan data, tetap harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "19280027";
            sheet.Cell(2, 2).Value = "NME19280027";
            sheet.Cell(2, 3).Value = "NST234233";
            sheet.Cell(2, 4).Value = "John Anis";
            sheet.Cell(2, 5).Value = "Direksi";
            sheet.Range(2, 1, 2, 5).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, 5).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // Sheet referensi daftar Jabatan aktif, supaya user tahu nilai valid untuk kolom "Jabatan"
            var jabatans = await _context.Jabatans
                .Where(j => !j.IsDeleted && j.IsActive)
                .OrderBy(j => j.Name)
                .ToListAsync();

            var refSheet = workbook.Worksheets.Add("Referensi Jabatan");
            refSheet.Cell(1, 1).Value = "Nama Jabatan (Valid)";
            refSheet.Cell(1, 1).Style.Font.Bold = true;

            for (var i = 0; i < jabatans.Count; i++)
                refSheet.Cell(i + 2, 1).Value = jabatans[i].Name;

            refSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            var resultStream = new MemoryStream(bytes);

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_Pekerja.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        private static Dictionary<string, int> BuildImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetImportCellText(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalizedHeader = NormalizeImportHeader(header);
            if (!headerMap.TryGetValue(normalizedHeader, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsImportRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var noPekerja = GetImportCellText(sheet, row, headerMap, "No.Pekerja");
            var namaPekerja = GetImportCellText(sheet, row, headerMap, "Nama Pekerja");
            return string.IsNullOrWhiteSpace(noPekerja) && string.IsNullOrWhiteSpace(namaPekerja);
        }

        private static void ValidateRequiredImportField(List<ImportRowError> errors, int row, string column, string value)
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