using ParticlesPlus.GUI;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ModSystem : Vintagestory.API.Common.ModSystem
    {
        public ModConfig modConfig;
        public ICoreClientAPI capi;
        private GuiSystem guiSystem;


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            modConfig = new(this);
            guiSystem = new GuiSystem(this);

            base.StartClientSide(capi);
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            ModConfig.Initialize();
            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
    }
}
