using System;
using System.Globalization;
using Avalonia.Media;
using QuestLog.GUI.Converters;
using Xunit;

namespace QuestLog.Tests
{
    public class BoolToFontWeightConverterTests
    {
        private readonly BoolToFontWeightConverter _converter = new();

        [Fact]
        public void Convert_Unread_ReturnsBold()
        {
            var result = _converter.Convert(false, typeof(FontWeight), null, CultureInfo.InvariantCulture);

            Assert.Equal(FontWeight.Bold, result);
        }

        [Fact]
        public void Convert_Read_ReturnsNormal()
        {
            var result = _converter.Convert(true, typeof(FontWeight), null, CultureInfo.InvariantCulture);

            Assert.Equal(FontWeight.Normal, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("true")]
        [InlineData(1)]
        public void Convert_NonBool_ReturnsNormal(object? value)
        {
            var result = _converter.Convert(value, typeof(FontWeight), null, CultureInfo.InvariantCulture);

            Assert.Equal(FontWeight.Normal, result);
        }

        [Fact]
        public void ConvertBack_IsNotSupported()
        {
            Assert.Throws<NotImplementedException>(
                () => _converter.ConvertBack(FontWeight.Bold, typeof(bool), null, CultureInfo.InvariantCulture));
        }
    }
}
