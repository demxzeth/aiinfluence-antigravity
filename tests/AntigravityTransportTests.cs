using System.Collections.Generic;
using AIInfluenceAntigravity.Transport;
using Xunit;

namespace AIInfluenceAntigravity.Tests
{
    public sealed class AntigravityTransportTests
    {
        [Fact]
        public void NormalizeModelName_CommonTypos_Corrected()
        {
            Assert.Equal("gemini-3.8-flash-low", AntigravityTransport.NormalizeModelName("gemini-3.8-low"));
            Assert.Equal("gemini-3.8-flash-medium", AntigravityTransport.NormalizeModelName("gemini-3.8-medium"));
            Assert.Equal("gemini-3.8-flash-high", AntigravityTransport.NormalizeModelName("gemini-3.8-high"));
            Assert.Equal("gemini-3.7-flash-low", AntigravityTransport.NormalizeModelName("gemini-3.7-low"));
            Assert.Equal("gemini-3.7-flash-high", AntigravityTransport.NormalizeModelName("gemini-3.7-high"));
            Assert.Equal("gemini-3.6-flash-low", AntigravityTransport.NormalizeModelName("gemini-3.6-low"));
        }

        [Fact]
        public void NormalizeModelName_DisplayNames_Corrected()
        {
            Assert.Equal("gemini-3.8-flash-low", AntigravityTransport.NormalizeModelName("Gemini 3.8 Flash (Low)"));
            Assert.Equal("gemini-3.7-flash-high", AntigravityTransport.NormalizeModelName("Gemini 3.7 Flash (High)"));
            Assert.Equal("claude-sonnet-4-6", AntigravityTransport.NormalizeModelName("Claude Sonnet 4.6 (Thinking)"));
            Assert.Equal("gpt-oss-120b-medium", AntigravityTransport.NormalizeModelName("GPT-OSS 120B (Medium)"));
        }

        [Fact]
        public void NormalizeModelName_ValidOrCustom_Preserved()
        {
            Assert.Equal("gemini-3.8-flash-low", AntigravityTransport.NormalizeModelName("gemini-3.8-flash-low"));
            Assert.Equal("custom-model-id", AntigravityTransport.NormalizeModelName("custom-model-id"));
        }

        [Fact]
        public void ResolveEffort_ValidValues_Mapped()
        {
            Assert.Equal("low", AntigravityTransport.ResolveEffort("Low"));
            Assert.Equal("medium", AntigravityTransport.ResolveEffort("MEDIUM"));
            Assert.Equal("high", AntigravityTransport.ResolveEffort("high"));
            Assert.Equal("xhigh", AntigravityTransport.ResolveEffort("xhigh"));
            Assert.Equal("max", AntigravityTransport.ResolveEffort("max"));
            Assert.Null(AntigravityTransport.ResolveEffort("default"));
            Assert.Null(AntigravityTransport.ResolveEffort(""));
        }

        [Fact]
        public void ParseReply_MarkdownFences_Stripped()
        {
            const string fenced = "```json\n{\"reply\":\"test message\"}\n```";
            string parsed = AntigravityTransport.ParseReply(fenced);
            Assert.Equal("{\"reply\":\"test message\"}", parsed);
        }

        [Fact]
        public void ParseModelsOutput_ParsesModelIds()
        {
            const string raw =
                "Fetching available models...\n" +
                "gemini-3.8-flash-high\tGemini 3.8 Flash (High)\n" +
                "gemini-3.8-flash-low\tGemini 3.8 Flash (Low)\n" +
                "claude-sonnet-4-6\tClaude Sonnet 4.6 (Thinking)\n";

            List<string> models = AntigravityTransport.ParseModelsOutput(raw);
            Assert.Equal(3, models.Count);
            Assert.Contains("gemini-3.8-flash-high", models);
            Assert.Contains("gemini-3.8-flash-low", models);
            Assert.Contains("claude-sonnet-4-6", models);
        }

        [Fact]
        public void EscapeArgument_HandlesSpacesAndQuotes()
        {
            Assert.Equal("simple", AntigravityTransport.EscapeArgument("simple"));
            Assert.Equal("\"with space\"", AntigravityTransport.EscapeArgument("with space"));
            Assert.Equal("\"\\\"quote\\\"\"", AntigravityTransport.EscapeArgument("\"quote\""));
        }
    }
}
