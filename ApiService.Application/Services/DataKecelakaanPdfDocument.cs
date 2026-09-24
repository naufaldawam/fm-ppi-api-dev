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
                    // ── HEADER ───────────────────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(4).Column(c =>
                        {
                            c.Item().Text("PT PERTAMINA POWER INDONESIA").FontSize(7).Bold();
                            c.Item().PaddingTop(2)
                                .Text("FORMULIR PENYELIDIKAN KECELAKAAN KERJA / INSIDEN")
                                .FontSize(14).Bold();
                        });

                        row.RelativeItem(2).Column(c =>
                        {
                            c.Item().AlignRight().Row(logoRow =>
                            {
                                logoRow.AutoItem().Column(lc =>
                                {
                                    lc.Item().AlignRight().Text("PERTAMINA").FontSize(10).Bold()
                                        .FontColor(Colors.Red.Medium);
                                    lc.Item().AlignRight().Text("POWER INDONESIA").FontSize(6)
                                        .FontColor(Colors.Grey.Darken2);
                                });
                            });

                            c.Item().PaddingTop(3).Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(2);
                                        cd.RelativeColumn(3);
                                    });

                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                        .Padding(2).Text("Nomor").FontSize(7);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten1)
                                        .Padding(2).Text(_d.Nomor).FontSize(7).Bold();

                                    t.Cell().Padding(2).Text("Revisi").FontSize(7);
                                    t.Cell().Padding(2).Text(_d.Status).FontSize(7);
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
                        .FontColor(Colors.White)
                        .FontSize(13)
                        .Bold();

                    col.Item().PaddingVertical(5);

                    // ── BODY: LEFT (3) + RIGHT (2) ──────────────────────────
                    col.Item().Row(row =>
                    {
                        // ── LEFT COLUMN ─────────────────────────────────────
                        row.RelativeItem(3).Column(left =>
                        {
                            // Judul
                            left.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.ConstantColumn(90);
                                        cd.RelativeColumn();
                                    });
                                    t.Cell().Padding(4).Text("Judul :").Bold();
                                    t.Cell().Padding(4).Text(_d.Judul2 ?? _d.Judul);
                                });

                            // Waktu
                            left.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Table(t =>
                                {
                                    t.ColumnsDefinition(cd =>
                                    {
                                        cd.ConstantColumn(90);
                                        cd.RelativeColumn();
                                    });
                                    t.Cell().Padding(4).Text("Waktu :").Bold();
                                    t.Cell().Padding(4)
                                        .Text($"{_d.Tanggal} pukul {_d.Waktu} WIB");
                                });

                            // Dampak + Kategori
                            left.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text(t =>
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

                            // Kronologi (bullet list - inline, no local function)
                            left.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text("Kronologi Kejadian :").Bold();
                                    c.Item().PaddingTop(2);

                                    // WHEN
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("WHEN: ").Bold();
                                            t.Span($"{_d.Tanggal}, pukul {_d.Waktu} WIB");
                                        });
                                    });
                                    // WHERE
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("WHERE: ").Bold();
                                            t.Span(_d.Alamat);
                                        });
                                    });
                                    // WHO
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Column(wc =>
                                        {
                                            wc.Item().Text(t =>
                                            {
                                                t.Span("WHO").Bold();
                                                t.Span(":");
                                            });
                                            wc.Item().PaddingLeft(8).Text(
                                                $"Pengemudi: {_d.DriverInfo}");
                                            if (!string.IsNullOrWhiteSpace(_d.PejabatInfo))
                                                wc.Item().PaddingLeft(8).Text(
                                                    $"Penugasan dari: {_d.PejabatInfo}");
                                        });
                                    });
                                    // WHAT
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("WHAT: ").Bold();
                                            t.Span(_d.DetailKejadian);
                                        });
                                    });
                                    // WHY
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("WHY: ").Bold();
                                            t.Span(_d.PenyebabKejadian);
                                        });
                                    });
                                    // HOW
                                    c.Item().Row(r =>
                                    {
                                        r.AutoItem().Text("• ").FontSize(8);
                                        r.RelativeItem().Text(t =>
                                        {
                                            t.Span("HOW: ").Bold();
                                            t.Span(_d.BagaimanaTerjadinya);
                                        });
                                    });
                                });
                        });

                        // ── RIGHT COLUMN ────────────────────────────────────
                        row.RelativeItem(2).Column(right =>
                        {
                            right.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text("Akar Permasalahan (Root Causes) :")
                                        .Bold().Italic();
                                    c.Item().PaddingTop(3)
                                        .Text(FormatNumberedList(_d.AkarPermasalahan));
                                });

                            right.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text("Tindakan segera yang dilakukan saat itu :")
                                        .Bold();
                                    c.Item().PaddingTop(3)
                                        .Text(FormatNumberedList(_d.TindakanSegara));
                                });

                            right.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text("Tindakan Perbaikan (follow-up) :").Bold();
                                    c.Item().PaddingTop(3)
                                        .Text(FormatNumberedList(_d.TindakanPerbaikan));
                                });

                            // Foto Bukti
                            right.Item().Border(1).BorderColor(Colors.Grey.Lighten1)
                                .Padding(4).Column(c =>
                                {
                                    c.Item().Text("Foto Bukti :").Bold();
                                    if (_d.FotoBukti.Count > 0)
                                    {
                                        foreach (var foto in _d.FotoBukti)
                                        {
                                            if (foto != null && foto.Length > 0)
                                            {
                                                c.Item().PaddingTop(4)
                                                    .Width(160).Height(105)
                                                    .Image(foto);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Empty placeholder boxes (3 visible in image)
                                        for (var i = 0; i < 3; i++)
                                        {
                                            c.Item().PaddingTop(4)
                                                .Border(1).BorderColor(Colors.Grey.Lighten2)
                                                .Width(160).Height(80)
                                                .Background(Colors.Grey.Lighten4);
                                        }
                                    }
                                });
                        });
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
    }
}