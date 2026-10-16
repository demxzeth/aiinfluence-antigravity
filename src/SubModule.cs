using System;
using System.IO;
using TaleWorlds.MountAndBlade;
using AIInfluenceAntigravity.Config;
using AIInfluenceAntigravity.Hook;
using AIInfluenceAntigravity.Infra;

namespace AIInfluenceAntigravity
{
    public sealed class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            string root = ModuleLocator.Root;
            if (string.IsNullOrEmpty(root))
            {
                LogSink.Initialize(Directory.GetCurrentDirectory(), verbose: true);
                LogSink.Error("module root could not be resolved; logging to current directory.");
            }
            else
            {
                LogSink.Initialize(root, verbose: true);
                LogSink.Always("loaded; root: " + root);
            }

            LogSink.Enabled = SettingsVault.Current.LogEnabled;

            try
            {
                McmSettings settings = McmSettings.Instance;
                LogSink.Always("mcm: settings registered.");
            }
            catch (Exception ex)
            {
                LogSink.Warn("mcm: registration failed (MCM not loaded?): " + ex.Message);
            }

            Engine.Create();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            Engine.PrimeStep(1f);
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            ChatNotices.FlushPending();
            Engine.PrimeStep(dt);
        }

        protected override void OnSubModuleUnloaded()
        {
            ChatNotices.FlushPending();
            Engine.TearDown();
            LogSink.Always("unloaded; patches removed.");
            LogSink.Flush();
            LogSink.Shutdown();
            base.OnSubModuleUnloaded();
        }
    }
}
