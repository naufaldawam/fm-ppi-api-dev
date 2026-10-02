using System;
using System.Collections.Generic;
using System.IO;

namespace ApiService.Application.DTOs.Mobile
{

    // =========================================================================
    // =================== Daily Check Up / DCU DTOs ===================
    // =========================================================================
    public class CreateDcuRequest
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
    }

    public class UpdateDcuRequest
    {
        public string DriverId { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DataDcuDto
    {
        public string Id { get; set; } = string.Empty;
        public string DriverId { get; set; } = string.Empty;
        public string NoPekerjaDriver { get; set; } = string.Empty;
        public string NamaDriver { get; set; } = string.Empty;
        public DateTime TanggalDcu { get; set; }
        public decimal TekananDarahSistolik { get; set; }
        public decimal TekananDarahDiastolik { get; set; }
        public decimal SaturasiOksigen { get; set; }
        public decimal NadiDenyut { get; set; }
        public decimal SuhuTubuh { get; set; }
        public string StatusKesehatan { get; set; } = string.Empty;
        public string? Keterangan { get; set; }
        public bool IsActive { get; set; }
        public EvidenceDcuDto? Evidence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }

    public class EvidenceDcuDto
    {
        public string Id { get; set; } = string.Empty;
        public string DataDcuId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string GeneratedName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class DataDcuFilterRequest
    {
        public string? Search { get; set; }
        public string? DriverId { get; set; }
        public string? StatusKesehatan { get; set; }
        public DateTime? TanggalFrom { get; set; }
        public DateTime? TanggalTo { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}