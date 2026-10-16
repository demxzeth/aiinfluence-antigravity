using System;
using System.Reflection;
using System.Threading.Tasks;

namespace AIInfluenceAntigravity.Hook
{
    public static class SignatureRules
    {
        public static bool LooksLikePrompt(Type type)
        {
            if (type == null) return false;
            if (type == typeof(string)) return true;
            return IsPayloadType(type);
        }

        public const string PayloadTypeFullName = "AIInfluence.API.PromptPayload";

        public static bool IsPayloadType(Type type)
        {
            if (type == null) return false;
            return type.GetProperty("FullText", BindingFlags.Public | BindingFlags.Instance) != null
                && type.GetProperty("UserContent", BindingFlags.Public | BindingFlags.Instance) != null;
        }

        public static TargetRole Classify(MethodBase method)
        {
            if (method == null) return TargetRole.None;

            ParameterInfo[] p = method.GetParameters();
            string name = method.Name;

            if (name == "GetAIResponse" && IsDialogueShape(p)) return TargetRole.Dialogue;
            if (name == "GetRawTextResponse" && IsRawShape(p)) return TargetRole.Raw;
            if (name == "GetRawTextResponseWithBackend" && IsWithBackendShape(p)) return TargetRole.RawWithBackend;

            if (name.Length > "GetResponse".Length
                && name.StartsWith("Get", StringComparison.Ordinal)
                && name.EndsWith("Response", StringComparison.Ordinal)
                && HasPromptParam(p)
                && ReturnsText(method))
            {
                return TargetRole.BackendDirect;
            }

            return TargetRole.None;
        }

        public static bool IsDialogueShape(ParameterInfo[] p)
        {
            if (p == null) return false;

            if (p.Length == 3
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(string)
                && LooksLikePrompt(p[2].ParameterType))
            {
                return true;
            }

            if (p.Length == 4
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(string)
                && p[2].ParameterType == typeof(string)
                && p[3].ParameterType == typeof(int))
            {
                return true;
            }

            if (p.Length == 3
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(string)
                && IsPayloadType(p[2].ParameterType))
            {
                return true;
            }

            return false;
        }

        public static bool IsRawShape(ParameterInfo[] p)
        {
            if (p == null) return false;

            if (p.Length == 1 && (LooksLikePrompt(p[0].ParameterType) || IsPayloadType(p[0].ParameterType)))
            {
                return true;
            }

            if (p.Length == 2
                && (LooksLikePrompt(p[0].ParameterType) || IsPayloadType(p[0].ParameterType))
                && p[1].ParameterType == typeof(string))
            {
                return true;
            }

            if (p.Length == 3 && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(int)
                && p[2].ParameterType == typeof(string))
            {
                return true;
            }

            return false;
        }

        public static bool IsWithBackendShape(ParameterInfo[] p)
        {
            if (p == null) return false;

            if (p.Length == 2
                && (LooksLikePrompt(p[0].ParameterType) || IsPayloadType(p[0].ParameterType))
                && p[1].ParameterType == typeof(string))
            {
                return true;
            }

            if (p.Length == 3
                && (LooksLikePrompt(p[0].ParameterType) || IsPayloadType(p[0].ParameterType))
                && p[1].ParameterType == typeof(string)
                && p[2].ParameterType == typeof(string))
            {
                return true;
            }

            if (p.Length == 3
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(string)
                && p[2].ParameterType == typeof(int))
            {
                return true;
            }

            return false;
        }

        public static bool HasPromptParam(ParameterInfo[] p)
        {
            if (p == null) return false;
            foreach (ParameterInfo parameter in p)
            {
                if (LooksLikePrompt(parameter.ParameterType)) return true;
            }
            return false;
        }

        private static bool ReturnsText(MethodBase method)
        {
            MethodInfo info = method as MethodInfo;
            if (info == null) return false;
            return info.ReturnType == typeof(Task<string>) || info.ReturnType == typeof(string);
        }
    }
}
