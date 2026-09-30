using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;

namespace ApiService.Application.Services
{

    public interface IOperasionalTagihanKwhUmumBulkUploadService
    {
        Task<ApiResponse<TagihanKwhUmumBulkUploadResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class OperasionalTagihanKwhUmumBulkUploadService(IServiceDbContext context) : IOperasionalTagihanKwhUmumBulkUploadService
    {
        private readonly IServiceDbContext _context = context;
        private const int HeaderRow = 1;
        private const int DataStartRow = 2;
        private static readonly string[] RequiredHeaders =
        {
            "periode", "tanggalpenagihan", "jumlahbiaya", "jumlahkwh"
        };

        public async Task<ApiResponse<TagihanKwhUmumBulkUploadResponse>> ImportFromExcelAsync(
            Stream fileStream,
            string userId)
        {
            var response = new TagihanKwhUmumBulkUploadResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<TagihanKwhUmumBulkUploadResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<TagihanKwhUmumBulkUploadResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<TagihanKwhUmumBulkUploadResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildHeaderMap(sheet, HeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = RequiredHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<TagihanKwhUmumBulkUploadResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<TagihanKwhUmumBulkUploadRowDto>();

                for (var row = DataStartRow; row <= lastRow; row++)
                {
                    if (IsRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsed = new TagihanKwhUmumBulkUploadRowDto
                    {
                        RowNumber = row,
                        PeriodeName = GetCellText(sheet, row, headerMap, "Periode"),
                        TanggalPenagihanText = GetCellText(sheet, row, headerMap, "TanggalPenagihan"),
                        JumlahBiayaText = GetCellText(sheet, row, headerMap, "JumlahBiaya"),
                        JumlahKwhText = GetCellText(sheet, row, headerMap, "JumlahKwh")
                    };

                    ValidateRequired(response.Errors, row, "Periode", parsed.PeriodeName);
                    ValidateRequired(response.Errors, row, "TanggalPenagihan", parsed.TanggalPenagihanText);
                    ValidateRequired(response.Errors, row, "JumlahBiaya", parsed.JumlahBiayaText);
                    ValidateRequired(response.Errors, row, "JumlahKwh", parsed.JumlahKwhText);

                    parsed.TanggalPenagihan = ParseImportDate(parsed.TanggalPenagihanText);
                    if (parsed.TanggalPenagihan == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "TanggalPenagihan",
                            Message = "TanggalPenagihan tidak valid. Format: yyyy-MM-dd."
                        });
                    }

                    parsed.JumlahBiaya = ParseImportDecimal(parsed.JumlahBiayaText);
                    if (parsed.JumlahBiaya == null || parsed.JumlahBiaya <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "JumlahBiaya",
                            Message = "JumlahBiaya tidak valid. Isi angka murni, contoh: 999000000."
                        });
                    }

                    parsed.JumlahKwh = ParseImportDecimal(parsed.JumlahKwhText);
                    if (parsed.JumlahKwh == null || parsed.JumlahKwh <= 0)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "JumlahKwh",
                            Message = "JumlahKwh tidak valid. Isi angka murni, contoh: 850."
                        });
                    }

                    parsedRows.Add(parsed);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<TagihanKwhUmumBulkUploadResponse>.BadRequest(
                        "Tidak ada data Tagihan KWh Umum pada file Excel.");

                var duplicatePeriods = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.PeriodeName))
                    .GroupBy(x => x.PeriodeName.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .ToList();

                foreach (var row in duplicatePeriods)
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Periode",
                        Message = $"Periode '{row.PeriodeName}' duplikat di dalam file Excel."
                    });
                }

                var periodes = await _context.Periodes
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();

                var existingPeriodeIds = await _context.TagihanKwhs
                    .Where(x => !x.IsDeleted && x.Kategori == TagihanKwh.KategoriUmum)
                    .Select(x => x.PeriodeId)
                    .ToListAsync();

                var existingPeriodeSet = existingPeriodeIds.ToHashSet();
                var contextRows = new List<(TagihanKwhUmumBulkUploadRowDto Row, Periode Periode)>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.PeriodeName))
                        continue;

                    var periode = periodes.FirstOrDefault(p =>
                        string.Equals(
                            p.NamaPeriode?.Trim(),
                            row.PeriodeName.Trim(),
                            StringComparison.OrdinalIgnoreCase));

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

                    if (existingPeriodeSet.Contains(periode.Id))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Periode",
                            Message = $"Tagihan KWh Umum untuk periode '{row.PeriodeName}' sudah ada."
                        });
                        continue;
                    }

                    contextRows.Add((row, periode));
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedTagihanKwh = 0;

                    return ApiResponse<TagihanKwhUmumBulkUploadResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Tagihan KWh Umum yang disimpan.");
                }

                var now = DateTime.UtcNow;
                var newTagihans = new List<TagihanKwh>();

                foreach (var (row, periode) in contextRows)
                {
                    var tagihan = new TagihanKwh
                    {
                        PeriodeId = periode.Id,
                        TanggalPenagihan = row.TanggalPenagihan!.Value,
                        JumlahBiaya = row.JumlahBiaya!.Value,
                        JumlahKwh = row.JumlahKwh!.Value,
                        Kategori = TagihanKwh.KategoriUmum,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newTagihans.Add(tagihan);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new TagihanKwhUmumBulkUploadPreviewDto
                        {
                            NamaPeriode = periode.NamaPeriode ?? string.Empty,
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

                return ApiResponse<TagihanKwhUmumBulkUploadResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedTagihanKwh} Tagihan KWh Umum berhasil diimport.");
            }
            catch (Exception ex)
            {
                return ApiResponse<TagihanKwhUmumBulkUploadResponse>.ErrorResponse(
                    "ERR-TAGIHANKWH-UMUM-IMPORT-001",
                    $"Gagal mengimport data Tagihan KWh Umum: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("TagihanKwhUmum");

            var headers = new[] { "Periode", "TanggalPenagihan", "JumlahBiaya", "JumlahKwh" };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(HeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(HeaderRow, 1, HeaderRow, headers.Length);
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

            var periodes = await _context.Periodes
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.NamaPeriode)
                .ToListAsync();

            var periodeSheet = workbook.Worksheets.Add("Referensi Periode");
            periodeSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            periodeSheet.Cell(1, 2).Value = "Tanggal Awal";
            periodeSheet.Cell(1, 3).Value = "Tanggal Akhir";
            periodeSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            for (var i = 0; i < periodes.Count; i++)
            {
                periodeSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                periodeSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                periodeSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }

            periodeSheet.Columns().AdjustToContents();

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                workbook.SaveAs(ms);
                bytes = ms.ToArray();
            }

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "Template_Upload_TagihanKwh_Umum.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = new MemoryStream(bytes)
            });
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header))
                    continue;

                var normalized = NormalizeHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetCellText(
            IXLWorksheet sheet,
            int row,
            Dictionary<string, int> headerMap,
            string header)
        {
            var normalized = NormalizeHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsRowEmpty(
            IXLWorksheet sheet,
            int row,
            Dictionary<string, int> headerMap)
        {
            return string.IsNullOrWhiteSpace(GetCellText(sheet, row, headerMap, "Periode"));
        }

        private static void ValidateRequired(
            List<ImportRowError> errors,
            int row,
            string column,
            string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return;

            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = column,
                Message = $"Data wajib '{column}' kosong."
            });
        }

        private static DateTime? ParseImportDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 10)
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

            var year = DigitValue(cleaned[0]) * 1000 + DigitValue(cleaned[1]) * 100 +
                       DigitValue(cleaned[2]) * 10 + DigitValue(cleaned[3]);
            var month = DigitValue(cleaned[5]) * 10 + DigitValue(cleaned[6]);
            var day = DigitValue(cleaned[8]) * 10 + DigitValue(cleaned[9]);

            if (year < 1900 || year > 9999 || month < 1 || month > 12 || day < 1 || day > 31)
                return null;

            try
            {
                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
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

        private static int DigitValue(char c) => c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            _ => 9
        };
    }
}
