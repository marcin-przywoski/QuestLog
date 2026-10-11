using System;
using QuestLog.GUI.Services;
using Xunit;

namespace QuestLog.Tests
{
    public class MessageIdValidationTests
    {
        [Theory]
        [InlineData("123")]
        [InlineData("0")]
        [InlineData("9876543210")]
        public void DigitsOnly_ReturnsIdUnchanged(string id)
        {
            Assert.Equal(id, AppleScriptOutlookService.ValidateMessageId(id));
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("12a3")]
        [InlineData(" 123")]
        [InlineData("123 ")]
        [InlineData("-1")]
        [InlineData("1; tell application")]
        public void NonDigits_ThrowsArgumentException(string id)
        {
            Assert.Throws<ArgumentException>(() => AppleScriptOutlookService.ValidateMessageId(id));
        }

        [Fact]
        public void Null_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => AppleScriptOutlookService.ValidateMessageId(null!));
        }
    }
}
