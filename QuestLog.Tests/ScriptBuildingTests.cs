using QuestLog.GUI.Services;
using Xunit;

namespace QuestLog.Tests
{
    /// <summary>
    /// Guards the __TOKEN__ substitution contract: the embedded AppleScript templates
    /// must exist and every placeholder must actually be replaced before the script
    /// reaches osascript.
    /// </summary>
    public class ScriptBuildingTests
    {
        [Fact]
        public void GetEmailsScript_SubstitutesCount_LeavesNoTokens()
        {
            var script = AppleScriptOutlookService.BuildGetEmailsScript(25, unreadOnly: false);

            Assert.DoesNotContain("__COUNT__", script);
            Assert.DoesNotContain("__FILTER_CLAUSE__", script);
            Assert.Contains("25", script);
        }

        [Fact]
        public void GetEmailsScript_UnreadOnly_AddsFilterClause()
        {
            var unfiltered = AppleScriptOutlookService.BuildGetEmailsScript(10, unreadOnly: false);
            var filtered = AppleScriptOutlookService.BuildGetEmailsScript(10, unreadOnly: true);

            Assert.DoesNotContain("whose is read is false", unfiltered);
            Assert.Contains("whose is read is false", filtered);
        }

        [Fact]
        public void GetEmailByIdScript_SubstitutesValidatedId()
        {
            var script = AppleScriptOutlookService.BuildGetEmailByIdScript("12345");

            Assert.DoesNotContain("__MESSAGE_ID__", script);
            Assert.Contains("12345", script);
        }

        [Fact]
        public void MarkAsReadScript_SubstitutesValidatedId()
        {
            var script = AppleScriptOutlookService.BuildMarkAsReadScript("678");

            Assert.DoesNotContain("__MESSAGE_ID__", script);
            Assert.Contains("678", script);
        }

        [Fact]
        public void ScriptBuilders_RejectNonNumericIds()
        {
            Assert.Throws<System.ArgumentException>(
                () => AppleScriptOutlookService.BuildGetEmailByIdScript("1; evil"));
            Assert.Throws<System.ArgumentException>(
                () => AppleScriptOutlookService.BuildMarkAsReadScript("abc"));
        }
    }
}
