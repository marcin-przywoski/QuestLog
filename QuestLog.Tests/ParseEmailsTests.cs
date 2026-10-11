using System;
using System.Linq;
using QuestLog.GUI.Services;
using Xunit;

namespace QuestLog.Tests
{
    public class ParseEmailsTests
    {
        private static string Record(
            string id = "123",
            string subject = "Hello",
            string sender = "Jane Doe",
            string senderEmail = "jane@example.com",
            string date = "2026-10-12 10:30:00",
            string isRead = "false",
            string body = "body text")
        {
            return string.Join("||", id, subject, sender, senderEmail, date, isRead, body);
        }

        [Fact]
        public void EmptyOutput_ReturnsEmptyList()
        {
            Assert.Empty(AppleScriptOutlookService.ParseEmails(string.Empty));
            Assert.Empty(AppleScriptOutlookService.ParseEmails("   \n  "));
        }

        [Fact]
        public void SingleRecord_ParsesAllFields()
        {
            var emails = AppleScriptOutlookService.ParseEmails(Record()).ToList();

            var email = Assert.Single(emails);
            Assert.Equal("123", email.Id);
            Assert.Equal("Hello", email.Subject);
            Assert.Equal("Jane Doe", email.Sender);
            Assert.Equal("jane@example.com", email.SenderEmail);
            Assert.Equal(new DateTime(2026, 10, 12, 10, 30, 0), email.ReceivedDate);
            Assert.False(email.IsRead);
            Assert.Equal("body text", email.Body);
            Assert.Equal("Inbox", email.Folder);
        }

        [Fact]
        public void MultipleRecords_SeparatedByRecordMarker_ParseAll()
        {
            var payload = Record(id: "1") + "<<EMAIL>>" + Record(id: "2") + "<<EMAIL>>" + Record(id: "3");

            var ids = AppleScriptOutlookService.ParseEmails(payload).Select(e => e.Id).ToList();

            Assert.Equal(new[] { "1", "2", "3" }, ids);
        }

        [Fact]
        public void BodyContainingFieldSeparator_IsPreservedBySplitCap()
        {
            var body = "first || second || third";

            var email = Assert.Single(AppleScriptOutlookService.ParseEmails(Record(body: body)));

            Assert.Equal(body, email.Body);
        }

        [Fact]
        public void MalformedRecord_FewerThanSevenFields_IsSkipped()
        {
            var malformed = "42||Subject||Sender";
            var payload = malformed + "<<EMAIL>>" + Record(id: "7");

            var email = Assert.Single(AppleScriptOutlookService.ParseEmails(payload));

            Assert.Equal("7", email.Id);
        }

        [Fact]
        public void Fields_WithSurroundingWhitespace_AreTrimmed()
        {
            var email = Assert.Single(
                AppleScriptOutlookService.ParseEmails(Record(subject: "  padded  ")));

            Assert.Equal("padded", email.Subject);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("garbage", false)]
        public void IsRead_ParsesCaseInsensitively(string isRead, bool expected)
        {
            var email = Assert.Single(AppleScriptOutlookService.ParseEmails(Record(isRead: isRead)));

            Assert.Equal(expected, email.IsRead);
        }

        [Fact]
        public void UnparseableDate_YieldsMinValue_NotException()
        {
            var email = Assert.Single(
                AppleScriptOutlookService.ParseEmails(Record(date: "not-a-date")));

            Assert.Equal(DateTime.MinValue, email.ReceivedDate);
        }
    }
}
