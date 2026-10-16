using AIInfluenceAntigravity.Infra;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class ChatNoticesTests
    {
        [Fact]
        public void Show_DoesNotThrow()
        {
            ChatNotices.Show("test message");
        }

        [Fact]
        public void Error_DoesNotThrow()
        {
            ChatNotices.Error("error test");
        }

        [Fact]
        public void ShowTimer_DoesNotThrow()
        {
            ChatNotices.ShowTimer(1234);
        }

        [Fact]
        public void FormatTimer_Milliseconds_ReturnsMs()
        {
            Assert.Equal("999 ms", ChatNotices.FormatTimer(999));
            Assert.Equal("0 ms", ChatNotices.FormatTimer(0));
            Assert.Equal("1 ms", ChatNotices.FormatTimer(1));
        }

        [Fact]
        public void FormatTimer_Seconds_ReturnsSecondsWithFormat()
        {
            Assert.Equal("156s", ChatNotices.FormatTimer(156000));
            Assert.Equal("1.2s", ChatNotices.FormatTimer(1234));
            Assert.Equal("1s", ChatNotices.FormatTimer(1000));
            Assert.Equal("12.3s", ChatNotices.FormatTimer(12345));
        }

        [Fact]
        public void Colors_VioletConstants_AreAccurate()
        {
            Assert.Equal(0.68f, ChatNotices.VioletR);
            Assert.Equal(0.45f, ChatNotices.VioletG);
            Assert.Equal(0.98f, ChatNotices.VioletB);
            Assert.Equal("[Antigravity] ", ChatNotices.Prefix);
        }
    }
}
