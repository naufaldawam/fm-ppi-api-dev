using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{
    public interface ITagihanKwhService
    {
        /// <summary>kategori = TagihanKwh.KategoriP8 ("p8") atau nanti KategoriUmum.</summary>
        Task<ApiResponse<PagedResponse<TagihanKwhDto>>> GetAllAsync(TagihanKwhFilterRequest filter, string kategori);
        Task<ApiResponse<TagihanKwhDto>> GetByIdAsync(string id, string kategori);
        Task<ApiResponse<TagihanKwhDto>> CreateAsync(CreateTagihanKwhRequest request, string kategori, string userId);
        Task<ApiResponse<TagihanKwhDto>> UpdateAsync(string id, UpdateTagihanKwhRequest request, string kategori, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string kategori, string userId);
        Task<ApiResponse<TagihanKwhSummaryDto>> GetSummaryAsync(TagihanKwhSummaryRequest filter, string kategori);
        Task<ApiResponse<TagihanKwhImportResponse>> ImportFromExcelAsync(Stream fileStream, string kategori, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class TagihanKwhService : ITagihanKwhService
    {
        private readonly IServiceDbContext _context;

        public TagihanKwhService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<TagihanKwh> BaseQuery() =>
            _context.TagihanKwhs
                .Include(t => t.Periode)
                .Where(t => !t.IsDeleted);

        public async Task<ApiResponse<PagedResponse<TagihanKwhDto>>> GetAllAsync(
            TagihanKwhFilterRequest filter, string kategori)
        {
            var query = BaseQuery().Where(t => t.Kategori == kategori);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(t => t.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<TagihanKwhDto>>.SuccessResponse(new PagedResponse<TagihanKwhDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<TagihanKwhDto>> GetByIdAsync(string id, string kategori)
        {
            var tagihan = await BaseQuery()
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori);

            if (tagihan == null)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(tagihan));
        }

        public async Task<ApiResponse<TagihanKwhDto>> CreateAsync(
            CreateTagihanKwhRequest request, string kategori, string userId)
        {
            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-007", "Periode tidak ditemukan");

            var tagihan = new TagihanKwh
            {
                PeriodeId = request.PeriodeId,
                TanggalPenagihan = request.TanggalPenagihan,
                JumlahBiaya = request.JumlahBiaya,
                JumlahKwh = request.JumlahKwh,
                // Kategori diisi server-side per endpoint (sekarang "p8")
                Kategori = kategori,
                IsActive = true,
                CreatedBy = userId
            };

            _context.TagihanKwhs.Add(tagihan);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(t => t.Id == tagihan.Id);
            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(created), "Tagihan KWh berhasil ditambahkan");
        }

        public async Task<ApiResponse<TagihanKwhDto>> UpdateAsync(
            string id, UpdateTagihanKwhRequest request, string kategori, string userId)
        {
            var tagihan = await _context.TagihanKwhs
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori && !t.IsDeleted);

            if (tagihan == null)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<TagihanKwhDto>.ErrorResponse("ERR-TAGIHANKWH-007", "Periode tidak ditemukan");

            tagihan.PeriodeId = request.PeriodeId;
            tagihan.TanggalPenagihan = request.TanggalPenagihan;
            tagihan.JumlahBiaya = request.JumlahBiaya;
            tagihan.JumlahKwh = request.JumlahKwh;
            tagihan.Kategori = kategori;
            tagihan.IsActive = request.IsActive;
            tagihan.ModifiedAt = DateTime.UtcNow;
            tagihan.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(t => t.Id == tagihan.Id);
            return ApiResponse<TagihanKwhDto>.SuccessResponse(MapToDto(updated), "Tagihan KWh berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string kategori, string userId)
        {
            var tagihan = await _context.TagihanKwhs
                .FirstOrDefaultAsync(t => t.Id == id && t.Kategori == kategori && !t.IsDeleted);

            if (tagihan == null)
                return ApiResponse<bool>.ErrorResponse("ERR-TAGIHANKWH-001", "Tagihan KWh not found");

            tagihan.IsDeleted = true;
            tagihan.DeletedAt = DateTime.UtcNow;
            tagihan.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Tagihan KWh deleted");
        }

        public async Task<ApiResponse<TagihanKwhSummaryDto>> GetSummaryAsync(
            TagihanKwhSummaryRequest filter, string kategori)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<TagihanKwhSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            var query = _context.TagihanKwhs.Where(t => !t.IsDeleted && t.Kategori == kategori);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(t => t.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(t => t.IsActive == filter.IsActive.Value);

            var totalTagihan = await query.CountAsync();
            var grandTotalBiaya = totalTagihan == 0 ? 0m : await query.SumAsync(t => t.JumlahBiaya);
            var grandTotalKwh = totalTagihan == 0 ? 0m : await query.SumAsync(t => t.JumlahKwh);

            return ApiResponse<TagihanKwhSummaryDto>.SuccessResponse(new TagihanKwhSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalTagihan = totalTagihan,
                GrandTotalBiaya = grandTotalBiaya,
                GrandTotalKwh = grandTotalKwh
            });
        }

        private const int TagihanKwhImportHeaderRow = 1;
        private const int TagihanKwhImportDataStartRow = 2;

        private static readonly string[] TagihanKwhRequiredImportHeaders =
        {
            "periode", "tanggalpenagihan", "jumlahbiaya", "jumlahkwh"
        };

        public async Task<ApiResponse<TagihanKwhImportResponse>> ImportFromExcelAsync(
            Stream fileStream, string kategori, string userId)
        {
            var response = new TagihanKwhImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<TagihanKwhImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<TagihanKwhImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<TagihanKwhImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildTagihanKwhImportHeaderMap(sheet, TagihanKwhImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = TagihanKwhRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<TagihanKwhImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<TagihanKwhImportRow>();

                for (var row = TagihanKwhImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsTagihanKwhImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new TagihanKwhImportRow
                    {
                        RowNumber = row,
                        PeriodeName = GetTagihanKwhImportCellText(sheet, row, headerMap, "Periode"),
                        TanggalPenagihanText = GetTagihanKwhImportCellText(sheet, row, headerMap, "TanggalPenagihan"),
                        JumlahBiayaText = GetTagihanKwhImportCellText(sheet, row, headerMap, "JumlahBiaya"),
                        JumlahKwhText = GetTagihanKwhImportCellText(sheet, row, headerMap, "JumlahKwh")
                    };

                    ValidateTagihanKwhRequiredField(response.Errors, row, "Periode", parsedRow.PeriodeName);
                    ValidateTagihanKwhRequiredField(response.Errors, row, "TanggalPenagihan", parsedRow.TanggalPenagihanText);
                    ValidateTagihanKwhRequiredField(response.Errors, row, "JumlahBiaya", parsedRow.JumlahBiayaText);
                    ValidateTagihanKwhRequiredField(response.Errors, row, "JumlahKwh", parsedRow.JumlahKwhText);

                    parsedRow.TanggalPenagihan = ParseImportDate(parsedRow.TanggalPenagihanText);
                    if (parsedRow.TanggalPenagihan == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "TanggalPenagihan",
                            Message = "TanggalPenagihan tidak valid. Format: yyyy-MM-dd."
                        });
                    }

                    parsedRow.JumlahBiaya = ParseImportDecimal(parsedRow.JumlahBiayaText);
                    if (parsedRow.JumlahBiaya == null || parsedRow.JumlahBiaya <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "JumlahBiaya",
                            Message = "JumlahBiaya tidak valid. Isi angka murni, contoh: 999000000."
                        });
                    }

                    parsedRow.JumlahKwh = ParseImportDecimal(parsedRow.JumlahKwhText);
                    if (parsedRow.JumlahKwh == null || parsedRow.JumlahKwh <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "JumlahKwh",
                            Message = "JumlahKwh tidak valid. Isi angka murni, contoh: 850."
                        });
                    }

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<TagihanKwhImportResponse>.BadRequest("Tidak ada data Tagihan KWh pada file Excel.");

                var periodeInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.PeriodeName))
                    .GroupBy(x => x.PeriodeName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.PeriodeName) &&
                             periodeInFile.Contains(x.PeriodeName.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Periode",
                        Message = $"Periode '{row.PeriodeName}' duplikat di dalam file Excel."
                    });
                }

                var periodes = await _context.Periodes.Where(x => !x.IsDeleted).ToListAsync();
                var existingTagihans = await _context.TagihanKwhs
                    .Where(x => !x.IsDeleted && x.Kategori == kategori)
                    .ToListAsync();

                var existingPeriodeIds = existingTagihans
                    .Select(t => t.PeriodeId)
                    .ToHashSet();

                var contextRows = new List<TagihanKwhImportContext>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.PeriodeName))
                        continue;

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

                    contextRows.Add(new TagihanKwhImportContext
                    {
                        Row = row,
                        Periode = periode
                    });
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedTagihanKwh = 0;

                    return ApiResponse<TagihanKwhImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Tagihan KWh yang disimpan.");
                }

                var now = DateTime.UtcNow;
                var newTagihans = new List<TagihanKwh>();

                foreach (var ctx in contextRows)
                {
                    var tagihan = new TagihanKwh
                    {
                        PeriodeId = ctx.Periode.Id,
                        TanggalPenagihan = (DateTime)ctx.Row.TanggalPenagihan!,
                        JumlahBiaya = (decimal)ctx.Row.JumlahBiaya!,
                        JumlahKwh = (decimal)ctx.Row.JumlahKwh!,
                        Kategori = kategori,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newTagihans.Add(tagihan);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new TagihanKwhImportPreviewDto
                        {
                            NamaPeriode = ctx.Periode.NamaPeriode,
                            TanggalPenagihan = tagihan.TanggalPenagihan,
                            JumlahBiaya = tagihan.JumlahBiaya,
                            JumlahKwh = tagihan.JumlahKwh
                        });
                    }
                }

                await _context.TagihanKwhs.AddRangeAsync(newTagihans);
                await _context.SaveChangesAsync();

                response.InsertedTagihanKwh = newTagihans.Count;
                response.SuccessCount = newTagihans.Count;
                response.ErrorCount = 0;

                return ApiResponse<TagihanKwhImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedTagihanKwh} Tagihan KWh berhasil diimport (kategori '{kategori}').");
            }
            catch (Exception ex)
            {
                return ApiResponse<TagihanKwhImportResponse>.ErrorResponse(
                    "ERR-TAGIHANKWH-IMPORT-001", $"Gagal mengimport data Tagihan KWh: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("TagihanKwh");

            var headers = new[] { "Periode", "TanggalPenagihan", "JumlahBiaya", "JumlahKwh" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(TagihanKwhImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(TagihanKwhImportHeaderRow, 1, TagihanKwhImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            sheet.Cell(2, 1).Value = "Januari 2026";
            sheet.Cell(2, 2).Value = "2026-09-11";
            sheet.Cell(2, 3).Value = "999000000";
            sheet.Cell(2, 4).Value = "850";
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Columns().AdjustToContents();

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
                FileName = "Template_Upload_TagihanKwh_P8.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        private static Dictionary<string, int> BuildTagihanKwhImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeTagihanKwhImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeTagihanKwhImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetTagihanKwhImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeTagihanKwhImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsTagihanKwhImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var periode = GetTagihanKwhImportCellText(sheet, row, headerMap, "Periode");
            return string.IsNullOrWhiteSpace(periode);
        }

        private static void ValidateTagihanKwhRequiredField(
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

        private static DateTime? ParseImportDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var cleaned = text.Trim();
            if (cleaned.Length < 10)
                return null;

            for (var i = 0; i < 10; i++)
            {
                var c = cleaned[i];
                if (i == 4 || i == 7)
                {
                    if (c != '-' && c != '/' && c != '.')
                        return null;
                }
                else if (c < '0' || c > '9')
                {
                    return null;
                }
            }

            var year = DigitValue(cleaned[0]) * 1000 + DigitValue(cleaned[1]) * 100 + DigitValue(cleaned[2]) * 10 + DigitValue(cleaned[3]);
            var month = DigitValue(cleaned[5]) * 10 + DigitValue(cleaned[6]);
            var day = DigitValue(cleaned[8]) * 10 + DigitValue(cleaned[9]);

            if (year < 1900 || year > 9999 || month < 1 || month > 12 || day < 1 || day > 31)
                return null;

            return new DateTime(year, month, day);
        }

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
                // selain angka/sep dibuang (Rp, spasi, dll)
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

        private sealed class TagihanKwhImportContext
        {
            public TagihanKwhImportRow Row { get; set; } = null!;
            public Periode Periode { get; set; } = null!;
        }

        private static TagihanKwhDto MapToDto(TagihanKwh t) => new()
        {
            Id = t.Id,
            PeriodeId = t.PeriodeId,
            NamaPeriode = t.Periode?.NamaPeriode ?? string.Empty,
            TanggalPenagihan = t.TanggalPenagihan,
            JumlahBiaya = t.JumlahBiaya,
            JumlahKwh = t.JumlahKwh,
            Kategori = t.Kategori,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt
        };
    }
}