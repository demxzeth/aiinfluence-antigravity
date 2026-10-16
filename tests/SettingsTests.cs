using AIInfluenceAntigravity.Config;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class SettingsTests
    {
        [Fact]
        public void Defaults_AreValid()
        {
            var s = new AntigravitySettings();
            Assert.True(s.Enabled);
            Assert.True(s.TimerInChat);
            Assert.Equal("gemini-3.8-flash-low", s.Model);
            Assert.Equal("Default", s.ReasoningEffort);
            Assert.Equal(360, s.TimeoutSeconds);
            Assert.Equal(1.0f, s.Temperature);
            Assert.Equal(0, s.MaxTokens);
            Assert.True(s.LogEnabled);
            Assert.True(s.JsonRepair);
            Assert.True(s.ErrorNotices);
        }

        [Fact]
        public void Clone_CopiesAllProperties()
        {
            var original = new AntigravitySettings
            {
                Enabled = false,
                TimerInChat = false,
                CliPath = "C:\\test\\agy.exe",
                Model = "gemini-3.8-flash-low",
                ReasoningEffort = "High",
                TimeoutSeconds = 120,
                Temperature = 0.5f,
                MaxTokens = 2048,
                LogEnabled = false,
                JsonRepair = false,
                ErrorNotices = false
            };
            original.AvailableModels.Add("m1");

            var clone = original.Clone();
            Assert.False(clone.Enabled);
            Assert.False(clone.TimerInChat);
            Assert.Equal("C:\\test\\agy.exe", clone.CliPath);
            Assert.Equal("gemini-3.8-flash-low", clone.Model);
            Assert.Equal("High", clone.ReasoningEffort);
            Assert.Equal(120, clone.TimeoutSeconds);
            Assert.Equal(0.5f, clone.Temperature);
            Assert.Equal(2048, clone.MaxTokens);
            Assert.Single(clone.AvailableModels);
            Assert.Equal("m1", clone.AvailableModels[0]);
        }
    }
}
