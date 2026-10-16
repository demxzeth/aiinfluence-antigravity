using System;
using System.IO;
using System.Reflection;

namespace AIInfluenceAntigravity.Infra
{
    public static class ModuleLocator
    {
        private static string _root;

        public static string Root => _root ?? (_root = ResolveRoot());

        public static void OverrideRoot(string root)
        {
            _root = root ?? string.Empty;
        }

        public static void Reset()
        {
            _root = null;
        }

        private static string ResolveRoot()
        {
            try
            {
                string assemblyPath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
                string binFolder = Path.GetDirectoryName(assemblyPath);
                string shipping = Path.GetDirectoryName(binFolder);
                return Directory.GetParent(shipping)?.FullName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
