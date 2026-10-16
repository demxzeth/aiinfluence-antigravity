using AIInfluenceAntigravity.Hook;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class DispatchTests
    {
        [Fact]
        public void TryExtract_SelectsLongestStringAsPrompt()
        {
            object[] args = new object[] { "npc_bob", "empire", "This is the very long prompt text that represents the dialogue.", 128 };
            Assert.True(Dispatch.TryExtract(args, out string prompt, out int cachePrefix));
            Assert.Equal("This is the very long prompt text that represents the dialogue.", prompt);
            Assert.Equal(128, cachePrefix);
        }

        [Fact]
        public void TryDeriveTextSplit_WithCachePrefix_SplitsProperly()
        {
            const string full = "System context and character traits here.### User says hello!";
            int prefix = "System context and character traits here.".Length;

            Assert.True(Dispatch.TryDeriveTextSplit(full, prefix, out string sys, out string user));
            Assert.Equal("System context and character traits here.", sys);
            Assert.Equal("### User says hello!", user);
        }

        [Fact]
        public void TryDeriveTextSplit_WithMarker_SplitsProperly()
        {
            const string full = "System rules\n### ТЕКУЩАЯ ИСТОРИЯ ДИАЛОГА\nNPC: Hi\nPlayer: Hello";
            Assert.True(Dispatch.TryDeriveTextSplit(full, 0, out string sys, out string user));
            Assert.Equal("System rules", sys);
            Assert.StartsWith("### ТЕКУЩАЯ ИСТОРИЯ ДИАЛОГА", user);
        }
    }
}
