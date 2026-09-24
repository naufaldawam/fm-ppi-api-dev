using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;

namespace ApiService.Application.Services
{
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
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── HEADER: satu tabel gabungan ─────────────────────────
                    col.Item().Table(headerTable =>
                    {
                        headerTable.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(3);
                            cd.RelativeColumn(2);
                        });

                        // Kiri: perusahaan + judul form (merged, dua baris dalam 1 cell)
                        headerTable.Cell()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten1)
                            .Padding(6)
                            .Column(c =>
                            {
                                c.Item().Text("PT PERTAMINA POWER INDONESIA").FontSize(7).Bold();
                                c.Item().PaddingTop(4)
                                    .Text("FORMULIR PENYELIDIKAN KECELAKAAN KERJA / INSIDEN")
                                    .FontSize(14).Bold();
                            });

                        // Kanan: logo di atas, nomor + revisi di bawah (satu cell berborder)
                        headerTable.Cell()
                            .Border(1)
                            .BorderColor(Colors.Grey.Lighten1)
                            .Padding(4)
                            .Column(c =>
                            {
                                if (_d.LogoBytes != null && _d.LogoBytes.Length > 0)
                                {
                                    // Row dengan spacer kiri/kanan -> logo selalu di tengah.
                                    // FitWidth: sesuaikan lebar 70, tinggi otomatis (logo ratio lebar).
                                    c.Item().PaddingBottom(3).Row(r =>
                                    {
                                        r.RelativeItem();
                                        r.ConstantItem(70).Height(35)
                                            .Image(_d.LogoBytes, ImageScaling.FitWidth);
                                        r.RelativeItem();
                                    });
                                }

                                c.Item()
                                    .Table(t =>
                                    {
                                        t.ColumnsDefinition(cd =>
                                        {
                                            cd.RelativeColumn(2);
                                            cd.RelativeColumn(3);
                                        });

                                        t.Cell().BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                            .Padding(2).Text("Nomor").FontSize(7);
                                        t.Cell().BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                            .Padding(2).Text(_d.Nomor).FontSize(7).Bold();

                                        t.Cell().Padding(2).Text("Revisi").FontSize(7);
                                        t.Cell().Padding(2).Text(_d.Revisi).FontSize(7);
                                    });
                            });
                    });

                    col.Item().PaddingVertical(5);

                    // ── RED BANNER ──────────────────────────────────────────
                    col.Item()
                        .Background(Colors.Red.Medium)
                        .Padding(7)
                        .AlignCenter()
                        .Text(_d.Judul)
                        .FontColor(Colors.Yellow.Medium)
                        .FontSize(13)
                        .Bold();

                    col.Item().PaddingVertical(5);

                    // ── BODY: sections full-width (breakable ke halaman berikutnya) ──
                    // Judul, Waktu, Dampak, Kategori
                    col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(4).Column(c =>
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("Judul : ").Bold();
                            t.Span(_d.Judul2 ?? _d.Judul);
                        });
                        c.Item().PaddingTop(3).Text(t =>
                        {
                            t.Span("Waktu : ").Bold();
                            t.Span($"{_d.Tanggal} pukul {_d.Waktu} WIB");
                        });
                        c.Item().PaddingTop(3).Text(t =>
                        {
                            t.Span("Dampak : ").Bold();
                            t.Span(_d.Dampak);
                        });
                        c.Item().PaddingTop(3).Text(t =>
                        {
                            t.Span("Kategori : ").Bold();
                            t.Span(_d.KategoriName);
                        });
                    });

                    // Kronologi (5W1H) - plain Text items (breakable, tidak pakai Row)
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Column(c =>
                        {
                            c.Item().Text("Kronologi Kejadian :").Bold();
                            c.Item().PaddingTop(2);

                            // WHEN
                            c.Item().Text(t =>
                            {
                                t.Span("• WHEN: ").Bold();
                                t.Span($"{_d.Tanggal}, pukul {_d.Waktu} WIB");
                            });
                            // WHERE
                            c.Item().Text(t =>
                            {
                                t.Span("• WHERE: ").Bold();
                                t.Span(_d.Alamat);
                            });
                            // WHO
                            c.Item().Text(t =>
                            {
                                t.Span("• WHO: ").Bold();
                                t.Span("Pengemudi " + _d.DriverInfo);
                            });
                            if (!string.IsNullOrWhiteSpace(_d.PejabatInfo))
                            {
                                c.Item().PaddingLeft(12).Text(
                                    "Penugasan dari: " + _d.PejabatInfo);
                            }
                            // WHAT
                            c.Item().Text(t =>
                            {
                                t.Span("• WHAT: ").Bold();
                                t.Span(_d.DetailKejadian);
                            });
                            // WHY
                            c.Item().Text(t =>
                            {
                                t.Span("• WHY: ").Bold();
                                t.Span(_d.PenyebabKejadian);
                            });
                            // HOW
                            c.Item().Text(t =>
                            {
                                t.Span("• HOW: ").Bold();
                                t.Span(_d.BagaimanaTerjadinya);
                            });
                        });

                    // Akar Permasalahan
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Column(c =>
                        {
                            c.Item().Text("Akar Permasalahan (Root Causes) :")
                                .Bold().Italic();
                            c.Item().PaddingTop(3)
                                .Text(FormatNumberedList(_d.AkarPermasalahan));
                        });

                    // Tindakan Segera
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Column(c =>
                        {
                            c.Item().Text("Tindakan segera yang dilakukan saat itu :")
                                .Bold();
                            c.Item().PaddingTop(3)
                                .Text(FormatNumberedList(_d.TindakanSegara));
                        });

                    // Tindakan Perbaikan
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Column(c =>
                        {
                            c.Item().Text("Tindakan Perbaikan (follow-up) :").Bold();
                            c.Item().PaddingTop(3)
                                .Text(FormatNumberedList(_d.TindakanPerbaikan));
                        });

                    // ── FOTO BUKTI (full width - bisa lanjut ke halaman berikutnya) ──
                    col.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1)
                        .Padding(4).Column(c =>
                        {
                            c.Item().Text("Foto Bukti :").Bold();

                            var photos = _d.FotoBukti
                                .Where(f => f != null && f.Length > 0)
                                .ToList();

                            if (photos.Count > 0)
                            {
                                var limit = photos.Count > 4 ? 4 : photos.Count;
                                for (var i = 0; i < limit; i++)
                                {
                                    c.Item().PaddingTop(4)
                                        .Width(120)
                                        .Height(80)
                                        .Image(photos[i], ImageScaling.FitWidth);
                                }
                            }
                        });
                });
            });
        }

        /// <summary>
        /// Convert plain text (HTML already stripped) into a numbered list string.
        /// Manual split on newlines - .Split / StringBuilder TIDAK tersedia di dialect.
        /// </summary>
        private static string FormatNumberedList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var lines = new List<string>();
            var current = new List<char>();

            foreach (var c in raw)
            {
                if (c == '\n' || c == '\r')
                {
                    if (current.Count > 0)
                    {
                        lines.Add(new string(current.ToArray()).Trim());
                        current = new List<char>();
                    }
                }
                else
                {
                    current.Add(c);
                }
            }

            if (current.Count > 0)
                lines.Add(new string(current.ToArray()).Trim());

            var nonEmpty = lines.Where(x => x.Length > 0).ToList();

            if (nonEmpty.Count <= 1)
                return raw.Trim();

            var result = string.Empty;
            var n = 1;
            foreach (var line in nonEmpty)
            {
                result += $"{n}. {line}";
                if (n < nonEmpty.Count)
                    result += "\n";
                n++;
            }

            return result;
        }
    }

    public class DataKecelakaanPdfDto
    {
        public string Nomor { get; set; } = string.Empty;
        public string Revisi { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Judul { get; set; } = string.Empty;

        /// <summary>
        /// Optional: full incident title for the left-column "Judul :" row.
        /// Falls back to Judul (the banner text) if null.
        /// </summary>
        public string? Judul2 { get; set; }

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

        /// <summary>Logo PPI (dari ApiService.API/Assets) - kosong kalau file tidak ada.</summary>
        public byte[]? LogoBytes { get; set; }
    }
}