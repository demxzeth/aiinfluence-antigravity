using System;
using System.Collections.Generic;

namespace AIInfluenceAntigravity.Config
{
    public sealed class AntigravitySettings
    {
        public bool Enabled { get; set; } = true;
        public bool TimerInChat { get; set; } = true;
        public string CliPath { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.8-flash-low";
        public List<string> AvailableModels { get; set; } = new List<string>();
        public string ReasoningEffort { get; set; } = "Default";
        public int TimeoutSeconds { get; set; } = 360;
        public float Temperature { get; set; } = 1.0f;
        public int MaxTokens { get; set; } = 0;
        public bool LogEnabled { get; set; } = true;
        public bool JsonRepair { get; set; } = true;
        public bool ErrorNotices { get; set; } = true;

        public AntigravitySettings Clone()
        {
            return new AntigravitySettings
            {
                Enabled = this.Enabled,
                TimerInChat = this.TimerInChat,
                CliPath = this.CliPath,
                Model = this.Model,
                AvailableModels = new List<string>(this.AvailableModels ?? new List<string>()),
                ReasoningEffort = this.ReasoningEffort,
                TimeoutSeconds = this.TimeoutSeconds,
                Temperature = this.Temperature,
                MaxTokens = this.MaxTokens,
                LogEnabled = this.LogEnabled,
                JsonRepair = this.JsonRepair,
                ErrorNotices = this.ErrorNotices
            };
        }
    }
}
