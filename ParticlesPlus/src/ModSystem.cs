using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ModSystem : Vintagestory.API.Common.ModSystem
    {
        public ICoreClientAPI API { get; private set; }
        public ModConfig ModConfig { get; private set; }
        public ChatMessanger ChatMessanger { get; private set; }
        public ParticlesManager ParticlesManager { get; private set; }
        public MainGuiDialog GUI { get; private set; }


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            API = api;
            ModConfig = new(this);
            ChatMessanger = new(this);
            ParticlesManager = new(this);
            api.Event.BlockTexturesLoaded += OnBlockTexturesLoaded;
            API.Input.RegisterHotKey(
                    "toggleParticles",
                    "Toggle Particles Plus",
                    GlKeys.P,
                    HotkeyType.HelpAndOverlays,
                    shiftPressed: false,
                    ctrlPressed: true,
                    altPressed: false
                    );
            API.Input.SetHotKeyHandler("toggleParticles", OnHotkeyToggleParticles);

            base.StartClientSide(API);
        }

        private void OnBlockTexturesLoaded()
        {
            GUI = new MainGuiDialog(this);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            ModConfig.Initialize();

            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        private bool OnHotkeyToggleParticles(KeyCombination keyComb)
        {
            GuiElementSwitch globalSwitch = GUI.SingleComposer.GetSwitch("globalSwitch");

            globalSwitch.SetValue(!ModConfig.Global);
            ModConfig.SetGlobal(!ModConfig.Global);

            return true;
        }
    }
}
