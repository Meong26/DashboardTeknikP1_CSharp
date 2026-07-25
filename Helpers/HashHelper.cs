using System;
using System.Security.Cryptography;
using System.Text;

namespace DashboardTeknikP1.Helpers
{
    public static class HashHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100000;

        /// <summary>
        /// Menghasilkan hash PBKDF2 ber-salt dengan standar industri (SHA256, 100.000 iterasi).
        /// Format: PBKDF2$100000$Base64Salt$Base64Hash
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return "";

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
        }

        /// <summary>
        /// Memverifikasi password terhadap hash tersimpan (mendukung PBKDF2 dan legacy SHA-256).
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword)) return false;

            // 1. Verifikasi Format Baru: PBKDF2
            if (hashedPassword.StartsWith("PBKDF2$"))
            {
                var parts = hashedPassword.Split('$');
                if (parts.Length != 4) return false;

                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedKey = Convert.FromBase64String(parts[3]);

                byte[] actualKey = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, expectedKey.Length);
                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }

            // 2. Verifikasi Format Lama: SHA-256 (Legacy Unsalted)
            string legacyHash = ComputeSha256Hash(password);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(legacyHash.ToLowerInvariant()),
                Encoding.UTF8.GetBytes(hashedPassword.ToLowerInvariant())
            );
        }

        /// <summary>
        /// Mengecek apakah hash yang tersimpan masih menggunakan format legacy SHA-256 (perlu di-upgrade).
        /// </summary>
        public static bool IsLegacyHash(string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword)) return false;
            return !hashedPassword.StartsWith("PBKDF2$");
        }

        /// <summary>
        /// Method legacy SHA-256 (tetap dipertahankan untuk backward compatibility).
        /// </summary>
        public static string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return "";

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
