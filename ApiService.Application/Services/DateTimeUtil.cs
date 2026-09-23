using System;

namespace ApiService.Application.Services
{
    /// <summary>
    /// BulanTahun boundary = UTC ISO string.
    /// FE kirim misal "2026-01-31T17:00:00.000Z" (= 2026-02-01 00:00 WIB / +07:00).
    /// Konvensi storage: naive UTC "YYYY-MM-01 00:00" (first day bulan).
    /// Output GET: selalu string "yyyy-MM-01T00:00:00.000Z" (dengan Z, bukan timestamp biasa).
    /// Tidak pakai ParseExact / Split / Substring - manual.
    /// </summary>
    public sealed class DateTimeUtil
    {
        private static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>
        /// Parse ISO UTC (contoh "2026-01-31T17:00:00.000Z") -> naive DateTime first-day bulan
        /// berdasarkan zona bisnis WIB (+07:00). "Februari 2026" (dipilih FE) => 2026-02-01 00:00.
        /// </summary>
        public static DateTime? ParseBulanTahunUtc(string text)
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

            var year = Digit(cleaned[0]) * 1000 + Digit(cleaned[1]) * 100 + Digit(cleaned[2]) * 10 + Digit(cleaned[3]);
            var month = Digit(cleaned[5]) * 10 + Digit(cleaned[6]);
            var day = Digit(cleaned[8]) * 10 + Digit(cleaned[9]);

            if (year < 1900 || year > 9999 || month < 1 || month > 12 || day < 1)
                return null;

            if (day > MaxDayOf(month, year))
                return null;

            // UTC hour di posisi 11-12 kalau ada 'T' atau spasi
            var hour = 0;
            if (cleaned.Length > 11)
            {
                var c10 = cleaned[10];
                if ((c10 == 'T' || c10 == ' ') && cleaned[11] >= '0' && cleaned[11] <= '9')
                {
                    hour = Digit(cleaned[11]) * 10;
                    if (cleaned.Length > 12 && cleaned[12] >= '0' && cleaned[12] <= '9')
                        hour += Digit(cleaned[12]);
                }
            }

            // Shift +07:00 WIB -> tentukan bulan bisnis
            var y = year;
            var m = month;
            var d = day;

            if (hour + 7 >= 24)
            {
                var nd = d + 1;
                if (nd > MaxDayOf(m, y))
                {
                    nd = 1;
                    m += 1;
                    if (m > 12)
                    {
                        m = 1;
                        y += 1;
                    }
                }
                d = nd;
            }

            return new DateTime(y, m, 1);
        }

        /// <summary>Output GET: "yyyy-MM-01T00:00:00.000Z" - selalu UTC, selalu Z.</summary>
        public static string ToUtcIsoMonthYear(DateTime value) =>
            $"{value:yyyy-MM-dd}T00:00:00.000Z";

        /// <summary>
        /// Parse tanggal dari ISO string (contoh "2026-09-23", "2026-09-23T14:30:00.000Z")
        /// -> naive DateTime jam 00:00 (date only). Tidak memakai ParseExact.
        /// </summary>
        public static DateTime? ParseDateOnly(string text)
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

            var year = Digit(cleaned[0]) * 1000 + Digit(cleaned[1]) * 100 + Digit(cleaned[2]) * 10 + Digit(cleaned[3]);
            var month = Digit(cleaned[5]) * 10 + Digit(cleaned[6]);
            var day = Digit(cleaned[8]) * 10 + Digit(cleaned[9]);

            if (year < 1900 || year > 9999 || month < 1 || month > 12 || day < 1)
                return null;

            if (day > MaxDayOf(month, year))
                return null;

            return new DateTime(year, month, day);
        }

        private static int MaxDayOf(int month, int year)
        {
            var max = DaysInMonth[month - 1];
            if (month == 2 && IsLeapYear(year))
                max = 29;
            return max;
        }

        private static bool IsLeapYear(int year) =>
            (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);

        private static int Digit(char c)
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
    }
}