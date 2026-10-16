using AIInfluenceAntigravity.Post;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class JsonHealerTests
    {
        [Fact]
        public void Heal_CleanJson_PassesThrough()
        {
            const string json = "{\"reply\":\"hello\",\"score\":10}";
            Assert.Equal(json, JsonHealer.Heal(json));
        }

        [Fact]
        public void Heal_BOMAndLeadingGarbage_Cleaned()
        {
            const string raw = "Here is the response:\n{\"reply\":\"test\"}";
            Assert.Equal("{\"reply\":\"test\"}", JsonHealer.Heal(raw));
        }

        [Fact]
        public void Heal_TrailingComma_Removed()
        {
            const string raw = "{\"reply\":\"test\",}";
            Assert.Equal("{\"reply\":\"test\"}", JsonHealer.Heal(raw));
        }

        [Fact]
        public void Heal_TruncatedJson_ClosedProperly()
        {
            const string raw = "{\"reply\":\"hello world";
            string healed = JsonHealer.Heal(raw);
            Assert.NotNull(healed);
            Assert.True(JsonHealer.TryParse(healed, out var obj));
            Assert.Equal("hello world", (string)obj["reply"]);
        }

        [Fact]
        public void Heal_DanglingKey_Removed()
        {
            const string raw = "{\"reply\":\"ok\",\"dangling\":";
            string healed = JsonHealer.Heal(raw);
            Assert.NotNull(healed);
            Assert.True(JsonHealer.TryParse(healed, out var obj));
            Assert.Equal("ok", (string)obj["reply"]);
        }
    }
}
