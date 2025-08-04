using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ModSystem : Vintagestory.API.Common.ModSystem
    {
        public ModConfig modConfig;
        public ParticlesManager particlesManager;
        public ICoreClientAPI capi;
        public GuiDialog dialog;
        

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            modConfig = new ModConfig(this);
            dialog = new MainGuiDialog(this);
            particlesManager = new ParticlesManager(this);
            capi.Input.RegisterHotKey(
                    "toggleParticles",
                    "Toggle Particles Plus",
                    GlKeys.P,
                    HotkeyType.HelpAndOverlays,         
                    shiftPressed: false,
                    ctrlPressed: true,
                    altPressed: false
                    );
            capi.Input.SetHotKeyHandler("toggleParticles", OnHotkeyToggleParticles);

            base.StartClientSide(capi);
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            if (modConfig != null && modConfig.Presets != null && modConfig.Particles != null)
            {
                particlesManager.ApplyEnabledParticles();
            }

            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        private bool OnHotkeyToggleParticles(KeyCombination keyComb)
        {
            GuiElementSwitch globalSwitch = dialog.Composers["single"].GetSwitch("globalSwitch");
            ToggleParticles(modConfig.Global);
            globalSwitch.SetValue(modConfig.Global);
            return true;
        }
        public void ToggleParticles(bool enabled) // TODO: Fix this and find it a place to live!
        {
            if (!enabled)
            {
                particlesManager.ApplyEnabledParticles();
                modConfig.Global = false;
                modConfig.WriteConfig();
            }
            else
            {
                modConfig.Global = true;
                particlesManager.RemoveEnabledParticles();
                modConfig.WriteConfig();
            }
        }
    }
}
