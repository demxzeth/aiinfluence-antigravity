using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AIInfluenceAntigravity.Infra;

namespace AIInfluenceAntigravity.Hook
{
    public static class TargetScanner
    {
        public const string EntryTypeFullName = "AIInfluence.API.AIClient";
        private static Type _entryType;

        public static Type EntryType => _entryType ?? (_entryType = FindEntryType());

        public static IReadOnlyList<HookTarget> CaptureTargets()
        {
            Type entry = EntryType;
            if (entry == null)
            {
                LogSink.Info("TargetScanner: " + EntryTypeFullName + " not loaded yet.");
                return new HookTarget[0];
            }

            const BindingFlags scan =
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

            List<HookTarget> targets = new List<HookTarget>();
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (MethodInfo method in entry.GetMethods(scan))
            {
                if (method.ReturnType != typeof(Task<string>))
                {
                    continue;
                }

                TargetRole role = SignatureRules.Classify(method);
                if (role == TargetRole.None)
                {
                    continue;
                }

                string key = method.Module.ModuleVersionId + ":" + method.MetadataToken;
                if (!seen.Add(key))
                {
                    continue;
                }

                targets.Add(new HookTarget(method, role));
            }

            LogSink.Info("TargetScanner: " + targets.Count + " target(s) on " + entry.FullName
                + " (dialogue=" + Count(targets, TargetRole.Dialogue)
                + " raw=" + Count(targets, TargetRole.Raw)
                + " withBackend=" + Count(targets, TargetRole.RawWithBackend)
                + " backendDirect=" + Count(targets, TargetRole.BackendDirect) + ").");

            return targets;
        }

        public static void Reset()
        {
            _entryType = null;
        }

        private static int Count(IReadOnlyList<HookTarget> targets, TargetRole role)
        {
            int n = 0;
            foreach (HookTarget target in targets)
            {
                if (target.Role == role) n++;
            }
            return n;
        }

        private static Type FindEntryType()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type t = assembly.GetType(EntryTypeFullName, throwOnError: false, ignoreCase: false);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }
    }
}
