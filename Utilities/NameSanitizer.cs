using System.Linq;
using System.Text;

namespace Vantum.AppKit.Generators.Utilities
{
    /// <summary>
    /// Sanitizes module names to create valid C# identifiers.
    /// </summary>
    internal static class NameSanitizer
    {
        public static string SanitizeIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Unknown";

            var sb = new StringBuilder();

            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else if (c == ' ' || c == '-' || c == '.')
                {
                    sb.Append('_');
                }
                // Skip other characters
            }

            var result = sb.ToString();

            // Ensure it doesn't start with a digit
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            // Ensure it's not empty
            if (string.IsNullOrEmpty(result))
                result = "Unknown";

            return result;
        }
    }
}
