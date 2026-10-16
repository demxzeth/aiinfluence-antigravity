using System.Reflection;
using System.Threading.Tasks;
using AIInfluenceAntigravity.Hook;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class SignatureRulesTests
    {
        private class DummyClient
        {
            public static Task<string> GetAIResponse(string npc, string faction, string prompt) => Task.FromResult("");
            public static Task<string> GetRawTextResponse(string prompt) => Task.FromResult("");
            public static Task<string> GetRawTextResponseWithBackend(string prompt, string backend) => Task.FromResult("");
            public static Task<string> GetCustomBackendResponse(string prompt) => Task.FromResult("");
            public static void NotATarget() { }
        }

        [Fact]
        public void Classify_Dialogue()
        {
            MethodInfo m = typeof(DummyClient).GetMethod(nameof(DummyClient.GetAIResponse));
            Assert.Equal(TargetRole.Dialogue, SignatureRules.Classify(m));
        }

        [Fact]
        public void Classify_Raw()
        {
            MethodInfo m = typeof(DummyClient).GetMethod(nameof(DummyClient.GetRawTextResponse));
            Assert.Equal(TargetRole.Raw, SignatureRules.Classify(m));
        }

        [Fact]
        public void Classify_RawWithBackend()
        {
            MethodInfo m = typeof(DummyClient).GetMethod(nameof(DummyClient.GetRawTextResponseWithBackend));
            Assert.Equal(TargetRole.RawWithBackend, SignatureRules.Classify(m));
        }

        [Fact]
        public void Classify_BackendDirect()
        {
            MethodInfo m = typeof(DummyClient).GetMethod(nameof(DummyClient.GetCustomBackendResponse));
            Assert.Equal(TargetRole.BackendDirect, SignatureRules.Classify(m));
        }

        [Fact]
        public void Classify_NotATarget()
        {
            MethodInfo m = typeof(DummyClient).GetMethod(nameof(DummyClient.NotATarget));
            Assert.Equal(TargetRole.None, SignatureRules.Classify(m));
        }
    }
}
