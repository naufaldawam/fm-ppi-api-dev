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
                    m.RfIdCode.Contains(filter.Search) ||
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

            // RF.ID dihandle server-side: client boleh kosong, server yang ambil/validasi
            var resolution = await ResolveRfIdCodeAsync(pekerja, request.RfIdCode);
            if (resolution.ErrorMessage != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse(resolution.ErrorCode!, resolution.ErrorMessage);

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == resolution.Code && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            var member = new MemberParkir
            {
                PekerjaId = request.PekerjaId,
                // Jabatan + RF.ID otomatis dihandle server-side (client tidak bisa diset)
                JabatanId = pekerja.JabatanId,
                PeriodeId = request.PeriodeId,
                RfIdCode = resolution.Code,
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

            // RF.ID dihandle server-side: client boleh kosong, server yang ambil/validasi
            var resolution = await ResolveRfIdCodeAsync(pekerja, request.RfIdCode);
            if (resolution.ErrorMessage != null)
                return ApiResponse<MemberParkirDto>.ErrorResponse(resolution.ErrorCode!, resolution.ErrorMessage);

            var rfidCodeExists = await _context.MemberParkirs
                .AnyAsync(m => m.RfIdCode == resolution.Code && m.Id != id && !m.IsDeleted);

            if (rfidCodeExists)
                return ApiResponse<MemberParkirDto>.ErrorResponse("ERR-MEMBERPARKIR-004", "RF.ID sudah terdaftar sebagai member parkir");

            member.PekerjaId = request.PekerjaId;
            // Jabatan + RF.ID otomatis mengikuti Pekerja (perubahan tidak dari client)
            member.JabatanId = pekerja.JabatanId;
            member.PeriodeId = request.PeriodeId;
            member.RfIdCode = resolution.Code;
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

        private sealed class RfIdResolution
        {
            public string Code { get; set; } = string.Empty;
            public string? ErrorCode { get; set; }
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// RF.ID dihandle server-side:
        /// - Client diisi kode -> server validasi kode ter-assign ke pekerja ini (via master RfIds).
        /// - Client kosong      -> server ambil kode pertama dari Pekerja.RfIds.
        /// </summary>
        private async Task<RfIdResolution> ResolveRfIdCodeAsync(Pekerja pekerja, string? requestCode)
        {
            var trimmed = string.IsNullOrWhiteSpace(requestCode) ? string.Empty : requestCode.Trim();

            if (!string.IsNullOrEmpty(trimmed))
            {
                var master = await _context.RfIds
                    .FirstOrDefaultAsync(r => r.RfIdCode == trimmed && !r.IsDeleted);

                if (master != null && master.PekerjaId != pekerja.Id)
                {
                    return new RfIdResolution
                    {
                        ErrorCode = "ERR-MEMBERPARKIR-005",
                        ErrorMessage = master.PekerjaId == null
                            ? $"RF.ID '{trimmed}' belum di-assign ke Pekerja '{pekerja.NoPekerja}'."
                            : $"RF.ID '{trimmed}' sudah di-assign ke Pekerja lain, tidak bisa dipakai."
                    };
                }

                return new RfIdResolution { Code = trimmed };
            }

            if (pekerja.RfIds.Count > 0)
                return new RfIdResolution { Code = pekerja.RfIds[0].Trim() };

            return new RfIdResolution
            {
                ErrorCode = "ERR-MEMBERPARKIR-006",
                ErrorMessage = $"Pekerja '{pekerja.NoPekerja}' belum memiliki RF.ID di-assign. Kode RF.ID wajib diisi."
            };
        }

        // =========================================================
        // BULK UPLOAD MEMBER PARKIR
        // Template kolom: NoPekerja | Rfid | Periode | TanggalPenagihan | JumlahBiaya
        // Lookup Pekerja via NoPekerja; Periode via NamaPeriode;
        // RF.ID server-side: diisi -> validasi belong to pekerja, kosong -> Pekerja.RfIds[0]
        // =========================================================

        private const int MemberParkirImportHeaderRow = 1;
        private const int MemberParkirImportDataStartRow = 2;

        private static readonly string[] MemberParkirRequiredImportHeaders =
        {
            "nopekerja", "rfid", "periode", "tanggalpenagihan", "jumlahbiaya"
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
                        RfIdCode = GetMemberParkirImportCellText(sheet, row, headerMap, "Rfid"),
                        PeriodeName = GetMemberParkirImportCellText(sheet, row, headerMap, "Periode"),
                        TanggalPenagihanText = GetMemberParkirImportCellText(sheet, row, headerMap, "TanggalPenagihan"),
                        JumlahBiaya = GetMemberParkirImportCellText(sheet, row, headerMap, "JumlahBiaya")
                    };

                    ValidateMemberParkirRequiredField(response.Errors, row, "NoPekerja", parsedRow.NoPekerja);
                    ValidateMemberParkirRequiredField(response.Errors, row, "Periode", parsedRow.PeriodeName);
                    ValidateMemberParkirRequiredField(response.Errors, row, "TanggalPenagihan", parsedRow.TanggalPenagihanText);
                    ValidateMemberParkirRequiredField(response.Errors, row, "JumlahBiaya", parsedRow.JumlahBiaya);
                    // Rfid opsional - dihandle server-side (ambil dari Pekerja.RfIds kalau kosong)

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

                    if (parsedRow.JumlahBiaya.Length > 50)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row,
                            Column = "JumlahBiaya",
                            Message = "JumlahBiaya maksimum 50 karakter."
                        });
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
                // DUPLIKAT RF.ID DI DALAM FILE
                // =========================
                var rfidInFile = parsedRows
                    .Where(x => !string.IsNullOrWhiteSpace(x.RfIdCode))
                    .GroupBy(x => x.RfIdCode.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var row in parsedRows.Where(x =>
                             !string.IsNullOrWhiteSpace(x.RfIdCode) &&
                             rfidInFile.Contains(x.RfIdCode.Trim())))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = row.RowNumber,
                        Column = "Rfid",
                        Message = $"Rfid '{row.RfIdCode}' duplikat di dalam file Excel."
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
                var rfIdMaster = await _context.RfIds.Where(x => !x.IsDeleted).ToListAsync();
                var existingMembers = await _context.MemberParkirs.Where(x => !x.IsDeleted).ToListAsync();

                var existingMemberKeys = existingMembers
                    .Select(m => $"{m.PekerjaId}|{m.PeriodeId}")
                    .ToHashSet();

                var existingRfIdSet = existingMembers
                    .Select(m => m.RfIdCode.Trim().ToLowerInvariant())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToHashSet();

                // =========================
                // VALIDASI PER ROW: Pekerja, Periode, Duplikat DB, RF.ID
                // =========================
                var contextRows = new List<MemberParkirImportContext>();

                foreach (var row in parsedRows)
                {
                    if (string.IsNullOrWhiteSpace(row.NoPekerja) || string.IsNullOrWhiteSpace(row.PeriodeName))
                        continue; // sudah ditangani required

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

                    // RF.ID dihandle server-side (sama logika dengan create)
                    var rfid = row.RfIdCode.Trim();
                    if (string.IsNullOrEmpty(rfid))
                    {
                        if (pekerja.RfIds.Count > 0)
                            rfid = pekerja.RfIds[0].Trim();
                        else
                        {
                            response.Errors.Add(new ImportRowError
                            {
                                RowNumber = row.RowNumber,
                                Column = "Rfid",
                                Message = $"Pekerja '{row.NoPekerja}' belum memiliki RF.ID di-assign. Kode RF.ID wajib diisi."
                            });
                            continue;
                        }
                    }

                    var rfidMasterRow = rfIdMaster.FirstOrDefault(r =>
                        string.Equals(r.RfIdCode?.Trim(), rfid, StringComparison.OrdinalIgnoreCase));

                    if (rfidMasterRow != null && rfidMasterRow.PekerjaId != pekerja.Id)
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Rfid",
                            Message = rfidMasterRow.PekerjaId == null
                                ? $"RF.ID '{rfid}' belum di-assign ke Pekerja '{row.NoPekerja}'."
                                : $"RF.ID '{rfid}' sudah di-assign ke Pekerja lain, tidak bisa dipakai."
                        });
                        continue;
                    }

                    if (existingRfIdSet.Contains(rfid.ToLowerInvariant()))
                    {
                        response.Errors.Add(new ImportRowError
                        {
                            RowNumber = row.RowNumber,
                            Column = "Rfid",
                            Message = $"RF.ID '{rfid}' sudah terdaftar sebagai member parkir."
                        });
                        continue;
                    }

                    contextRows.Add(new MemberParkirImportContext
                    {
                        Row = row,
                        Pekerja = pekerja,
                        Periode = periode,
                        RfIdCode = rfid
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
                        // Jabatan + RF.ID otomatis dari Pekerja (server-side)
                        JabatanId = ctx.Pekerja.JabatanId,
                        PeriodeId = ctx.Periode.Id,
                        RfIdCode = ctx.RfIdCode,
                        TanggalPenagihan = (DateTime)ctx.Row.TanggalPenagihan,
                        JumlahBiaya = ctx.Row.JumlahBiaya.Trim(),
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
                            RfIdCode = member.RfIdCode,
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
                var dbMessage = ex.InnerException?.Message ?? ex.Message;

                if (dbMessage.Contains("IX_MemberParkir_RfIdCode", StringComparison.OrdinalIgnoreCase))
                {
                    response.Errors.Add(new ImportRowError
                    {
                        RowNumber = 0,
                        Column = "Rfid",
                        Message = "Terdapat RF.ID yang sudah terdaftar."
                    });
                }

                if (response.Errors.Count > 0)
                {
                    response.ErrorCount = response.Errors.Count;
                    response.SuccessCount = 0;
                    response.InsertedMemberParkir = 0;

                    return ApiResponse<MemberParkirImportResponse>.SuccessResponse(
                        response,
                        "Import dibatalkan karena terdapat data duplikat.");
                }

                return ApiResponse<MemberParkirImportResponse>.ErrorResponse(
                    "ERR-MEMBERPARKIR-IMPORT-001", "Terjadi kesalahan saat menyimpan data Member Parkir.");
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

            var headers = new[] { "NoPekerja", "Rfid", "Periode", "TanggalPenagihan", "JumlahBiaya" };
            for (var i = 0; i < headers.Length; i++)
                sheet.Cell(MemberParkirImportHeaderRow, i + 1).Value = headers[i];

            var headerRange = sheet.Range(MemberParkirImportHeaderRow, 1, MemberParkirImportHeaderRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCEEE8");
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Baris contoh (italic abu-abu - harus dihapus/ditimpa user)
            sheet.Cell(2, 1).Value = "19280027";
            sheet.Cell(2, 2).Value = "1234567891";
            sheet.Cell(2, 3).Value = "Januari 2026";
            sheet.Cell(2, 4).Value = "2026-09-11";
            sheet.Cell(2, 5).Value = "Rp.999.000.000";
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

            // Sheet referensi RF.ID - kode kartu + pekerja pemegang (untuk auto-fill Rfid)
            var rfIds = await _context.RfIds
                .Include(r => r.Pekerja)
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.RfIdCode)
                .ToListAsync();

            var rfidRefSheet = workbook.Worksheets.Add("Referensi RF.ID");
            rfidRefSheet.Cell(1, 1).Value = "RfIdCode (Valid)";
            rfidRefSheet.Cell(1, 2).Value = "No Pekerja";
            rfidRefSheet.Cell(1, 3).Value = "Nama Pekerja";
            rfidRefSheet.Range(1, 1, 1, 3).Style.Font.Bold = true;
            for (var i = 0; i < rfIds.Count; i++)
            {
                rfidRefSheet.Cell(i + 2, 1).Value = rfIds[i].RfIdCode;
                rfidRefSheet.Cell(i + 2, 2).Value = rfIds[i].Pekerja?.NoPekerja ?? string.Empty;
                rfidRefSheet.Cell(i + 2, 3).Value = rfIds[i].Pekerja?.NamaPekerja ?? string.Empty;
            }
            rfidRefSheet.Columns().AdjustToContents();

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
            public string RfIdCode { get; set; } = string.Empty;
        }

        private static MemberParkirDto MapToDto(MemberParkir m) => new()
        {
            Id = m.Id,
            PekerjaId = m.PekerjaId,
            NoPekerja = m.Pekerja?.NoPekerja ?? string.Empty,
            NamaPekerja = m.Pekerja?.NamaPekerja ?? string.Empty,
            JabatanId = m.JabatanId,
            JabatanName = m.Jabatan?.Name ?? m.Pekerja?.Jabatan?.Name ?? string.Empty,
            RfIdCode = m.RfIdCode,
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