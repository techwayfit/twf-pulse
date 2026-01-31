using System.Text.RegularExpressions;

namespace TechWayFit.Pulse.AI.Utilities
{
    /// <summary>
    /// Sanitizes text by removing Personally Identifiable Information (PII) before sending to AI services.
    /// </summary>
    public static class PiiSanitizer
    {
        // Email pattern
        private static readonly Regex EmailRegex = new(
            @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Phone number patterns (various formats)
        private static readonly Regex PhoneRegex = new(
            @"\b(?:\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})\b",
            RegexOptions.Compiled);

        // SSN pattern (XXX-XX-XXXX)
        private static readonly Regex SsnRegex = new(
            @"\b\d{3}-\d{2}-\d{4}\b",
            RegexOptions.Compiled);

        // Credit card pattern (simple 13-19 digit detection)
        private static readonly Regex CreditCardRegex = new(
            @"\b\d{13,19}\b",
            RegexOptions.Compiled);

        // IP Address pattern
        private static readonly Regex IpAddressRegex = new(
            @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b",
            RegexOptions.Compiled);

        /// <summary>
        /// Sanitizes text by removing common PII patterns.
        /// </summary>
        /// <param name="text">The text to sanitize</param>
        /// <param name="maxLength">Optional maximum length to truncate to (default: 500)</param>
        /// <returns>Sanitized text with PII removed</returns>
        public static string Sanitize(string? text, int maxLength = 500)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var sanitized = text;

            // Remove email addresses
            sanitized = EmailRegex.Replace(sanitized, "[EMAIL_REMOVED]");

            // Remove phone numbers
            sanitized = PhoneRegex.Replace(sanitized, "[PHONE_REMOVED]");

            // Remove SSNs
            sanitized = SsnRegex.Replace(sanitized, "[SSN_REMOVED]");

            // Remove credit card numbers
            sanitized = CreditCardRegex.Replace(sanitized, "[CARD_REMOVED]");

            // Remove IP addresses
            sanitized = IpAddressRegex.Replace(sanitized, "[IP_REMOVED]");

            // Truncate if needed
            if (sanitized.Length > maxLength)
            {
                sanitized = sanitized.Substring(0, maxLength) + "... [TRUNCATED]";
            }

            return sanitized;
        }

        /// <summary>
        /// Checks if text appears to contain PII.
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if potential PII is detected</returns>
        public static bool ContainsPii(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return EmailRegex.IsMatch(text) ||
                   PhoneRegex.IsMatch(text) ||
                   SsnRegex.IsMatch(text) ||
                   CreditCardRegex.IsMatch(text);
        }

        /// <summary>
        /// Sanitizes a summary ensuring it's within length limits for AI context.
        /// </summary>
        /// <param name="text">The text to sanitize</param>
        /// <param name="maxLength">Maximum allowed length (default: 500 as per design)</param>
        /// <returns>Sanitized and length-limited summary</returns>
        public static string SanitizeSummary(string? text, int maxLength = 500)
        {
            return Sanitize(text, maxLength);
        }
    }
}
