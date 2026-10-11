using System;
using QuestLog.GUI.Services;
using Xunit;

namespace QuestLog.Tests
{
    public class ParseDateTests
    {
        [Theory]
        [InlineData("2026-10-12 10:30:00", 2026, 10, 12, 10, 30, 0)]
        [InlineData("2026-01-01 00:00:00", 2026, 1, 1, 0, 0, 0)]
        public void IsoFormat_ParsesCorrectly(
            string input, int year, int month, int day, int hour, int minute, int second)
        {
            Assert.Equal(
                new DateTime(year, month, day, hour, minute, second),
                AppleScriptOutlookService.ParseDate(input));
        }

        [Fact]
        public void ExplicitUsFormat_ParsesViaFormatFallback()
        {
            // Matches the "M/d/yyyy h:mm:ss tt" fallback format; unambiguous day > 12
            // so culture parsing cannot silently reorder fields.
            var result = AppleScriptOutlookService.ParseDate("10/25/2026 3:45:30 PM");

            Assert.Equal(new DateTime(2026, 10, 25, 15, 45, 30), result);
        }

        [Theory]
        [InlineData("not a date at all")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("99/99/9999 99:99")]
        public void UnparseableInput_ReturnsMinValue_NeverThrows(string input)
        {
            Assert.Equal(DateTime.MinValue, AppleScriptOutlookService.ParseDate(input));
        }

        [Fact]
        public void SurroundingWhitespace_DoesNotBreakParsing()
        {
            Assert.Equal(
                new DateTime(2026, 10, 12, 10, 30, 0),
                AppleScriptOutlookService.ParseDate("  2026-10-12 10:30:00  "));
        }
    }
}
