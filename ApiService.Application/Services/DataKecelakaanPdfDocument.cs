using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace ApiService.Application.Services
{
    /// <summary>
    /// PDF Formulir Penyelidikan Kecelakaan Kerja / Insiden (Pertamina PPI).
    /// Pattern sama dengan MCU: implementasi QuestPDF IDocument + GeneratePdf().
    /// </summary>
    public class DataKecelakaanPdfDocument : IDocument
    {
        private readonly DataKecelakaanPdfDto _d;

        public DataKecelakaanPdfDocument(DataKecelakaanPdfDto data)
        {
            _d = data;
        }

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── HEADER ───────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(3).Column(c =>
                        {
                            c.Item().Text("PT PERTAMINA POWER INDONESIA").FontSize(8).Bold();
                            c.Item().Text("FORMULIR PENYELIDIKAN KECELAKAAN KERJA / INSIDEN")
                                .FontSize(13).Bold();
                        });

                        row.RelativeItem(1).Border(1).BorderColor(Colors.Grey.Lighten1).Column(c =>
                        {
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Padding(3).Text("Nomor").FontSize(7);
                                r.RelativeItem().Padding(3).Text(_d.Nomor).FontSize(7).Bold();
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Padding(3).Text("Status").FontSize(7);
                                r.RelativeItem().Padding(3).Text(_d.Status).FontSize(7);
                            });
                        });
                    });

                    col.Item().PaddingVertical(6);

                    // ── JUDUL MERAH ─────────────────────────────────────────
                    col.Item()
                        .Background(Colors.Red.Medium)
                        .Padding(8)
                        .AlignCenter()
                        .Text(_d.Judul)
                        .FontColor(Colors.White)
                        .FontSize(13)
                        .Bold();

                    col.Item().PaddingVertical(6);

                    // ── BODY: 2 KOLOM ───────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        // KOLOM KIRI
                        row.RelativeItem(3).Column(left =>
                        {
                            left.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("Waktu : ").Bold();
                                    t.Span($"{_d.Tanggal} pukul {_d.Waktu} WIB");
                                });
                            });

                            left.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text(t =>
                                {
                                    t.Span("Dampak : ").Bold();
                                    t.Span(_d.Dampak);
                                });
                                c.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Kategori : ").Bold();
                                    t.Span(_d.KategoriName);
                                });
                                c.Item().PaddingTop(4).Text(t =>
                                {
                                    t.Span("Periode : ").Bold();
                                    t.Span(_d.PeriodeName);
                                });
                            });

                            left.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text("Kronologi Kejadian :").Bold();
                                c.Item().Text($"WHERE: {_d.Alamat}");
                                c.Item().Text($"WHO  : Pengemudi {_d.DriverInfo}");
                                if (_d.PejabatInfo.Length > 0)
                                    c.Item().Text($"       Penugasan dari {_d.PejabatInfo}");
                                c.Item().Text($"WHAT : {_d.DetailKejadian}");
                                c.Item().Text($"WHY  : {_d.PenyebabKejadian}");
                                c.Item().Text($"HOW  : {_d.BagaimanaTerjadinya}");
                            });
                        });

                        // KOLOM KANAN
                        row.RelativeItem(2).Column(right =>
                        {
                            right.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text("Akar Permasalahan (Root Causes) :").Bold().Italic();
                                c.Item().PaddingTop(4).Text(_d.AkarPermasalahan);
                            });

                            right.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text("Tindakan segara yang dilakukan saat itu :").Bold();
                                c.Item().PaddingTop(4).Text(_d.TindakanSegara);
                            });

                            right.Item().Border(1).Padding(6).Column(c =>
                            {
                                c.Item().Text("Tindakan Perbaikan (follow-up) :").Bold();
                                c.Item().PaddingTop(4).Text(_d.TindakanPerbaikan);
                            });

                            // Foto Bukti
                            if (_d.FotoBukti.Count > 0)
                            {
                                right.Item().Border(1).Padding(6).Column(c =>
                                {
                                    c.Item().Text("Foto Bukti :").Bold().FontSize(8);
                                    foreach (var foto in _d.FotoBukti)
                                    {
                                        if (foto != null && foto.Length > 0)
                                        {
                                            c.Item().PaddingTop(4)
                                                .Width(160)
                                                .Height(105)
                                                .Image(foto);
                                        }
                                    }
                                });
                            }
                        });
                    });
                });
            });
        }
    }

    /// <summary>Data snapshot untuk render PDF (bebas dari entity/FK dependencies).</summary>
    public class DataKecelakaanPdfDto
    {
        public string Nomor { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Judul { get; set; } = string.Empty;
        public string Tanggal { get; set; } = string.Empty;
        public string Waktu { get; set; } = string.Empty;
        public string Dampak { get; set; } = string.Empty;
        public string KategoriName { get; set; } = string.Empty;
        public string PeriodeName { get; set; } = string.Empty;
        public string Alamat { get; set; } = string.Empty;
        public string DriverInfo { get; set; } = string.Empty;
        public string PejabatInfo { get; set; } = string.Empty;
        public string DetailKejadian { get; set; } = string.Empty;
        public string PenyebabKejadian { get; set; } = string.Empty;
        public string BagaimanaTerjadinya { get; set; } = string.Empty;
        public string AkarPermasalahan { get; set; } = string.Empty;
        public string TindakanSegara { get; set; } = string.Empty;
        public string TindakanPerbaikan { get; set; } = string.Empty;
        public List<byte[]> FotoBukti { get; set; } = new();
    }
}