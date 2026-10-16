using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AIInfluenceAntigravity.Config;
using AIInfluenceAntigravity.Hook;
using AIInfluenceAntigravity.Infra;

namespace AIInfluenceAntigravity.Transport
{
    public static class Pipeline
    {
        public static Func<TargetRole, string, int, MethodBase, Task<string>> SenderForTests { get; set; }

        public static async Task<string> SendAsync(
            TargetRole role, string prompt, int cachePrefix, MethodBase originalMethod,
            string splitSystem = null, string splitUser = null)
        {
            Func<TargetRole, string, int, MethodBase, Task<string>> sender = SenderForTests;
            if (sender != null)
            {
                return await sender(role, prompt, cachePrefix, originalMethod).ConfigureAwait(false);
            }

            AntigravitySettings settings = SettingsVault.Current;
            ChatTurn turn = ChatTurn.FromPrompt(prompt, splitSystem, splitUser);
            ModelOptions options = new ModelOptions(settings.Model, settings.Temperature, settings.MaxTokens);
            Stopwatch clock = Stopwatch.StartNew();

            if (settings.TimerInChat)
            {
                ChatNotices.Show("- Request sent");
            }

            try
            {
                int timeoutSeconds = settings.TimeoutSeconds < 10 ? 10 : settings.TimeoutSeconds;
                using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    Task<string> sendTask = AntigravityTransport.SendAsync(turn, options, settings, timeout.Token);

                    Task<object> progress = settings.TimerInChat
                        ? RunProgressTicker(sendTask, clock)
                        : Task.FromResult<object>(null);

                    string reply = await sendTask.ConfigureAwait(false);
                    clock.Stop();

                    if (string.IsNullOrEmpty(reply))
                    {
                        if (settings.TimerInChat)
                        {
                            ChatNotices.Error("Empty reply from Antigravity");
                        }
                        LogSink.Warn("pipeline: empty reply from Antigravity in " + clock.ElapsedMilliseconds + " ms.");
                        return string.Empty;
                    }

                    LogSink.Always("pipeline: Antigravity replied " + reply.Length + " chars in " + clock.ElapsedMilliseconds + " ms (role=" + role + ").");

                    if (settings.TimerInChat)
                    {
                        ChatNotices.Show("- Response received in " + FormatSeconds(clock.ElapsedMilliseconds));
                    }

                    return reply;
                }
            }
            catch (Exception ex)
            {
                clock.Stop();
                string msg = ex is TransportException te ? te.Message : ex.Message;

                if (settings.ErrorNotices)
                {
                    ChatNotices.Error("Request failed: " + msg + " (" + clock.ElapsedMilliseconds + " ms)");
                }

                LogSink.Error("pipeline: request failed (" + clock.ElapsedMilliseconds + " ms).", ex);
                return "[Antigravity] request failed: " + msg;
            }
        }

        private static async Task<object> RunProgressTicker(Task<string> sendTask, Stopwatch clock)
        {
            try
            {
                while (!sendTask.IsCompleted)
                {
                    Task completed = await Task.WhenAny(sendTask, Task.Delay(10000)).ConfigureAwait(false);
                    if (completed == sendTask)
                    {
                        break;
                    }

                    ChatNotices.Show("- Timer: " + (clock.ElapsedMilliseconds / 1000) + "s");
                }
            }
            catch { }
            return null;
        }

        public static string ProbeCli()
        {
            AntigravitySettings settings = SettingsVault.Current;
            string cli = AntigravityTransport.ResolveCliPath(settings.CliPath);
            Stopwatch clock = Stopwatch.StartNew();

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    ProcessResult res = AntigravityTransport.RunProcessAsync(cli, "--version", 10, cts.Token)
                        .GetAwaiter().GetResult();
                    clock.Stop();

                    if (res.ExitCode == 0)
                    {
                        string ver = (res.Output ?? string.Empty).Trim();
                        ChatNotices.Show("CLI OK (" + clock.ElapsedMilliseconds + " ms): "
                            + (string.IsNullOrEmpty(ver) ? "ready" : ver));
                        return "ok";
                    }

                    string err = !string.IsNullOrWhiteSpace(res.Error) ? res.Error : "Exit code " + res.ExitCode;
                    ChatNotices.Error("CLI test failed: " + err);
                    return "failed";
                }
            }
            catch (Exception ex)
            {
                clock.Stop();
                string msg = ex.Message;
                ChatNotices.Error("CLI test error: " + msg);
                return "error: " + msg;
            }
        }

        public static string ProbeModel()
        {
            AntigravitySettings settings = SettingsVault.Current;
            string cli = AntigravityTransport.ResolveCliPath(settings.CliPath);
            string model = AntigravityTransport.ResolveModel(settings.Model, "gemini-3.8-flash-low");
            string effort = AntigravityTransport.ResolveEffort(settings.ReasoningEffort);

            Stopwatch clock = Stopwatch.StartNew();

            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("--input-format text");
                    sb.Append(" --model ");
                    sb.Append(AntigravityTransport.EscapeArgument(model));
                    if (!string.IsNullOrEmpty(effort))
                    {
                        sb.Append(" --effort ");
                        sb.Append(effort);
                    }
                    sb.Append(" --disable-slash-commands");

                    ProcessResult res = AntigravityTransport.RunProcessAsync(
                        cli, sb.ToString(), 60, cts.Token, "Antigravity test message. Reply with exactly: pong")
                        .GetAwaiter().GetResult();
                    clock.Stop();

                    if (res.ExitCode == 0)
                    {
                        string reply = (res.Output ?? string.Empty).Trim();
                        if (string.IsNullOrEmpty(reply))
                        {
                            ChatNotices.Show("Connection OK (model responded in "
                                + FormatSeconds(clock.ElapsedMilliseconds)
                                + ", empty reply)");
                        }
                        else
                        {
                            string preview = reply.Length > 80 ? reply.Substring(0, 80) + "..." : reply;
                            ChatNotices.Show("Connection OK (model responded in "
                                + FormatSeconds(clock.ElapsedMilliseconds)
                                + "): \"" + preview + "\"");
                        }
                        return "ok";
                    }

                    string err = !string.IsNullOrWhiteSpace(res.Error) ? res.Error : "Exit code " + res.ExitCode;
                    ChatNotices.Error("Model test failed: " + err);
                    return "failed";
                }
            }
            catch (Exception ex)
            {
                clock.Stop();
                string msg = ex.Message;
                ChatNotices.Error("Model test error: " + msg);
                return "error: " + msg;
            }
        }

        public static string FormatSeconds(long ms)
        {
            double sec = Math.Round(ms / 1000.0, 1);
            if (sec < 60)
            {
                return sec.ToString("0.#") + "s";
            }
            int minutes = (int)(sec / 60);
            int remSec = (int)(sec % 60);
            return minutes + "m " + remSec + "s";
        }
    }
}
