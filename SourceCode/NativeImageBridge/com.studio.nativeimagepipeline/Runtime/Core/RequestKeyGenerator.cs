using System;
using System.Security.Cryptography;
using System.Text;

namespace Studio.NativeImagePipeline
{
    /// <summary>
    /// Converts puzzle image URLs into deterministic, filesystem-safe RequestKey strings
    /// and resolves file naming conventions for default (256px, 1080px) and custom sizes.
    /// </summary>
    public static class RequestKeyGenerator
    {
        /// <summary>
        /// Generates a unique, deterministic RequestKey string for a given image URL.
        /// Extracts a readable prefix (e.g. filename) combined with a short SHA-256 hash
        /// so keys are human-debuggable, filesystem-safe, and collision-free across 500+ URLs.
        /// </summary>
        public static string FromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("Image URL cannot be null or empty.", nameof(url));

            string trimmed = url.Trim();
            string readablePrefix = ExtractReadablePrefix(trimmed);
            string hashHex = ComputeShortSha256(trimmed, 16);

            return string.IsNullOrEmpty(readablePrefix)
                ? $"img_{hashHex}"
                : $"{readablePrefix}_{hashHex}";
        }

        /// <summary>
        /// Returns the standardized JPEG filename for a given RequestKey and target pixel size.
        /// Example: "puzzle_01_a1b2c3d4e5f60718_256.jpg" or "puzzle_01_a1b2c3d4e5f60718_1080.jpg"
        /// </summary>
        public static string GetFileName(string requestKey, int sizePx)
        {
            if (string.IsNullOrWhiteSpace(requestKey))
                throw new ArgumentException("RequestKey cannot be null or empty.", nameof(requestKey));
            if (sizePx <= 0)
                throw new ArgumentOutOfRangeException(nameof(sizePx), "Resolution sizePx must be positive.");

            return $"{requestKey}_{sizePx}.jpg";
        }

        private static string ExtractReadablePrefix(string url)
        {
            try
            {
                int queryIdx = url.IndexOf('?');
                string cleanUrl = queryIdx >= 0 ? url.Substring(0, queryIdx) : url;

                int lastSlash = cleanUrl.LastIndexOf('/');
                string leaf = lastSlash >= 0 && lastSlash < cleanUrl.Length - 1
                    ? cleanUrl.Substring(lastSlash + 1)
                    : cleanUrl;

                int dotIdx = leaf.LastIndexOf('.');
                if (dotIdx > 0)
                    leaf = leaf.Substring(0, dotIdx);

                var sb = new StringBuilder(Math.Min(leaf.Length, 24));
                foreach (char c in leaf)
                {
                    if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                    {
                        sb.Append(char.ToLowerInvariant(c));
                        if (sb.Length >= 24)
                            break;
                    }
                }

                return sb.ToString().Trim('_', '-');
            }
            catch
            {
                return "img";
            }
        }

        private static string ComputeShortSha256(string input, int hexLength)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(hexLength);
                int byteCount = (hexLength + 1) / 2;
                for (int i = 0; i < byteCount && i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                return sb.ToString(0, Math.Min(hexLength, sb.Length));
            }
        }
    }
}
