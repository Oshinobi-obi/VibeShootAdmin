using System;
using System.Globalization;
using Microsoft.AspNetCore.Html;

namespace VibeShootAdmin.Models
{
    /// <summary>Small formatting helpers shared by the views.</summary>
    public static class Ui
    {
        private static readonly CultureInfo PH = CultureInfo.GetCultureInfo("en-PH");

        public static string Peso(decimal amount) => "₱" + amount.ToString("N2", PH);

        public static string PesoShort(decimal amount) =>
            amount >= 1_000_000 ? "₱" + (amount / 1_000_000m).ToString("0.#", PH) + "M"
            : amount >= 10_000 ? "₱" + (amount / 1000m).ToString("0.#", PH) + "k"
            : "₱" + amount.ToString("N0", PH);

        public static HtmlString Pill(string? status) =>
            new HtmlString($"<span class=\"pill pill-{Slug(status)}\">{System.Net.WebUtility.HtmlEncode(status)}</span>");

        public static string Slug(string? value) => (value ?? "").Trim().ToLowerInvariant().Replace(' ', '-');

        /// <summary>Converts a UTC timestamp to Philippine time for display.</summary>
        public static DateTime Local(DateTime utc)
        {
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Singapore Standard Time" : "Asia/Manila");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
            }
            catch (TimeZoneNotFoundException)
            {
                return utc.ToLocalTime();
            }
        }

        public static string Stamp(DateTime utc) => Local(utc).ToString("MMM d, yyyy · h:mm tt");
    }
}
