using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AIInfluenceAntigravity.Config;
using AIInfluenceAntigravity.Infra;
using AIInfluenceAntigravity.Post;

namespace AIInfluenceAntigravity.Transport
{
    public sealed class ProcessResult
    {
        public string Output { get; set; }
        public string Error { get; set; }
        public int ExitCode { get; set; }
    }

    public sealed class TransportException : Exception
    {
        public int StatusCode { get; }

        public TransportException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public TransportException(int statusCode, string message, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }

    public static class AntigravityTransport
    {
        public static readonly string[] FallbackAntigravityModels = new[]
        {
            "gemini-3.8-flash-high",
            "gemini-3.8-flash-medium",
            "gemini-3.8-flash-low",
            "gemini-3.7-flash-high",
            "gemini-3.7-flash-medium",
            "gemini-3.7-flash-low",
            "gemini-3.6-flash-high",
            "gemini-3.6-flash-medium",
            "gemini-3.6-flash-low",
            "gemini-3.1-pro-high",
            "gemini-3.1-pro-low",
            "claude-sonnet-4-6",
            "claude-opus-4-6-thinking",
            "gpt-oss-120b-medium"
        };

        public static string ParseReply(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody)) return rawBody;
            string trimmed = rawBody.Trim();

            // Strip markdown code fences (e.g. ```json ... ``` or ``` ... ```)
            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                int firstNewline = trimmed.IndexOf('\n');
                if (firstNewline > 0)
                {
                    int lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
                    if (lastFence > firstNewline)
                    {
                        trimmed = trimmed.Substring(firstNewline + 1, lastFence - firstNewline - 1).Trim();
                    }
                }
            }

            // Heal JSON if needed and repair is enabled
            if (SettingsVault.Current.JsonRepair && trimmed.Contains("{"))
            {
                string healed = JsonHealer.Heal(trimmed);
                if (!string.IsNullOrEmpty(healed))
                {
                    return healed;
                }
            }

