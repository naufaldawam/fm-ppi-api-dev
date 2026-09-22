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
    public interface IPerjalananDinasService
    {
        Task<ApiResponse<PagedResponse<PerjalananDinasDto>>> GetAllAsync(PerjalananDinasFilterRequest filter);
        Task<ApiResponse<PerjalananDinasDto>> GetByIdAsync(string id);
        Task<ApiResponse<PerjalananDinasDto>> CreateAsync(CreatePerjalananDinasRequest request, string userId);
        Task<ApiResponse<PerjalananDinasDto>> UpdateAsync(string id, UpdatePerjalananDinasRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<PerjalananDinasSummaryDto>> GetSummaryAsync(PerjalananDinasSummaryRequest filter);
        Task<ApiResponse<PerjalananDinasImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class PerjalananDinasService : IPerjalananDinasService
    {
        private readonly IServiceDbContext _context;

        public PerjalananDinasService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<PerjalananDinas> BaseQuery() =>
            _context.PerjalananDinas
                .Include(p => p.Pekerja)
                    .ThenInclude(pk => pk!.Jabatan)
                .Include(p => p.Periode)
                .Where(p => !p.IsDeleted);

        public async Task<ApiResponse<PagedResponse<PerjalananDinasDto>>> GetAllAsync(PerjalananDinasFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(p =>
                    (p.Pekerja != null && p.Pekerja.NoPekerja.Contains(s)) ||
                    (p.Pekerja != null && p.Pekerja.NamaPekerja.Contains(s)) ||
                    (p.Pekerja != null && p.Pekerja.NopekHome.Contains(s)) ||
                    (p.Pekerja != null && p.Pekerja.NopekHost.Contains(s)));
            }

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(p => p.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.BulanTahun)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<PerjalananDinasDto>>.SuccessResponse(new PagedResponse<PerjalananDinasDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<PerjalananDinasDto>> GetByIdAsync(string id)
        {
            var dinas = await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);

            if (dinas == null)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-001", "Perjalanan dinas not found");

            return ApiResponse<PerjalananDinasDto>.SuccessResponse(MapToDto(dinas));
        }

        public async Task<ApiResponse<PerjalananDinasDto>> CreateAsync(CreatePerjalananDinasRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-002", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-003", "Periode tidak ditemukan");

            var duplicate = await _context.PerjalananDinas.AnyAsync(p =>
                !p.IsDeleted &&
                p.PekerjaId == request.PekerjaId &&
                p.PeriodeId == request.PeriodeId &&
                p.BulanTahun.Date == request.BulanTahun.Date);

            if (duplicate)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse(
                    "ERR-PERJALANANDINAS-004",
                    "Perjalanan dinas pekerja ini untuk bulan-periode ini sudah terdaftar");

            var dinas = new PerjalananDinas
            {
                PekerjaId = request.PekerjaId,
                BulanTahun = request.BulanTahun,
                PeriodeId = request.PeriodeId,
                TotalBiayaDinas = request.TotalBiayaDinas,
                IsActive = true,
                CreatedBy = userId
            };

            _context.PerjalananDinas.Add(dinas);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(p => p.Id == dinas.Id);
            return ApiResponse<PerjalananDinasDto>.SuccessResponse(MapToDto(created), "Perjalanan dinas berhasil ditambahkan");
        }

        public async Task<ApiResponse<PerjalananDinasDto>> UpdateAsync(string id, UpdatePerjalananDinasRequest request, string userId)
        {
            var dinas = await _context.PerjalananDinas
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (dinas == null)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-001", "Perjalanan dinas not found");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-002", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse("ERR-PERJALANANDINAS-003", "Periode tidak ditemukan");

            var duplicate = await _context.PerjalananDinas.AnyAsync(p =>
                !p.IsDeleted &&
                p.Id != id &&
                p.PekerjaId == request.PekerjaId &&
                p.PeriodeId == request.PeriodeId &&
                p.BulanTahun.Date == request.BulanTahun.Date);

            if (duplicate)
                return ApiResponse<PerjalananDinasDto>.ErrorResponse(
                    "ERR-PERJALANANDINAS-004",
                    "Perjalanan dinas pekerja ini untuk bulan-periode ini sudah terdaftar");

            dinas.PekerjaId = request.PekerjaId;
            dinas.BulanTahun = request.BulanTahun;
            dinas.PeriodeId = request.PeriodeId;
            dinas.TotalBiayaDinas = request.TotalBiayaDinas;
            dinas.IsActive = request.IsActive;
            dinas.ModifiedAt = DateTime.UtcNow;
            dinas.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(p => p.Id == dinas.Id);
            return ApiResponse<PerjalananDinasDto>.SuccessResponse(MapToDto(updated), "Perjalanan dinas berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var dinas = await _context.PerjalananDinas
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (dinas == null)
                return ApiResponse<bool>.ErrorResponse("ERR-PERJALANANDINAS-001", "Perjalanan dinas not found");

            dinas.IsDeleted = true;
            dinas.DeletedAt = DateTime.UtcNow;
            dinas.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Perjalanan dinas deleted");
        }

        // =========================================================
        // SUMMARY: total dinas pekerja (count) + total perjalanan dinas (sum)
        // =========================================================
        public async Task<ApiResponse<PerjalananDinasSummaryDto>> GetSummaryAsync(PerjalananDinasSummaryRequest filter)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<PerjalananDinasSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            var query = _context.PerjalananDinas.Where(p => !p.IsDeleted);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(p => p.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            var totalDinasPekerja = await query.CountAsync();
            var totalPerjalananDinas = totalDinasPekerja == 0 ? 0m : await query.SumAsync(p => p.TotalBiayaDinas);

            return ApiResponse<PerjalananDinasSummaryDto>.SuccessResponse(new PerjalananDinasSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalDinasPekerja = totalDinasPekerja,
                TotalPerjalananDinas = totalPerjalananDinas
            });
        }

        // =========================================================
        // BULK UPLOAD PERJALANAN DINAS
        // Template kolom: NoPekerja | BulanTahun | Periode | TotalBiayaDinas
        // Lookup Pekerja via NoPekerja; Periode via NamaPeriode
        // =========================================================

        private const int ImportHeaderRow = 1;
        private const int ImportDataStartRow = 2;

        private static readonly string[] RequiredImportHeaders =
        {
            "nopekerja", "bulantahun", "periode", "totalbiayadinas"
        };

        public async Task<ApiResponse<PerjalananDinasImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new PerjalananDinasImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<PerjalananDinasImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<PerjalananDinasImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<PerjalananDinasImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildImportHeaderMap(sheet, ImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<PerjalananDinasImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<PerjalananDinasImportRow>();

                for (var row = ImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new PerjalananDinasImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetImportCellText(sheet, row, headerMap, "NoPekerja"),
                        BulanTahunText = GetImportCellText(sheet, row, headerMap, "BulanTahun"),
                        PeriodeName = GetImportCellText(sheet, row, headerMap, "Periode"),
                        TotalBiayaDinasText = GetImportCellText(sheet, row, headerMap, "TotalBiayaDinas")
                    };

                    ValidateRequiredField(response.Errors, row, "NoPekerja", parsedRow.NoPekerja);
                    ValidateRequiredField(response.Errors, row, "BulanTahun", parsedRow.BulanTahunText);
                    ValidateRequiredField(response.Errors, row, "Periode", parsedRow.PeriodeName);
                    ValidateRequiredField(response.Errors, row, "TotalBiayaDinas", parsedRow.TotalBiayaDinasText);

                    parsedRow.BulanTahun = ParseBulanTahun(parsedRow.BulanTahunText);
                    if (parsedRow.BulanTahun == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "BulanTahun",
                            Message = "BulanTahun tidak valid. Format: NamaBulan, TAHUN (contoh: Januari, 2024)."
                        });
                    }

                    parsedRow.TotalBiayaDinas = ParseImportDecimal(parsedRow.TotalBiayaDinasText);
                    if (parsedRow.TotalBiayaDinas == null || parsedRow.TotalBiayaDinas <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "TotalBiayaDinas",
                            Message = "TotalBiayaDinas tidak valid. Isi angka murni, contoh: 1500000."
                        });
                    }

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<PerjalananDinasImportResponse>.BadRequest("Tidak ada data Perjalanan Dinas pada file Excel.");

                // —— Duplikat (NoPekerja + Periode + BulanTahun) di dalam file ——
                var keyInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                                !string.IsNullOrWhiteSpace(x.PeriodeName) &&
                                x.BulanTahun != null)
                    .GroupBy(x => $"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}|{x.BulanTahun}",
                        StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             x.BulanTahun != null &&
                             !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                             !string.IsNullOrWhiteSpace(x.PeriodeName) &&
                             keyInFile.Contains($"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}|{x.BulanTahun}")))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "NoPekerja",
                        Message = $"Perjalanan dinas NoPekerja '{row.NoPekerja}' di periode '{row.PeriodeName}' duplikat di dalam file Excel."
                    });
                }

                // —— Load master sekali (Pekerja + Jabatan untuk preview) ——
                var pekerjas = await _context.Pekerjas
                    .Include(p => p.Jabatan)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();
                var periodes = await _context.Periodes.Where(x => !x.IsDeleted).ToListAsync();
                var existingDinas = await _context.PerjalananDinas.Where(x => !x.IsDeleted).ToListAsync();

                var existingKeys = existingDinas
                    .Select(d => $"{d.PekerjaId}|{d.PeriodeId}|{d.BulanTahun.Date}")
                    .ToHashSet();

                var contextRows = new List<PerjalananDinasImportContext>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja) ||
                        string.IsNullOrWhiteSpace(row.PeriodeName) ||
                        row.BulanTahun == null)
                    {
                        continue; // sudah ditangani required / validasi
                    }

                    var pekerja = pekerjas.FirstOrDefault(p =>
                        string.Equals(p.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (pekerja == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"NoPekerja '{row.NoPekerja}' tidak ditemukan pada master Pekerja."
                        });
                        continue;
                    }

                    var periode = periodes.FirstOrDefault(p =>
                        string.Equals(p.NamaPeriode?.Trim(), row.PeriodeName.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (periode == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Periode",
                            Message = $"Periode '{row.PeriodeName}' tidak ditemukan pada master Periode."
                        });
                        continue;
                    }

                    var existingKey = $"{pekerja.Id}|{periode.Id}|{row.BulanTahun.Value.Date}";
                    if (existingKeys.Contains(existingKey))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"Perjalanan dinas NoPekerja '{row.NoPekerja}' bulan '{row.BulanTahun.Value.Date:yyyy-MM-dd}' di periode '{row.PeriodeName}' sudah terdaftar."
                        });
                        continue;
                    }

                    contextRows.Add(new PerjalananDinasImportContext
                    {
                        Row = row,
                        Pekerja = pekerja,
                        Periode = periode
                    });
                }

                // —— All-or-nothing: ada error = tidak ada yang disimpan ——
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedPerjalananDinas = 0;

                    return ApiResponse<PerjalananDinasImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Perjalanan Dinas yang disimpan.");
                }

                // —— Build & save ——
                var now = DateTime.UtcNow;
                var newDinas = new List<PerjalananDinas>();

                foreach (var ctx in contextRows)
                {
                    var dinas = new PerjalananDinas
                    {
                        PekerjaId = ctx.Pekerja.Id,
                        BulanTahun = (DateTime)ctx.Row.BulanTahun,
                        PeriodeId = ctx.Periode.Id,
                        TotalBiayaDinas = (decimal)ctx.Row.TotalBiayaDinas,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newDinas.Add(dinas);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new PerjalananDinasImportPreviewDto
                        {
                            NoPekerja = ctx.Row.NoPekerja.Trim(),
                            NamaPekerja = ctx.Pekerja.NamaPekerja,
                            JabatanName = ctx.Pekerja.Jabatan?.Name ?? string.Empty,
                            BulanTahun = dinas.BulanTahun,
                            NamaPeriode = ctx.Periode.NamaPeriode,
                            TotalBiayaDinas = dinas.TotalBiayaDinas
                        });
                    }
                }

                await _context.PerjalananDinas.AddRangeAsync(newDinas);
                await _context.SaveChangesAsync();

                response.InsertedPerjalananDinas = newDinas.Count;
                response.SuccessCount = newDinas.Count;
                response.ErrorCount = 0;

                return ApiResponse<PerjalananDinasImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedPerjalananDinas} Perjalanan Dinas berhasil diimport.");
            }
            catch (Exception ex)
            {
                return ApiResponse<PerjalananDinasImportResponse>.ErrorResponse(
                    "ERR-PERJALANANDINAS-IMPORT-001", $"Gagal mengimport data Perjalanan Dinas: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("PerjalananDinas");

            var headers = new[] { "NoPekerja", "BulanTahun", "Periode", "TotalBiayaDinas" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(ImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(ImportHeaderRow, 1, ImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (italic abu-abu - harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "19280027";
            sheet.Cell(2, 2).Value = "Januari, 2026";
            sheet.Cell(2, 3).Value = "Januari 2026";
            sheet.Cell(2, 4).Value = "1500000";
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

            // Sheet referensi Pekerja - NoPekerja + NamaPekerja + Jabatan
            var pekerjas = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPekerja)
                .ToListAsync();

            var pekerjaRefSheet = workbook.Worksheets.Add("Referensi Pekerja");
            pekerjaRefSheet.Cell(1, 1).Value = "NoPekerja (Valid)";
            pekerjaRefSheet.Cell(1, 2).Value = "Nama Pekerja";
            pekerjaRefSheet.Cell(1, 3).Value = "Jabatan";
            pekerjaRefSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < pekerjas.Count; i++)
            {
                pekerjaRefSheet.Cell(i + 2, 1).Value = pekerjas[i].NoPekerja;
                pekerjaRefSheet.Cell(i + 2, 2).Value = pekerjas[i].NamaPekerja;
                pekerjaRefSheet.Cell(i + 2, 3).Value = pekerjas[i].Jabatan?.Name ?? string.Empty;
            }
            pekerjaRefSheet.Columns().AdjustToContents();

            // Sheet referensi Periode - NamaPeriode + TanggalAwal + TanggalAkhir
            var periodes = await _context.Periodes
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPeriode)
                .ToListAsync();

            var periodeRefSheet = workbook.Worksheets.Add("Referensi Periode");
            periodeRefSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            periodeRefSheet.Cell(1, 2).Value = "Tanggal Awal";
            periodeRefSheet.Cell(1, 3).Value = "Tanggal Akhir";
            periodeRefSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < periodes.Count; i++)
            {
                periodeRefSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                periodeRefSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                periodeRefSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }
            periodeRefSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            var resultStream = new MemoryStream(bytes);

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_PerjalananDinas.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        // =========================================================
        // HELPERS (private, khusus import Perjalanan Dinas)
        // =========================================================

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

        private static string GetImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var noPekerja = GetImportCellText(sheet, row, headerMap, "NoPekerja");
            var periode = GetImportCellText(sheet, row, headerMap, "Periode");
            return string.IsNullOrWhiteSpace(noPekerja) && string.IsNullOrWhiteSpace(periode);
        }

        private static void ValidateRequiredField(
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

        private static readonly string[] IndonesianMonths =
{
            "januari", "februari", "maret", "april", "mei", "juni",
            "juli", "agustus", "september", "oktober", "november", "desember"
        };

        /// <summary>
        /// Parse "NamaBulan, TAHUN" (contoh "Januari, 2024") -> DateTime first day bulan.
        /// Mendukung format "Januari 2024", "Januari,2024", "JANUARI 2024" (case-insensitive).
        /// Tidak pakai Split/IndexOf/Parse - manual seperti helper lainnya.
        /// </summary>
        private static DateTime? ParseBulanTahun(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var cleaned = text.Trim();

            // Ekstrak nama bulan (huruf saja, lowercase) dan ambil 4 digit terakhir = tahun
            var monthLetters = string.Empty;
            var count = 0;
            var d1 = 0;
            var d2 = 0;
            var d3 = 0;
            var d4 = 0;

            for (var i = 0; i < cleaned.Length; i++)
            {
                var c = cleaned[i];

                if (c >= '0' && c <= '9')
                {
                    d1 = d2;
                    d2 = d3;
                    d3 = d4;
                    d4 = DigitValue(c);
                    if (count < 4)
                        count++;
                }
                else if (char.IsLetter(c))
                {
                    monthLetters += char.ToLower(c);
                }
                // koma / spasi / simbol lain -> diabaikan
            }

            if (count < 4 || monthLetters.Length == 0)
                return null;

            var year = d1 * 1000 + d2 * 100 + d3 * 10 + d4;

            var month = 0;
            for (var i = 0; i < IndonesianMonths.Length; i++)
            {
                if (monthLetters == IndonesianMonths[i])
                {
                    month = i + 1;
                    break;
                }
            }

            if (month < 1 || year < 1900 || year > 9999)
                return null;

            return new DateTime(year, month, 1);
        }

        /// <summary>
        /// Parse angka murni ("1500000", "1.500.000", "1,500,000.50"): abaikan karakter
        /// non-digit; '.' atau ',' pertama dianggap pemisah desimal.
        /// </summary>
        private static decimal? ParseImportDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            long integerPart = 0;
            long fractionPart = 0;
            var fractionDigits = -1;
            var anyDigit = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c >= '0' && c <= '9')
                {
                    if (fractionDigits >= 0)
                    {
                        fractionPart = fractionPart * 10 + (c - '0');
                        fractionDigits++;
                    }
                    else
                    {
                        integerPart = integerPart * 10 + (c - '0');
                    }
                    anyDigit = true;
                }
                else if (c == '.' || c == ',')
                {
                    if (fractionDigits >= 0)
                        break;
                    fractionDigits = 0;
                }
            }

            if (!anyDigit)
                return null;

            var value = (decimal)integerPart;
            if (fractionDigits > 0)
            {
                var divisor = 1m;
                for (var i = 0; i < fractionDigits; i++)
                    divisor *= 10m;

                value += (decimal)fractionPart / divisor;
            }

            return value;
        }

        private static int DigitValue(char c)
        {
            if (c == '0') return 0;
            if (c == '1') return 1;
            if (c == '2') return 2;
            if (c == '3') return 3;
            if (c == '4') return 4;
            if (c == '5') return 5;
            if (c == '6') return 6;
            if (c == '7') return 7;
            if (c == '8') return 8;
            return 9;
        }

        private sealed class PerjalananDinasImportContext
        {
            public PerjalananDinasImportRow Row { get; set; } = null!;
            public Pekerja Pekerja { get; set; } = null!;
            public Periode Periode { get; set; } = null!;
        }


        private static PerjalananDinasDto MapToDto(PerjalananDinas p) => new()
        {
            Id = p.Id,
            PekerjaId = p.PekerjaId,
            NoPekerja = p.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = p.Pekerja?.NamaPekerja ?? string.Empty,
            NopekHome = p.Pekerja?.NopekHome ?? string.Empty,
            NopekHost = p.Pekerja?.NopekHost ?? string.Empty,
            JabatanId = p.Pekerja?.JabatanId ?? string.Empty,
            JabatanName = p.Pekerja?.Jabatan?.Name ?? string.Empty,
            BulanTahun = p.BulanTahun,
            PeriodeId = p.PeriodeId,
            NamaPeriode = p.Periode?.NamaPeriode ?? string.Empty,
            TotalBiayaDinas = p.TotalBiayaDinas,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            ModifiedAt = p.ModifiedAt
        };
    }
}