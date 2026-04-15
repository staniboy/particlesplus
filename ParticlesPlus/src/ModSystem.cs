using ParticlesPlus.GUI;
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
        public GuiSystem GUI { get; private set; }


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            API = api;
            ParticlesManager = new(this);
            ModConfig = new(this);
            GUI = new(this);
            ChatMessanger = new(this);

            base.StartClientSide(API);
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            ModConfig.Initialize();
            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
    }
}
