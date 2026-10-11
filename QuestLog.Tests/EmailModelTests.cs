using System.ComponentModel;
using QuestLog.GUI.Models;
using Xunit;

namespace QuestLog.Tests
{
    public class EmailModelTests
    {
        [Fact]
        public void IsRead_Change_RaisesPropertyChanged()
        {
            var email = new Email();
            string? raised = null;
            email.PropertyChanged += (_, e) => raised = e.PropertyName;

            email.IsRead = true;

            Assert.Equal(nameof(Email.IsRead), raised);
        }

        [Fact]
        public void IsRead_SameValue_DoesNotRaiseAgain()
        {
            var email = new Email { IsRead = true };
            var count = 0;
            email.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(Email.IsRead))
                {
                    count++;
                }
            };

            email.IsRead = true;

            Assert.Equal(0, count);
        }
    }
}
