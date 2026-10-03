using System;
using System.Security.Cryptography;

namespace VibeShootAdmin.Services
{
    public static class IdGenerator
    {
        private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I to avoid misreads

        /// <summary>Booking transaction id, e.g. VS-20261003-K7Q2M9.</summary>
        public static string TransactionId() => $"VS-{DateTime.Now:yyyyMMdd}-{RandomCode(6)}";

        /// <summary>Official receipt number, e.g. OR-261003-8XK4T2.</summary>
        public static string ReceiptNumber() => $"OR-{DateTime.Now:yyMMdd}-{RandomCode(6)}";

        public static string AccessToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        private static string RandomCode(int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }
            return new string(chars);
        }
    }
}