            return trimmed;
        }

        public static async Task<string> SendAsync(
            ChatTurn turn, ModelOptions options, AntigravitySettings settings, CancellationToken cancellationToken)
        {
            string cli = ResolveCliPath(settings.CliPath);
            string model = ResolveModel(options?.Model, settings.Model);
            int timeoutSeconds = settings.TimeoutSeconds >= 10 ? settings.TimeoutSeconds : 360;
            string effort = ResolveEffort(settings.ReasoningEffort);

            // Two-stage request: built-in whenever turn has split content
            if (turn != null && turn.HasSplit)
            {
                return await SendTwoStageAsync(
                    turn, options, settings, cli, model, effort, timeoutSeconds, cancellationToken)
                    .ConfigureAwait(false);
            }

            return await SendSingleStageAsync(
                turn, options, settings, cli, model, effort, timeoutSeconds, cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<string> SendTwoStageAsync(
            ChatTurn turn, ModelOptions options, AntigravitySettings settings,
            string cli, string model, string effort, int timeoutSeconds, CancellationToken cancellationToken)
        {
            LogSink.Info("antigravity: two-stage request starting (stage 1 context="
                + turn.SplitSystem.Length + " chars, stage 2 dialogue=" + turn.SplitUser.Length + " chars)");

            // Stage 1: Send static context, world info, rules and character data.
            string stage1Prompt = "[System instruction: The following is background context, character data, game state, and rules for a video game dialogue scene. Read and memorize all information. Acknowledge with a brief confirmation; do not generate in-game dialogue yet.]\n\n"
                + turn.SplitSystem;

            StringBuilder sb1 = new StringBuilder();
            sb1.Append("--input-format text");
            if (!string.IsNullOrWhiteSpace(model))
            {
                sb1.Append(" --model ");
                sb1.Append(EscapeArgument(model));
            }
            if (!string.IsNullOrEmpty(effort))
            {
                sb1.Append(" --effort ");
                sb1.Append(effort);
            }
            sb1.Append(" --disable-slash-commands");

            ProcessResult res1 = await RunProcessAsync(cli, sb1.ToString(), timeoutSeconds, cancellationToken, stage1Prompt)
                .ConfigureAwait(false);

            if (res1.ExitCode != 0)
            {
                string errMsg = !string.IsNullOrWhiteSpace(res1.Error) ? res1.Error : res1.Output;
                if (string.IsNullOrWhiteSpace(errMsg))
                {
                    errMsg = "agy.exe exited with code " + res1.ExitCode;
                }
                LogSink.Warn("antigravity stage 1 error: " + errMsg);
                throw new TransportException(res1.ExitCode, "Stage 1 failed: " + errMsg);
            }

            LogSink.Info("antigravity: stage 1 completed (" + (res1.Output ?? string.Empty).Length + " chars reply), starting stage 2 (--continue)");

            // Stage 2: Send dialogue history and prompt for NPC response using --continue.
            string stage2Prompt = "[System instruction: Now generate the character's in-game response to the latest message in the dialogue history below, following all formatting rules and constraints provided earlier. Do not invoke any tools or external commands.]\n\n"
                + turn.SplitUser;

            StringBuilder sb2 = new StringBuilder();
            sb2.Append("--continue --input-format text");
            if (!string.IsNullOrWhiteSpace(model))
            {
                sb2.Append(" --model ");
                sb2.Append(EscapeArgument(model));
            }
            if (!string.IsNullOrEmpty(effort))
            {
                sb2.Append(" --effort ");
                sb2.Append(effort);
            }
            sb2.Append(" --disable-slash-commands");

            ProcessResult res2 = await RunProcessAsync(cli, sb2.ToString(), timeoutSeconds, cancellationToken, stage2Prompt)
                .ConfigureAwait(false);

            if (res2.ExitCode != 0)
            {
                string errMsg = !string.IsNullOrWhiteSpace(res2.Error) ? res2.Error : res2.Output;
                if (string.IsNullOrWhiteSpace(errMsg))
                {
                    errMsg = "agy.exe exited with code " + res2.ExitCode;
                }
                LogSink.Warn("antigravity stage 2 error: " + errMsg);
                throw new TransportException(res2.ExitCode, "Stage 2 failed: " + errMsg);
            }

            if (string.IsNullOrWhiteSpace(res2.Output))
            {
                if (!string.IsNullOrWhiteSpace(res2.Error))
                {
                    throw new TransportException(res2.ExitCode, res2.Error.Trim());
                }
                return string.Empty;
            }

            return ParseReply(res2.Output);
        }

        private static async Task<string> SendSingleStageAsync(
            ChatTurn turn, ModelOptions options, AntigravitySettings settings,
            string cli, string model, string effort, int timeoutSeconds, CancellationToken cancellationToken)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("--input-format text");

            if (!string.IsNullOrWhiteSpace(model))
            {
                sb.Append(" --model ");
                sb.Append(EscapeArgument(model));
            }

            if (!string.IsNullOrEmpty(effort))
            {
                sb.Append(" --effort ");
                sb.Append(effort);
            }

            sb.Append(" --disable-slash-commands");

            string promptToSend = turn?.Prompt ?? string.Empty;
            ProcessResult res = await RunProcessAsync(cli, sb.ToString(), timeoutSeconds, cancellationToken, promptToSend)
                .ConfigureAwait(false);

            if (res.ExitCode != 0)
            {
                string errMsg = !string.IsNullOrWhiteSpace(res.Error) ? res.Error : res.Output;
                if (string.IsNullOrWhiteSpace(errMsg))
                {
                    errMsg = "agy.exe exited with code " + res.ExitCode;
                }
                LogSink.Warn("antigravity error: " + errMsg);
                throw new TransportException(res.ExitCode, errMsg);
            }

            if (string.IsNullOrWhiteSpace(res.Output))
            {
                if (!string.IsNullOrWhiteSpace(res.Error))
                {
                    throw new TransportException(res.ExitCode, res.Error.Trim());
                }
                return string.Empty;
            }

            return ParseReply(res.Output);
        }

        public static async Task<ProcessResult> RunProcessAsync(
            string executable, string arguments, int timeoutSeconds, CancellationToken cancellationToken, string stdinInput = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = stdinInput != null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (var process = new Process { StartInfo = psi })
            {
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        if (stdout.Length > 0) stdout.AppendLine();
                        stdout.Append(e.Data);
                    }
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        if (stderr.Length > 0) stderr.AppendLine();
                        stderr.Append(e.Data);
                    }
                };

                try
                {
                    if (!process.Start())
                    {
                        throw new InvalidOperationException("Failed to start process: " + executable);
                    }
                }
                catch (Exception ex)
                {
                    throw new TransportException(-1, "Cannot start agy.exe: " + ex.Message, ex);
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (stdinInput != null)
                {
                    try
                    {
                        using (var writer = new StreamWriter(process.StandardInput.BaseStream, new UTF8Encoding(false), 8192, leaveOpen: false))
                        {
                            await writer.WriteAsync(stdinInput).ConfigureAwait(false);
                            await writer.FlushAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogSink.Warn("antigravity stdin write warning: " + ex.Message);
                    }
                }

                var tcs = new TaskCompletionSource<bool>();
                process.EnableRaisingEvents = true;
                process.Exited += (s, e) => tcs.TrySetResult(true);

                if (process.HasExited)
                {
                    tcs.TrySetResult(true);
                }

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));

                    using (linkedCts.Token.Register(() =>
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                        catch { }
                        tcs.TrySetCanceled();
                    }))
                    {
                        await tcs.Task.ConfigureAwait(false);
                    }
                }

                process.WaitForExit(1000);

                return new ProcessResult
                {
                    Output = stdout.ToString(),
                    Error = stderr.ToString(),
                    ExitCode = process.ExitCode
                };
            }
        }

        public static string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            if (!arg.Contains(" ") && !arg.Contains("\"") && !arg.Contains("\t"))
            {
                return arg;
            }
            return "\"" + arg.Replace("\"", "\\\"") + "\"";
        }

        public static string ResolveCliPath(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string trimmed = configured.Trim().Trim('"', '\'').Trim();
                if (File.Exists(trimmed))
                {
                    return trimmed;
                }
                if (!trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(trimmed + ".exe"))
                {
                    return trimmed + ".exe";
                }

                if (trimmed.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
                    trimmed.IndexOf(Path.AltDirectorySeparatorChar) >= 0)
                {
                    return trimmed;
                }
            }

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                string candidate = Path.Combine(localAppData, "agy", "bin", "agy.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            string pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string part in pathEnv.Split(Path.PathSeparator))
                {
                    try
                    {
                        string trimmedPart = part.Trim().Trim('"', '\'').Trim();
                        if (trimmedPart.Length > 0)
                        {
                            string candidate = Path.Combine(trimmedPart, "agy.exe");
                            if (File.Exists(candidate))
                            {
                                return candidate;
                            }
                        }
                    }
                    catch { }
                }
            }

            return !string.IsNullOrWhiteSpace(configured) ? configured.Trim().Trim('"', '\'').Trim() : "agy";
        }

        public static string ResolveModel(string scopeModel, string defaultModel)
        {
            if (!string.IsNullOrWhiteSpace(scopeModel))
            {
                return NormalizeModelName(scopeModel);
            }
            if (!string.IsNullOrWhiteSpace(defaultModel))
            {
                return NormalizeModelName(defaultModel);
            }
            return "gemini-3.8-flash-low";
        }

        public static string NormalizeModelName(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) return "gemini-3.8-flash-low";
            string trimmed = model.Trim().Trim('"', '\'').Trim();

            switch (trimmed.ToLowerInvariant())
            {
                case "gemini-3.8-low":
                case "gemini 3.8 flash (low)":
                case "gemini 3.8 flash low":
                    return "gemini-3.8-flash-low";

                case "gemini-3.8-medium":
                case "gemini 3.8 flash (medium)":
                case "gemini 3.8 flash medium":
                    return "gemini-3.8-flash-medium";

                case "gemini-3.8-high":
                case "gemini 3.8 flash (high)":
                case "gemini 3.8 flash high":
                    return "gemini-3.8-flash-high";

                case "gemini-3.7-low":
                case "gemini 3.7 flash (low)":
                case "gemini 3.7 flash low":
                    return "gemini-3.7-flash-low";

                case "gemini-3.7-medium":
                case "gemini 3.7 flash (medium)":
                case "gemini 3.7 flash medium":
                    return "gemini-3.7-flash-medium";

                case "gemini-3.7-high":
                case "gemini 3.7 flash (high)":
                case "gemini 3.7 flash high":
                    return "gemini-3.7-flash-high";

                case "gemini-3.6-low":
                case "gemini 3.6 flash (low)":
                case "gemini 3.6 flash low":
                    return "gemini-3.6-flash-low";

                case "gemini-3.6-medium":
                case "gemini 3.6 flash (medium)":
                case "gemini 3.6 flash medium":
                    return "gemini-3.6-flash-medium";

                case "gemini-3.6-high":
                case "gemini 3.6 flash (high)":
                case "gemini 3.6 flash high":
                    return "gemini-3.6-flash-high";

                case "gemini-3.1-pro-low":
                case "gemini 3.1 pro (low)":
                case "gemini 3.1 pro low":
                    return "gemini-3.1-pro-low";

                case "gemini-3.1-pro-high":
                case "gemini 3.1 pro (high)":
                case "gemini 3.1 pro high":
                    return "gemini-3.1-pro-high";

                case "claude-sonnet-4-6":
                case "claude sonnet 4.6 (thinking)":
                case "claude sonnet 4.6":
                    return "claude-sonnet-4-6";

                case "claude-opus-4-6-thinking":
                case "claude opus 4.6 (thinking)":
                case "claude opus 4.6":
                    return "claude-opus-4-6-thinking";

                case "gpt-oss-120b-medium":
                case "gpt-oss 120b (medium)":
                case "gpt-oss 120b":
                    return "gpt-oss-120b-medium";

                default:
                    return trimmed;
            }
        }

        public static string ResolveEffort(string effort)
        {
            if (string.IsNullOrWhiteSpace(effort)) return null;

            string lower = effort.Trim().ToLowerInvariant();
            switch (lower)
            {
                case "low":
                case "medium":
                case "high":
                case "xhigh":
                case "max":
                    return lower;
                default:
                    return null;
            }
        }

        public static async Task<List<string>> FetchModelsAsync(string configuredCliPath, CancellationToken cancellationToken)
        {
            try
            {
                string cli = ResolveCliPath(configuredCliPath);
                ProcessResult res = await RunProcessAsync(cli, "models", 10, cancellationToken)
                    .ConfigureAwait(false);
                if (res.ExitCode == 0 && !string.IsNullOrWhiteSpace(res.Output))
                {
                    List<string> models = ParseModelsOutput(res.Output);
                    if (models.Count > 0)
                    {
                        return models;
                    }
                }
            }
            catch (Exception ex)
            {
                LogSink.Warn("antigravity: fetch models via cli failed: " + ex.Message);
            }

            return new List<string>(FallbackAntigravityModels);
        }

        public static List<string> ParseModelsOutput(string output)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(output)) return result;

            using (var reader = new StringReader(output))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    if (trimmed.StartsWith("Fetching", StringComparison.OrdinalIgnoreCase)) continue;

                    string[] parts = trimmed.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        string modelId = parts[0].Trim();
                        if (modelId.Contains("-") ||
                            modelId.StartsWith("gpt", StringComparison.OrdinalIgnoreCase) ||
                            modelId.StartsWith("claude", StringComparison.OrdinalIgnoreCase) ||
                            modelId.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!result.Contains(modelId))
                            {
                                result.Add(modelId);
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
