using System;
using System.Collections.Generic;
using System.Globalization;
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
    public interface IOperasionalTagihanBbmBulkUploadService
    {
        Task<ApiResponse<OperasionalTagihanBbmImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
    }

    public class OperasionalTagihanBbmBulkUploadService(IServiceDbContext context) : IOperasionalTagihanBbmBulkUploadService
    {
        private readonly IServiceDbContext _context = context;
        private const int HeaderRow = 1;
        private const int DataStartRow = 2;
        private static readonly string[] RequiredHeaders =
        {
            "nopekerja",
            "periode",
            "tanggalpenggunaan",
            "nomorpolisi",
            "jumlahpenggunaanbbm",
            "nilaiodometer",
            "nilainota"
        };

        public async Task<ApiResponse<OperasionalTagihanBbmImportResponse>> ImportFromExcelAsync(
            Stream fileStream,
            string userId)
        {
            var response = new OperasionalTagihanBbmImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Worksheet tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Worksheet kosong.");

                var headerMap = BuildHeaderMap(sheet, HeaderRow);
                var missingHeaders = RequiredHeaders.Where(h => !headerMap.ContainsKey(h)).ToList();

                if (missingHeaders.Count > 0)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest(
                        $"Header template tidak lengkap. Kolom hilang: {string.Join(", ", missingHeaders)}.");

                var lastRow = usedRange.LastRow().RowNumber();
                var parsedRows = new List<OperasionalTagihanBbmImportRow>();

                for (var row = DataStartRow; row <= lastRow; row++)
                {
                    if (IsRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsed = new OperasionalTagihanBbmImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetText(sheet, row, headerMap, "NoPekerja"),
                        PeriodeName = GetText(sheet, row, headerMap, "Periode"),
                        TanggalPenggunaan = ParseRequiredDateTime(
                            sheet, row, headerMap, "TanggalPenggunaan", response.Errors),
                        NomorPolisi = GetText(sheet, row, headerMap, "NomorPolisi"),
                        JumlahPenggunaanBbm = ParseRequiredDecimal(
                            sheet, row, headerMap, "JumlahPenggunaanBbm", response.Errors),
                        NilaiOdometer = ParseRequiredDecimal(
                            sheet, row, headerMap, "NilaiOdometer", response.Errors),
                        NilaiNota = ParseRequiredDecimal(
                            sheet, row, headerMap, "NilaiNota", response.Errors),
                        CatatanTambahan = GetNullableText(sheet, row, headerMap, "CatatanTambahan"),
                        Status = GetTextOrDefault(sheet, row, headerMap, "Status", BbmSubmission.StatusPending),
                        FotoOdometerUrl = GetNullableText(sheet, row, headerMap, "foto_odometer_url"),
                        FotoNotaUrl = GetNullableText(sheet, row, headerMap, "foto_nota_url"),
                        RejectedReason = GetNullableText(sheet, row, headerMap, "RejectedReason")
                    };

                    ValidateRequired(response.Errors, row, "NoPekerja", parsed.NoPekerja);
                    ValidateRequired(response.Errors, row, "Periode", parsed.PeriodeName);
                    ValidateRequired(response.Errors, row, "NomorPolisi", parsed.NomorPolisi);

                    if (parsed.TanggalPenggunaan == default)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "TanggalPenggunaan",
                            Message = "Kolom 'TanggalPenggunaan' wajib diisi dengan tanggal yang valid."
                        });
                    }

                    ValidateNonNegative(response.Errors, row, "JumlahPenggunaanBbm", parsed.JumlahPenggunaanBbm);
                    ValidateNonNegative(response.Errors, row, "NilaiOdometer", parsed.NilaiOdometer);
                    ValidateNonNegative(response.Errors, row, "NilaiNota", parsed.NilaiNota);
                    ValidateStatus(response.Errors, row, parsed.Status);

                    if (string.Equals(parsed.Status, BbmSubmission.StatusRejected, StringComparison.OrdinalIgnoreCase) &&
                        string.IsNullOrWhiteSpace(parsed.RejectedReason))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "RejectedReason",
                            Message = "RejectedReason wajib diisi jika Status = Rejected."
                        });
                    }

                    parsedRows.Add(parsed);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.BadRequest("Tidak ada data pada file.");

                var duplicateRows = parsedRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.NoPekerja) &&
                                !string.IsNullOrWhiteSpace(r.PeriodeName) &&
                                !string.IsNullOrWhiteSpace(r.NomorPolisi) &&
                                r.TanggalPenggunaan != default)
                    .GroupBy(r => BuildBusinessKey(r.NoPekerja, r.PeriodeName, r.NomorPolisi, r.TanggalPenggunaan))
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g)
                    .ToList();

                foreach (var duplicate in duplicateRows)
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = duplicate.RowNumber,
                        Column = "NoPekerja",
                        Message = $"Kombinasi NoPekerja '{duplicate.NoPekerja}', Periode '{duplicate.PeriodeName}', NomorPolisi '{duplicate.NomorPolisi}', TanggalPenggunaan '{duplicate.TanggalPenggunaan:yyyy-MM-dd}' duplikat dalam file."
                    });
                }

                var drivers = await _context.Drivers
                    .Include(d => d.Atasan)
                    .Where(d => !d.IsDeleted && d.IsActive)
                    .ToListAsync();

                var periodes = await _context.Periodes
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                var kendaraans = await _context.Kendaraans
                    .Include(k => k.BahanBakar)
                    .Where(k => !k.IsDeleted && k.IsActive)
                    .ToListAsync();

                var existing = await _context.BbmSubmissions
                    .Where(b => !b.IsDeleted && b.Status != BbmSubmission.StatusRejected)
                    .Select(b => new
                    {
                        b.DriverId,
                        b.PeriodeId,
                        b.KendaraanId,
                        b.TanggalPenggunaan
                    })
                    .ToListAsync();

                var existingKeys = existing
                    .Select(x => BuildBusinessKey(x.DriverId, x.PeriodeId, x.KendaraanId, x.TanggalPenggunaan))
                    .ToHashSet();

                var validRows = new List<(OperasionalTagihanBbmImportRow Row, Driver Driver, Periode Periode, Kendaraan Kendaraan)>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja) ||
                        string.IsNullOrWhiteSpace(row.PeriodeName) ||
                        string.IsNullOrWhiteSpace(row.NomorPolisi) ||
                        row.TanggalPenggunaan == default)
                        continue;

                    var driver = drivers.FirstOrDefault(d =>
                        string.Equals(d.NoPekerja?.Trim(), row.NoPekerja.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (driver == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"NoPekerja '{row.NoPekerja}' tidak ditemukan di master Driver atau tidak aktif."
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
                            Message = $"Periode '{row.PeriodeName}' tidak ditemukan di master Periode."
                        });
                        continue;
                    }

                    var kendaraan = kendaraans.FirstOrDefault(k =>
                        string.Equals(k.NomorPolisi?.Trim(), row.NomorPolisi.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (kendaraan == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NomorPolisi",
                            Message = $"Nomor Polisi '{row.NomorPolisi}' tidak ditemukan di master Kendaraan atau tidak aktif."
                        });
                        continue;
                    }

                    var key = BuildBusinessKey(driver.Id, periode.Id, kendaraan.Id, row.TanggalPenggunaan);
                    if (existingKeys.Contains(key))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"Pengajuan BBM untuk driver '{row.NoPekerja}', kendaraan '{row.NomorPolisi}', tanggal '{row.TanggalPenggunaan:yyyy-MM-dd}' pada periode '{row.PeriodeName}' sudah ada."
                        });
                        continue;
                    }

                    validRows.Add((row, driver, periode, kendaraan));
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    return ApiResponse<OperasionalTagihanBbmImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data yang disimpan.");
                }

                var now = DateTime.UtcNow;
                var entities = new List<BbmSubmission>();

                foreach (var item in validRows)
                {
                    var status = NormalizeStatus(item.Row.Status);
                    var entity = new BbmSubmission
                    {
                        DriverId = item.Driver.Id,
                        AtasanPekerjaId = item.Driver.AtasanId,
                        PeriodeId = item.Periode.Id,
                        TanggalPenggunaan = item.Row.TanggalPenggunaan,
                        KendaraanId = item.Kendaraan.Id,
                        JumlahPenggunaanBbm = item.Row.JumlahPenggunaanBbm,
                        NilaiOdometer = item.Row.NilaiOdometer,
                        NilaiNota = item.Row.NilaiNota,
                        FotoOdometerUrl = item.Row.FotoOdometerUrl ?? string.Empty,
                        FotoNotaUrl = item.Row.FotoNotaUrl ?? string.Empty,
                        CatatanTambahan = item.Row.CatatanTambahan,
                        Status = status,
                        RejectedReason = status == BbmSubmission.StatusRejected ? item.Row.RejectedReason?.Trim() : null,
                        ApprovedBy = status is BbmSubmission.StatusApproved or BbmSubmission.StatusRejected ? userId : null,
                        ApprovedAt = status is BbmSubmission.StatusApproved or BbmSubmission.StatusRejected ? now : null,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    entities.Add(entity);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new OperasionalTagihanBbmImportPreviewDto
                        {
                            NoPekerja = item.Driver.NoPekerja,
                            NamaDriver = item.Driver.NamaDriver,
                            NamaVP = item.Driver.Atasan?.NamaPekerja ?? string.Empty,
                            NamaPeriode = item.Periode.NamaPeriode,
                            TanggalPenggunaan = item.Row.TanggalPenggunaan,
                            NomorPolisi = item.Kendaraan.NomorPolisi,
                            JenisBbmName = item.Kendaraan.BahanBakar?.Name ?? string.Empty,
                            JumlahPenggunaanBbm = item.Row.JumlahPenggunaanBbm,
                            NilaiOdometer = item.Row.NilaiOdometer,
                            NilaiNota = item.Row.NilaiNota,
                            Status = status
                        });
                    }
                }

                await _context.BbmSubmissions.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                response.InsertedBbmSubmission = entities.Count;
                response.SuccessCount = entities.Count;
                response.ErrorCount = 0;

                return ApiResponse<OperasionalTagihanBbmImportResponse>.SuccessResponse(
                    response,
                    $"{entities.Count} data Tagihan BBM berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<OperasionalTagihanBbmImportResponse>.ErrorResponse(
                    "ERR-BBM-IMPORT-001",
                    $"Gagal menyimpan data: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<OperasionalTagihanBbmImportResponse>.ErrorResponse(
                    "ERR-BBM-IMPORT-002",
                    $"Gagal mengimport: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("TagihanBbm");

            var headers = new[]
            {
                "NoPekerja",
                "Periode",
                "TanggalPenggunaan",
                "NomorPolisi",
                "JumlahPenggunaanBbm",
                "NilaiOdometer",
                "NilaiNota",
                "CatatanTambahan",
                "Status",
                "foto_odometer_url",
                "foto_nota_url",
                "RejectedReason"
            };

            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(HeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(HeaderRow, 1, HeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            var exRow = new object[]
            {
                "DRV001",
                "Januari 2026",
                "2026-01-15",
                "B 1234 XYZ",
                10.5,
                12500,
                150000,
                "Contoh import",
                BbmSubmission.StatusPending,
                "",
                "",
                ""
            };

            for (var i = 0; i < exRow.Length; i++)
                sheet.Cell(DataStartRow, i + 1).Value = XLCellValue.FromObject(exRow[i]);

            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.Italic = true;
            sheet.Range(DataStartRow, 1, DataStartRow, headers.Length).Style.Font.FontColor = XLColor.Gray;

            sheet.Column(3).Style.NumberFormat.Format = "yyyy-mm-dd";
            for (var col = 5; col <= 7; col++)
                sheet.Column(col).Style.NumberFormat.Format = "#,##0.00";

            sheet.Columns().AdjustToContents();

            var driverSheet = workbook.Worksheets.Add("Referensi Driver");
            driverSheet.Cell(1, 1).Value = "NoPekerja (Valid)";
            driverSheet.Cell(1, 2).Value = "Nama Driver";
            driverSheet.Cell(1, 3).Value = "Nama Atasan";
            driverSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            var drivers = await _context.Drivers
                .Include(d => d.Atasan)
                .Where(d => !d.IsDeleted && d.IsActive)
                .OrderBy(d => d.NamaDriver)
                .ToListAsync();

            for (var i = 0; i < drivers.Count; i++)
            {
                driverSheet.Cell(i + 2, 1).Value = drivers[i].NoPekerja;
                driverSheet.Cell(i + 2, 2).Value = drivers[i].NamaDriver;
                driverSheet.Cell(i + 2, 3).Value = drivers[i].Atasan?.NamaPekerja ?? string.Empty;
            }
            driverSheet.Columns().AdjustToContents();

            var kendaraanSheet = workbook.Worksheets.Add("Referensi Kendaraan");
            kendaraanSheet.Cell(1, 1).Value = "NomorPolisi (Valid)";
            kendaraanSheet.Cell(1, 2).Value = "Jenis BBM";
            kendaraanSheet.Cell(1, 3).Value = "Merek";
            kendaraanSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            var kendaraans = await _context.Kendaraans
                .Include(k => k.BahanBakar)
                .Where(k => !k.IsDeleted && k.IsActive)
                .OrderBy(k => k.NomorPolisi)
                .ToListAsync();

            for (var i = 0; i < kendaraans.Count; i++)
            {
                kendaraanSheet.Cell(i + 2, 1).Value = kendaraans[i].NomorPolisi;
                kendaraanSheet.Cell(i + 2, 2).Value = kendaraans[i].BahanBakar?.Name ?? string.Empty;
                kendaraanSheet.Cell(i + 2, 3).Value = kendaraans[i].Merek;
            }
            kendaraanSheet.Columns().AdjustToContents();

            var periodeSheet = workbook.Worksheets.Add("Referensi Periode");
            periodeSheet.Cell(1, 1).Value = "NamaPeriode (Valid)";
            periodeSheet.Cell(1, 2).Value = "Tanggal Awal";
            periodeSheet.Cell(1, 3).Value = "Tanggal Akhir";
            periodeSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;

            var periodes = await _context.Periodes
                .Where(p => !p.IsDeleted && p.IsActive)
                .OrderBy(p => p.TanggalAwal)
                .ToListAsync();

            for (var i = 0; i < periodes.Count; i++)
            {
                periodeSheet.Cell(i + 2, 1).Value = periodes[i].NamaPeriode;
                periodeSheet.Cell(i + 2, 2).Value = periodes[i].TanggalAwal;
                periodeSheet.Cell(i + 2, 3).Value = periodes[i].TanggalAkhir;
            }
            periodeSheet.Columns().AdjustToContents();

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            var bytes = ms.ToArray();

            return ApiResponse<FileResult>.Ok(new FileResult
            {
                FileName = "TEMPLATE_BULK_UPLOAD_TAGIHAN_BBM.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = new MemoryStream(bytes)
            });
        }

        private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var col = 1; col <= lastColumn; col++)
            {
                var header = sheet.Cell(headerRow, col).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = Normalize(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, col);
            }

            return result;
        }

        private static string Normalize(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetText(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header)
        {
            if (!map.TryGetValue(Normalize(header), out var col)) return string.Empty;
            return sheet.Cell(row, col).GetString()?.Trim() ?? string.Empty;
        }

        private static string? GetNullableText(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header)
        {
            var value = GetText(sheet, row, map, header);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string GetTextOrDefault(IXLWorksheet sheet, int row, Dictionary<string, int> map, string header, string defaultValue)
        {
            var value = GetText(sheet, row, map, header);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        private static bool IsRowEmpty(IXLWorksheet sheet, int row, Dictionary<string, int> map)
        {
            return string.IsNullOrWhiteSpace(GetText(sheet, row, map, "NoPekerja")) &&
                   string.IsNullOrWhiteSpace(GetText(sheet, row, map, "Periode")) &&
                   string.IsNullOrWhiteSpace(GetText(sheet, row, map, "NomorPolisi"));
        }

        private static void ValidateRequired(List<ImportRowError> errors, int row, string column, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return;

            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = column,
                Message = $"Kolom '{column}' wajib diisi."
            });
        }

        private static decimal ParseRequiredDecimal(
            IXLWorksheet sheet,
            int row,
            Dictionary<string, int> map,
            string header,
            List<ImportRowError> errors)
        {
            if (!map.TryGetValue(Normalize(header), out var col))
                return 0m;

            var cell = sheet.Cell(row, col);

            if (cell.DataType == XLDataType.Number)
                return cell.GetValue<decimal>();

            var text = cell.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = header,
                    Message = $"Kolom '{header}' wajib diisi."
                });
                return 0m;
            }

            var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
            var lastComma = cleaned.LastIndexOf(',');
            var lastDot = cleaned.LastIndexOf('.');
            var normalized = lastComma > lastDot
                ? cleaned.Replace(".", "").Replace(",", ".")
                : cleaned.Replace(",", "");

            if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                return parsed;

            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = header,
                Message = $"'{header}' tidak valid. Isi angka yang valid."
            });

            return 0m;
        }

        private static DateTime ParseRequiredDateTime(
            IXLWorksheet sheet,
            int row,
            Dictionary<string, int> map,
            string header,
            List<ImportRowError> errors)
        {
            if (!map.TryGetValue(Normalize(header), out var col))
                return default;

            var cell = sheet.Cell(row, col);

            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            if (cell.DataType == XLDataType.Number &&
                double.TryParse(cell.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            {
                try { return DateTime.FromOADate(oa); } catch { }
            }

            var text = cell.GetString()?.Trim();
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;

            if (DateTime.TryParse(text, out parsed))
                return parsed;

            if (!string.IsNullOrWhiteSpace(text))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = row,
                    Column = header,
                    Message = $"'{header}' tidak valid. Gunakan format tanggal yyyy-MM-dd."
                });
            }

            return default;
        }

        private static void ValidateNonNegative(List<ImportRowError> errors, int row, string column, decimal value)
        {
            if (value >= 0) return;

            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = column,
                Message = $"'{column}' tidak boleh negatif."
            });
        }

        private static void ValidateStatus(List<ImportRowError> errors, int row, string value)
        {
            if (string.Equals(value, BbmSubmission.StatusPending, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BbmSubmission.StatusApproved, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, BbmSubmission.StatusRejected, StringComparison.OrdinalIgnoreCase))
                return;

            errors.Add(new ImportRowError
            {
                RowNumber = row,
                Column = "Status",
                Message = "Status hanya boleh Pending, Approved, atau Rejected."
            });
        }

        private static string NormalizeStatus(string value)
        {
            if (string.Equals(value, BbmSubmission.StatusApproved, StringComparison.OrdinalIgnoreCase))
                return BbmSubmission.StatusApproved;
            if (string.Equals(value, BbmSubmission.StatusRejected, StringComparison.OrdinalIgnoreCase))
                return BbmSubmission.StatusRejected;
            return BbmSubmission.StatusPending;
        }

        private static string BuildBusinessKey(string driverId, string periodeId, string kendaraanId, DateTime tanggal)
            => $"{driverId}|{periodeId}|{kendaraanId}|{tanggal.Date:yyyy-MM-dd}";
    }
}
