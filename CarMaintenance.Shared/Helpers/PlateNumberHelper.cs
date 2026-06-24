using System.Text;
using System.Text.RegularExpressions;

namespace CarMaintenance.Shared.Helpers
{
    public static class PlateNumberHelper
    {
        private static readonly Regex EgyptianPlatePattern =
            new(@"^[\u0621-\u064A]{1,3}\s\d{1,4}$", RegexOptions.Compiled);

        private static readonly Regex MultipleSpaces =
            new(@"\s+", RegexOptions.Compiled);

        public static string CleanAndNormalize(string? rawPlate)
        {
            if (string.IsNullOrWhiteSpace(rawPlate))
                return string.Empty;

            var result = ConvertToEnglishDigits(rawPlate.Trim());

            result = MultipleSpaces.Replace(result, " ");

            result = result.ToUpperInvariant();

            return result;
        }

        public static bool IsValidEgyptianPlate(string cleanedPlate)
            => EgyptianPlatePattern.IsMatch(cleanedPlate);

        private static string ConvertToEnglishDigits(string input)
        {
            var sb = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                if (ch >= '٠' && ch <= '٩')
                    sb.Append((char)('0' + (ch - '٠')));
                else
                    sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}