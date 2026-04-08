using ParticlesPlus.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ModSystem : Vintagestory.API.Common.ModSystem
    {
        public ModConfig ModConfig => new(this);
        public ICoreClientAPI capi;
        private GuiDialog dialog;
        private GuiSystem guiSystem;


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            dialog = new MainGuiDialog(this);
            guiSystem = new GuiSystem(this);

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
            ModConfig.Initialize();
            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        private bool OnHotkeyToggleParticles(KeyCombination keyComb)
        {
            GuiElementSwitch globalSwitch = dialog.Composers["single"].GetSwitch("globalSwitch");

            globalSwitch.SetValue(!ModConfig.Global);
            ModConfig.SetGlobal(!ModConfig.Global);

            return true;
        }
    }
}
