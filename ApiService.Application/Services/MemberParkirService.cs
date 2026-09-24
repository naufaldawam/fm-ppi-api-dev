using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiService.Application.DTOs;
using ApiService.Application.Interfaces;
using ApiService.Domain.Entities;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;

namespace ApiService.Application.Services
{
    public interface IMemberParkirService
    {
        Task<ApiResponse<PagedResponse<MemberParkirDto>>> GetAllAsync(MemberParkirFilterRequest filter);
        Task<ApiResponse<MemberParkirDto>> GetByIdAsync(string id);
        /// <summary>Ambil satu pekerja by id - untuk prefill form Member Parkir (Jabatan + RfIds).</summary>
        Task<ApiResponse<PekerjaLookupDto>> GetPekerjaByPekerjaIdAsync(string pekerjaId);
        Task<ApiResponse<MemberParkirDto>> CreateAsync(CreateMemberParkirRequest request, string userId);
        Task<ApiResponse<MemberParkirDto>> UpdateAsync(string id, UpdateMemberParkirRequest request, string userId);
        Task<ApiResponse<bool>> DeleteAsync(string id, string userId);
        Task<ApiResponse<MemberParkirImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId);
        Task<ApiResponse<FileResult>> GetImportTemplateAsync();
        Task<ApiResponse<MemberParkirSummaryDto>> GetSummaryAsync(MemberParkirSummaryRequest filter);
    }

    public class MemberParkirService : IMemberParkirService
    {
        private readonly IServiceDbContext _context;

        public MemberParkirService(IServiceDbContext context)
        {
            _context = context;
        }

        private IQueryable<MemberParkir> BaseQuery() =>
            _context.MemberParkirs
                .Include(m => m.Pekerja)
                    .ThenInclude(p => p!.Jabatan)
                .Include(m => m.Jabatan)
                .Include(m => m.Periode)
                .Where(m => !m.IsDeleted);

        public async Task<ApiResponse<PagedResponse<MemberParkirDto>>> GetAllAsync(MemberParkirFilterRequest filter)
        {
            var query = BaseQuery();

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(m =>
                    (m.Pekerja != null && m.Pekerja.NoPekerja.Contains(filter.Search)) ||
                    (m.Pekerja != null && m.Pekerja.NamaPekerja.Contains(filter.Search)));

            if (!string.IsNullOrEmpty(filter.JabatanId))
                query = query.Where(m => m.JabatanId == filter.JabatanId);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(m => m.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(m => m.IsActive == filter.IsActive.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResponse<MemberParkirDto>>.SuccessResponse(new PagedResponse<MemberParkirDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<MemberParkirDto>> GetByIdAsync(string id)
        {
            var member = await BaseQuery().FirstOrDefaultAsync(m => m.Id == id);

            if (member == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(member));
        }

        public async Task<ApiResponse<PekerjaLookupDto>> GetPekerjaByPekerjaIdAsync(string pekerjaId)
        {
            if (string.IsNullOrWhiteSpace(pekerjaId))
                return ApiResponse<PekerjaLookupDto>.BadRequest("PekerjaId wajib diisi.");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == pekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<PekerjaLookupDto>.NotFound("Pekerja tidak ditemukan");

            return ApiResponse<PekerjaLookupDto>.SuccessResponse(new PekerjaLookupDto
            {
                Id = pekerja.Id,
                NoPekerja = pekerja.NoPekerja,
                NamaPekerja = pekerja.NamaPekerja,
                JabatanId = pekerja.JabatanId,
                JabatanName = pekerja.Jabatan?.Name ?? string.Empty,
                RfIds = pekerja.RfIds
            });
        }

        public async Task<ApiResponse<MemberParkirDto>> CreateAsync(CreateMemberParkirRequest request, string userId)
        {
            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-003", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-007", "Periode tidak ditemukan");

            var member = new MemberParkir
            {
                PekerjaId = request.PekerjaId,
                // Jabatan otomatis dihandle server-side (client tidak bisa diset)
                JabatanId = pekerja.JabatanId,
                PeriodeId = request.PeriodeId,
                TanggalPenagihan = request.TanggalPenagihan,
                JumlahBiaya = request.JumlahBiaya,
                IsActive = true,
                CreatedBy = userId
            };

            _context.MemberParkirs.Add(member);
            await _context.SaveChangesAsync();

            var created = await BaseQuery().FirstAsync(m => m.Id == member.Id);
            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(created), "Member parkir berhasil ditambahkan");
        }

        public async Task<ApiResponse<MemberParkirDto>> UpdateAsync(string id, UpdateMemberParkirRequest request, string userId)
        {
            var member = await _context.MemberParkirs
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (member == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            var pekerja = await _context.Pekerjas
                .Include(p => p.Jabatan)
                .FirstOrDefaultAsync(p => p.Id == request.PekerjaId && !p.IsDeleted);

            if (pekerja == null)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-003", "Pekerja tidak ditemukan");

            var periodeOk = await _context.Periodes
                .AnyAsync(p => p.Id == request.PeriodeId && !p.IsDeleted);

            if (!periodeOk)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-007", "Periode tidak ditemukan");

            member.PekerjaId = request.PekerjaId;
            // Jabatan otomatis mengikuti Pekerja (perubahan tidak dari client)
            member.JabatanId = pekerja.JabatanId;
            member.PeriodeId = request.PeriodeId;
            member.TanggalPenagihan = request.TanggalPenagihan;
            member.JumlahBiaya = request.JumlahBiaya;
            member.IsActive = request.IsActive;
            member.ModifiedAt = DateTime.UtcNow;
            member.ModifiedBy = userId;

            await _context.SaveChangesAsync();

            var updated = await BaseQuery().FirstAsync(m => m.Id == member.Id);
            return ApiResponse<MemberParkirDto>.SuccessResponse(MapToDto(updated), "Member parkir berhasil diperbarui");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(string id, string userId)
        {
            var member = await _context.MemberParkirs
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

            if (member == null)
                return ApiResponse<bool>.ErrorResponse("ERR-MEMBERPARKIR-001", "Member parkir not found");

            member.IsDeleted = true;
            member.DeletedAt = DateTime.UtcNow;
            member.DeletedBy = userId;

            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Member parkir deleted");
        }

        // =========================================================
        // BULK UPLOAD MEMBER PARKIR
        // Template kolom: NoPekerja | Periode | TanggalPenagihan | JumlahBiaya
        // Lookup Pekerja via NoPekerja; Periode via NamaPeriode.
        // RF.ID TIDAK ada di template - selalu ditarik dari Pekerja.RfIds saat preview/ditampilkan.
        // =========================================================

        private const int MemberParkirImportHeaderRow = 1;
        private const int MemberParkirImportDataStartRow = 2;

        private static readonly string[] MemberParkirRequiredImportHeaders =
        {
            "nopekerja", "periode", "tanggalpenagihan", "jumlahbiaya"
        };

        public async Task<ApiResponse<MemberParkirImportResponse>> ImportFromExcelAsync(Stream fileStream, string userId)
        {
            var response = new MemberParkirImportResponse();

            if (fileStream == null || !fileStream.CanRead)
                return ApiResponse<MemberParkirImportResponse>.BadRequest("File tidak dapat dibaca.");

            try
            {
                using var workbook = new XLWorkbook(fileStream);

                if (workbook.Worksheets.Count == 0)
                    return ApiResponse<MemberParkirImportResponse>.BadRequest("Worksheet Excel tidak ditemukan.");

                var sheet = workbook.Worksheet(1);
                var usedRange = sheet.RangeUsed();

                if (usedRange == null)
                    return ApiResponse<MemberParkirImportResponse>.BadRequest("Worksheet Excel kosong.");

                var headerMap = BuildMemberParkirImportHeaderMap(sheet, MemberParkirImportHeaderRow);
                var lastRow = usedRange.LastRow().RowNumber();

                var missingHeaders = MemberParkirRequiredImportHeaders
                    .Where(x => !headerMap.ContainsKey(x))
                    .ToList();

                if (missingHeaders.Count > 0)
                {
                    return ApiResponse<MemberParkirImportResponse>.BadRequest(
                        $"Header template tidak lengkap atau sudah diubah. Kolom hilang: {string.Join(", ", missingHeaders)}.");
                }

                var parsedRows = new List<MemberParkirImportRow>();

                for (var row = MemberParkirImportDataStartRow; row <= lastRow; row++)
                {
                    if (IsMemberParkirImportRowEmpty(sheet, row, headerMap))
                        continue;

                    response.TotalRows++;

                    var parsedRow = new MemberParkirImportRow
                    {
                        RowNumber = row,
                        NoPekerja = GetMemberParkirImportCellText(sheet, row, headerMap, "NoPekerja"),
                        PeriodeName = GetMemberParkirImportCellText(sheet, row, headerMap, "Periode"),
                        TanggalPenagihanText = GetMemberParkirImportCellText(sheet, row, headerMap, "TanggalPenagihan"),
                        JumlahBiayaText = GetMemberParkirImportCellText(sheet, row, headerMap, "JumlahBiaya")
                    };

                    ValidateMemberParkirRequiredField(response.Errors, row, "NoPekerja", parsedRow.NoPekerja);
                    ValidateMemberParkirRequiredField(response.Errors, row, "Periode", parsedRow.PeriodeName);
                    ValidateMemberParkirRequiredField(response.Errors, row, "TanggalPenagihan", parsedRow.TanggalPenagihanText);
                    ValidateMemberParkirRequiredField(response.Errors, row, "JumlahBiaya", parsedRow.JumlahBiayaText);

                    parsedRow.TanggalPenagihan = ReadImportDate(sheet, row, headerMap);
                    if (parsedRow.TanggalPenagihan == null)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "TanggalPenagihan",
                            Message = "TanggalPenagihan tidak valid. Format: yyyy-MM-dd."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(parsedRow.JumlahBiayaText))
                    {
                        parsedRow.JumlahBiaya = ParseImportDecimal(sheet, row, headerMap, "JumlahBiaya");

                        if (parsedRow.JumlahBiaya == null)
                        {
                            response.Errors.Add(new ImportRowError
                            {
                                RowNumber = row,
                                Column = "JumlahBiaya",
                                Message = "JumlahBiaya tidak valid. Isi angka murni, contoh: 999000000."
                            });
                        }
                        else if (parsedRow.JumlahBiaya <= 0)
                        {
                            response.Errors.Add(new ImportRowError
                            {
                                RowNumber = row,
                                Column = "JumlahBiaya",
                                Message = "JumlahBiaya harus lebih dari 0."
                            });
                        }
                    }

                    parsedRows.Add(parsedRow);
                }

                if (parsedRows.Count == 0)
                    return ApiResponse<MemberParkirImportResponse>.BadRequest("Tidak ada data Member Parkir pada file Excel.");

                // =========================
                // DUPLIKAT (NoPekerja + Periode) DI DALAM FILE
                // =========================
                var memberKeyInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.NoPekerja) && !string.IsNullOrWhiteSpace(x.PeriodeName))
                    .GroupBy(x => $"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}", StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.NoPekerja) &&
                             !string.IsNullOrWhiteSpace(x.PeriodeName) &&
                             memberKeyInFile.Contains($"{x.NoPekerja.Trim()}|{x.PeriodeName.Trim()}")))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "NoPekerja",
                        Message = $"NoPekerja '{row.NoPekerja}' di periode '{row.PeriodeName}' duplikat di dalam file Excel."
                    });
                }

                // =========================
                // LOAD MASTER DATA SEKALI (include Jabatan untuk preview)
                // =========================
                var pekerjas = await _context.Pekerjas
                    .Include(p => p.Jabatan)
                    .Where(x => !x.IsDeleted)
                    .ToListAsync();
                var periodes = await _context.Periodes.Where(x => !x.IsDeleted).ToListAsync();
                var existingMembers = await _context.MemberParkirs.Where(x => !x.IsDeleted).ToListAsync();

                var existingMemberKeys = existingMembers
                    .Select(m => $"{m.PekerjaId}|{m.PeriodeId}")
                    .ToHashSet();

                // =========================
                // VALIDASI PER ROW: Pekerja, Periode, Duplikat DB
                // =========================
                var contextRows = new List<MemberParkirImportContext>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja) || string.IsNullOrWhiteSpace(row.PeriodeName))
                        continue; // sudah ditangani required

                    if (row.JumlahBiaya == null)
                        continue; // sudah ditangani validasi JumlahBiaya

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

                    var memberKey = $"{pekerja.Id}|{periode.Id}";
                    if (existingMemberKeys.Contains(memberKey))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "NoPekerja",
                            Message = $"Member parkir NoPekerja '{row.NoPekerja}' di periode '{row.PeriodeName}' sudah terdaftar."
                        });
                        continue;
                    }

                    contextRows.Add(new MemberParkirImportContext
                    {
                        Row = row,
                        Pekerja = pekerja,
                        Periode = periode
                    });
                }

                // =========================
                // JIKA ADA ERROR VALIDASI -> TIDAK ADA YANG DISIMPAN (all-or-nothing)
                // =========================
                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedMemberParkir = 0;

                    return ApiResponse<MemberParkirImportResponse>.SuccessResponse(
                        response,
                        "Import selesai dengan error. Tidak ada data Member Parkir yang disimpan.");
                }

                // =========================
                // BUILD & SAVE
                // =========================
                var now = DateTime.UtcNow;
                var newMembers = new List<MemberParkir>();

                foreach (var ctx in contextRows)
                {
                    var member = new MemberParkir
                    {
                        PekerjaId = ctx.Pekerja.Id,
                        // Jabatan otomatis dari Pekerja (server-side)
                        JabatanId = ctx.Pekerja.JabatanId,
                        PeriodeId = ctx.Periode.Id,
                        TanggalPenagihan = (DateTime)ctx.Row.TanggalPenagihan!,
                        JumlahBiaya = ctx.Row.JumlahBiaya!.Value,
                        IsActive = true,
                        CreatedBy = userId,
                        CreatedAt = now
                    };

                    newMembers.Add(member);

                    if (response.Preview.Count < 10)
                    {
                        response.Preview.Add(new MemberParkirImportPreviewDto
                        {
                            NoPekerja = ctx.Row.NoPekerja.Trim(),
                            NamaPekerja = ctx.Pekerja.NamaPekerja,
                            JabatanName = ctx.Pekerja.Jabatan?.Name ?? string.Empty,
                            RfIds = ctx.Pekerja.RfIds,
                            NamaPeriode = ctx.Periode.NamaPeriode,
                            TanggalPenagihan = member.TanggalPenagihan,
                            JumlahBiaya = member.JumlahBiaya
                        });
                    }
                }

                await _context.MemberParkirs.AddRangeAsync(newMembers);
                await _context.SaveChangesAsync();

                response.InsertedMemberParkir = newMembers.Count;
                response.SuccessCount = newMembers.Count;
                response.ErrorCount = 0;

                return ApiResponse<MemberParkirImportResponse>.SuccessResponse(
                    response,
                    $"{response.InsertedMemberParkir} Member Parkir berhasil diimport.");
            }
            catch (DbUpdateException ex)
            {
                return ApiResponse<MemberParkirImportResponse>.ErrorResponse(
                    "ERR-MEMBERPARKIR-IMPORT-001", $"Terjadi kesalahan saat menyimpan data Member Parkir: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                return ApiResponse<MemberParkirImportResponse>.ErrorResponse(
                    "ERR-MEMBERPARKIR-IMPORT-002", $"Gagal mengimport data Member Parkir: {ex.Message}");
            }
        }

        public async Task<ApiResponse<FileResult>> GetImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("MemberParkir");

            var headers = new[] { "NoPekerja", "Periode", "TanggalPenagihan", "JumlahBiaya" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(MemberParkirImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(MemberParkirImportHeaderRow, 1, MemberParkirImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (italic abu-abu - harus dihapus/ditimpa user)
            // JumlahBiaya: angka murni (Number), BUKAN teks berformat mata uang.
            sheet.Cell(2, 1).Value = "19280027";
            sheet.Cell(2, 2).Value = "Januari 2026";
            sheet.Cell(2, 3).Value = new DateTime(2026, 9, 11);
            sheet.Cell(2, 4).Value = 999000000;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.Italic = true;
            sheet.Range(2, 1, 2, headers.Length).Style.Font.FontColor = XLColor.Gray;

            // Format kolom JumlahBiaya sebagai angka dengan pemisah ribuan, biar user tahu
            // ini harus diisi angka murni (bukan "Rp.999.000.000" sebagai teks).
            sheet.Column(4).Style.NumberFormat.Format = "#,##0";
            sheet.Column(3).Style.DateFormat.Format = "yyyy-mm-dd";

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
                FileName = "Template_Upload_MemberParkir.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileStream = resultStream
            });
        }

        /// <summary>
        /// Total record Member Parkir & grand total JumlahBiaya. Kalau PeriodeId diisi,
        /// dihitung hanya untuk periode itu; kalau kosong, dihitung untuk semua periode.
        /// </summary>
        public async Task<ApiResponse<MemberParkirSummaryDto>> GetSummaryAsync(MemberParkirSummaryRequest filter)
        {
            string? namaPeriode = null;

            if (!string.IsNullOrEmpty(filter.PeriodeId))
            {
                namaPeriode = await _context.Periodes
                    .Where(p => p.Id == filter.PeriodeId && !p.IsDeleted)
                    .Select(p => p.NamaPeriode)
                    .FirstOrDefaultAsync();

                if (namaPeriode == null)
                    return ApiResponse<MemberParkirSummaryDto>.NotFound("Periode tidak ditemukan");
            }

            var query = _context.MemberParkirs.Where(m => !m.IsDeleted);

            if (!string.IsNullOrEmpty(filter.PeriodeId))
                query = query.Where(m => m.PeriodeId == filter.PeriodeId);

            if (filter.IsActive.HasValue)
                query = query.Where(m => m.IsActive == filter.IsActive.Value);

            var totalMemberParkir = await query.CountAsync();
            var grandTotalBiaya = totalMemberParkir == 0
                ? 0m
                : await query.SumAsync(m => m.JumlahBiaya);

            return ApiResponse<MemberParkirSummaryDto>.SuccessResponse(new MemberParkirSummaryDto
            {
                PeriodeId = filter.PeriodeId,
                NamaPeriode = namaPeriode,
                TotalMemberParkir = totalMemberParkir,
                GrandTotalBiaya = grandTotalBiaya
            });
        }

        // =========================================================
        // HELPERS (private, khusus import Member Parkir)
        // =========================================================

        private static Dictionary<string, int> BuildMemberParkirImportHeaderMap(IXLWorksheet sheet, int headerRow)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.RangeUsed()?.LastColumn().ColumnNumber() ?? 0;

            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(headerRow, column).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(header)) continue;

                var normalized = NormalizeMemberParkirImportHeader(header);
                if (!result.ContainsKey(normalized))
                    result.Add(normalized, column);
            }

            return result;
        }

        private static string NormalizeMemberParkirImportHeader(string value) =>
            new(value.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string GetMemberParkirImportCellText(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeMemberParkirImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return string.Empty;

            return sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty;
        }

        private static bool IsMemberParkirImportRowEmpty(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var noPekerja = GetMemberParkirImportCellText(sheet, row, headerMap, "NoPekerja");
            var periode = GetMemberParkirImportCellText(sheet, row, headerMap, "Periode");
            return string.IsNullOrWhiteSpace(noPekerja) && string.IsNullOrWhiteSpace(periode);
        }

        private static void ValidateMemberParkirRequiredField(
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

        private static IXLCell? GetImportCell(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var normalized = NormalizeMemberParkirImportHeader(header);
            if (!headerMap.TryGetValue(normalized, out var column))
                return null;

            return sheet.Cell(row, column);
        }

        /// <summary>
        /// Parse JumlahBiaya dari cell Excel ke decimal:
        /// 1) Cell numerik -> ambil langsung nilainya (tidak perlu parsing teks).
        /// 2) Cell teks -> bersihkan simbol non-angka ("Rp", spasi, dll), lalu deteksi
        ///    apakah koma atau titik dipakai sebagai pemisah desimal berdasarkan mana
        ///    yang muncul terakhir, supaya format "999.000.000" maupun "999000000,50"
        ///    sama-sama bisa terbaca.
        /// </summary>
        private static decimal? ParseImportDecimal(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap, string header)
        {
            var cell = GetImportCell(sheet, row, headerMap, header);
            if (cell == null) return null;

            if (cell.DataType == XLDataType.Number)
                return cell.GetValue<decimal>();

            var text = cell.GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return null;

            var cleaned = new string(text.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
            if (string.IsNullOrEmpty(cleaned)) return null;

            var lastComma = cleaned.LastIndexOf(',');
            var lastDot = cleaned.LastIndexOf('.');

            var normalized = lastComma > lastDot
                ? cleaned.Replace(".", "").Replace(",", ".")   // koma = desimal, titik = ribuan
                : cleaned.Replace(",", "");                     // titik = desimal (atau tanpa desimal), koma = ribuan

            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
                ? value
                : (decimal?)null;
        }

        /// <summary>
        /// Ambil tanggal dari cell Excel:
        /// 1) Kalau cell type DATE -> ClosedXML GetDateTime() langsung return DateTime (no parsing need).
        /// 2) Fallback text -> ParseImportDate (format ISO yyyy-MM-dd).
        /// </summary>
        private static DateTime? ReadImportDate(
            IXLWorksheet sheet, int row, Dictionary<string, int> headerMap)
        {
            var normalized = NormalizeMemberParkirImportHeader("TanggalPenagihan");
            if (!headerMap.TryGetValue(normalized, out var column))
                return null;

            try
            {
                return sheet.Cell(row, column).GetDateTime();
            }
            catch (Exception ex)
            {
                // bukan cell date - fallback ke text parse
            }

            return ParseImportDate(sheet.Cell(row, column).GetString()?.Trim() ?? string.Empty);
        }

        /// <summary>
        /// Parse tanggal ISO "yyyy-MM-dd" (opsional trailing waktu diakcept).
        /// TIDAK pakai DateTime.ParseExact - API ini tidak tersedia di dialect ini.
        /// </summary>
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

        /// <summary>Context hasil lookup saat fase import (entity resolved server-side).</summary>
        private sealed class MemberParkirImportContext
        {
            public MemberParkirImportRow Row { get; set; } = null!;
            public Pekerja Pekerja { get; set; } = null!;
            public Periode Periode { get; set; } = null!;
        }

        private static MemberParkirDto MapToDto(MemberParkir m) => new()
        {
            Id = m.Id,
            PekerjaId = m.PekerjaId,
            NoPekerja = m.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = m.Pekerja?.NamaPekerja ?? string.Empty,
            JabatanId = m.JabatanId,
            JabatanName = m.Jabatan?.Name ?? m.Pekerja?.Jabatan?.Name ?? string.Empty,
            // RF.ID selalu ditarik dari Pekerja.RfIds (bisa kosong, bisa lebih dari satu)
            RfIds = m.Pekerja?.RfIds ?? new List<string>(),
            PeriodeId = m.PeriodeId,
            NamaPeriode = m.Periode?.NamaPeriode ?? string.Empty,
            TanggalPenagihan = m.TanggalPenagihan,
            JumlahBiaya = m.JumlahBiaya,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt,
            ModifiedAt = m.ModifiedAt
        };
    }
}