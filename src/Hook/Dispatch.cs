using System;
using System.Reflection;
using System.Threading.Tasks;
using AIInfluenceAntigravity.Config;
using AIInfluenceAntigravity.Infra;
using AIInfluenceAntigravity.Transport;

namespace AIInfluenceAntigravity.Hook
{
    public static class Dispatch
    {
        public static bool Prefix(object[] __args, MethodBase __originalMethod, ref Task<string> __result)
        {
            try
            {
                TargetRole role = SignatureRules.Classify(__originalMethod);
                if (role == TargetRole.None)
                {
                    LogSink.Warn("dispatch: unknown role for " + __originalMethod.Name + "; passing through.");
                    return true;
                }

                if (!SettingsVault.Current.Enabled)
                {
                    LogSink.Info("dispatch: Antigravity switch off; passing through.");
                    return true;
                }

                if (!TryExtract(__args, out string prompt, out int cachePrefix, out string systemPart, out string userPart))
                {
                    LogSink.Warn("dispatch[" + role + "]: no prompt in args ("
                        + (__args == null ? "null" : __args.Length.ToString()) + " params); passing through.");
                    return true;
                }

                LogSink.Info("dispatch[" + role + "]: prompt=" + prompt.Length + " chars, cachePrefix=" + cachePrefix);

                MethodInfo callable = __originalMethod as MethodInfo;
                if (callable == null || callable.ReturnType != typeof(Task<string>))
                {
                    LogSink.Warn("dispatch[" + role + "]: unexpected return type on "
                        + __originalMethod.Name + "; passing through.");
                    return true;
                }

                __result = Pipeline.SendAsync(role, prompt, cachePrefix, __originalMethod, systemPart, userPart);
                return false;
            }
            catch (Exception ex)
            {
                LogSink.Error("dispatch failed; passing through.", ex);
                return true;
            }
        }

        public static bool TryExtract(object[] args, out string prompt, out int cachePrefix)
        {
            return TryExtract(args, out prompt, out cachePrefix, out _, out _);
        }

        public static bool TryExtract(
            object[] args, out string prompt, out int cachePrefix, out string systemContent, out string userContent)
        {
            prompt = null;
            cachePrefix = 0;
            systemContent = null;
            userContent = null;

            if (args == null || args.Length == 0)
            {
                return false;
            }

            string best = null;
            object bestArg = null;
            foreach (object arg in args)
            {
                string candidate = AsPrompt(arg);
                if (candidate != null && (best == null || candidate.Length > best.Length))
                {
                    best = candidate;
                    bestArg = arg;
                }
            }

            prompt = best;
            if (prompt == null)
            {
                return false;
            }

            for (int i = args.Length - 1; i >= 0; i--)
            {
                if (args[i] is int value)
                {
                    cachePrefix = value;
                    break;
                }
            }

            if (bestArg != null && !(bestArg is string))
            {
                ExtractPayloadSplit(bestArg, ref cachePrefix, out systemContent, out userContent);
            }

            if (string.IsNullOrEmpty(systemContent) || string.IsNullOrEmpty(userContent))
            {
                TryDeriveTextSplit(prompt, cachePrefix, out systemContent, out userContent);
            }

            return true;
        }

        private static void ExtractPayloadSplit(
            object payload, ref int cachePrefix, out string systemContent, out string userContent)
        {
            systemContent = null;
            userContent = null;
            if (payload == null) return;

            Type type = payload.GetType();
            PropertyInfo sysProp = type.GetProperty("SystemContent", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo usrProp = type.GetProperty("UserContent", BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo pfxProp = type.GetProperty("StaticPrefixLength", BindingFlags.Public | BindingFlags.Instance);

            if (sysProp != null && usrProp != null)
            {
                string s = sysProp.GetValue(payload, null) as string;
                string u = usrProp.GetValue(payload, null) as string;
                if (!string.IsNullOrEmpty(s) && !string.IsNullOrEmpty(u))
                {
                    systemContent = s;
                    userContent = u;
                }
            }

            if (pfxProp != null)
            {
                object val = pfxProp.GetValue(payload, null);
                if (val is int pfx && pfx > 0)
                {
                    cachePrefix = pfx;
                }
            }
        }

        public static bool TryDeriveTextSplit(
            string prompt, int cachePrefix, out string systemContent, out string userContent)
        {
            systemContent = null;
            userContent = null;
            if (string.IsNullOrEmpty(prompt)) return false;

            if (cachePrefix > 0 && cachePrefix < prompt.Length)
            {
                systemContent = prompt.Substring(0, cachePrefix).TrimEnd();
                userContent = prompt.Substring(cachePrefix).TrimStart();
                return true;
            }

            string[] markers = new string[]
            {
                "### ТЕКУЩАЯ ИСТОРИЯ ДИАЛОГА",
                "### Conversation history ###",
                "### Conversation History ###",
                "### Group conversation ###",
                "### Dialogue history ###"
            };

            foreach (string marker in markers)
            {
                int idx = prompt.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    int sep = prompt.LastIndexOf("============================================================", idx, StringComparison.Ordinal);
                    int split = (sep > 0 && (idx - sep) < 200) ? sep : idx;

                    systemContent = prompt.Substring(0, split).TrimEnd();
                    userContent = prompt.Substring(split).TrimStart();
                    return true;
                }
            }

            return false;
        }

        public static string AsPrompt(object arg)
        {
            if (arg == null) return null;

            if (arg is string text)
            {
                return text.Length > 0 ? text : null;
            }

            PropertyInfo prompt = arg.GetType().GetProperty(
                "FullText", BindingFlags.Public | BindingFlags.Instance);
            if (prompt != null)
            {
                string value = prompt.GetValue(arg, null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }

                PropertyInfo system = arg.GetType().GetProperty(
                    "SystemContent", BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo user = arg.GetType().GetProperty(
                    "UserContent", BindingFlags.Public | BindingFlags.Instance);
                string s = system == null ? null : system.GetValue(arg, null) as string;
                string u = user == null ? null : user.GetValue(arg, null) as string;
                if (!string.IsNullOrEmpty(s) || !string.IsNullOrEmpty(u))
                {
                    return (s ?? string.Empty) + Environment.NewLine + (u ?? string.Empty);
                }
            }

            return null;
        }
    }
}
