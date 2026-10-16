using System.Reflection;

namespace AIInfluenceAntigravity.Hook
{
    public sealed class HookTarget
    {
        public MethodInfo Method { get; }
        public TargetRole Role { get; }

        public HookTarget(MethodInfo method, TargetRole role)
        {
            Method = method;
            Role = role;
        }
    }
}
