using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using System.Linq;

namespace ParticlesPlus
{
    public class ParticlesPlusModSystem : ModSystem
    {
        public static ModConfig LoadedConfig { get; private set; }
        private string configFileName;
        private bool configValid = true;
        private ICoreClientAPI capi;
        GuiDialog dialog;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.capi = api;
            configFileName = $"{Mod.Info.ModID}.json";
            try
            {
                LoadedConfig = api.LoadModConfig<ModConfig>(configFileName);
                if (LoadedConfig == null)
                {
                    // Load embedded default config from mod assets
                    var defaultConfigAsset = api.Assets.Get(new AssetLocation(Mod.Info.ModID, "config/particlesplus.json"));
                    string defaultConfigText = defaultConfigAsset.ToText();

                    // Deserialize default config
                    LoadedConfig = JsonConvert.DeserializeObject<ModConfig>(defaultConfigText);

                    // Save to mod config folder for future editing
                    WriteConfig();
                }
                else if (LoadedConfig.Version == 0)
                {
                    api.Logger.Error($"[{Mod.Info.Name}] Config file is missing required 'Version' field (old or malformed config). Please regenerate or update it.");
                    configValid = false;
                    return;
                }
            }
            catch (Exception e)
            {
                api.Logger.Error($"[{Mod.Info.Name}] Failed to load config file: {e.Message}");
                configValid = false;
                return;
            }
            dialog = new MainGuiDialog(api, LoadedConfig, this);   
            api.Input.RegisterHotKey(
                    "toggleParticles",
                    "Toggle Particles Plus",
                    GlKeys.P,
                    HotkeyType.HelpAndOverlays,         
                    shiftPressed: false,
                    ctrlPressed: true,
                    altPressed: false
                    );
            api.Input.SetHotKeyHandler("toggleParticles", OnHotkeyToggleParticles);
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            if (!configValid) return;
            if (LoadedConfig != null && LoadedConfig.Presets != null && LoadedConfig.Particles != null)
            {
                ApplyConfigPresets(LoadedConfig);
            }

            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        public void WriteConfig()
        {
            capi.StoreModConfig(LoadedConfig, configFileName);
        }
        private Block[] GetBlocks(string wildcard)
        {
            return capi.World.SearchBlocks(wildcard);
        }
        public void RemoveParticles(string wildcard)
        {
            if (LoadedConfig.Global)
            {
                Block[] blocks = GetBlocks(wildcard);
                foreach (Block block in blocks)
                {
                    block.ParticleProperties = Array.Empty<AdvancedParticleProperties>();
                }
            }
        }
        public void AddParticles(string wildcard, AdvancedParticleProperties[] particles)
        {
            if (LoadedConfig.Global) 
            {
                Block[] blocks = GetBlocks(wildcard);
                foreach (Block block in blocks)
                {
                    block.ParticleProperties ??= Array.Empty<AdvancedParticleProperties>();
                    block.ParticleProperties = block.ParticleProperties
                                    .Concat(particles)
                                    .ToArray();
                }
            }
        }

        private bool OnHotkeyToggleParticles(KeyCombination keyComb)
        {
            GuiElementSwitch globalSwitch = dialog.Composers["single"].GetSwitch("globalSwitch");
            ToggleParticles(LoadedConfig.Global);
            globalSwitch.SetValue(LoadedConfig.Global);
            return true;
        }

        public void ToggleParticles(bool enabled)
        {
            if (enabled)
            {
                RemoveConfigPresets(LoadedConfig);
                LoadedConfig.Global = false;
                WriteConfig();
            }
            else
            {
                LoadedConfig.Global = true;
                ApplyConfigPresets(LoadedConfig);
                WriteConfig();
            }
        }

        private void ApplyConfigPresets(ModConfig config)
        {
            foreach (var preset in config.Presets)
            {
                if (!preset.Value.Enabled) continue; // Skip if disabled

                Block[] blocks = GetBlocks(preset.Value.Wildcard);
                AdvancedParticleProperties[] particles = GetParticles(preset.Value.Particles);

                AddParticles(preset.Value.Wildcard, particles);
            }
        }

        private void RemoveConfigPresets(ModConfig config)
        {
            foreach (var preset in config.Presets)
            {
                if (!preset.Value.Enabled) continue; // Skip if disabled

                Block[] blocks = GetBlocks(preset.Value.Wildcard);
                AdvancedParticleProperties[] particles = GetParticles(preset.Value.Particles);

                RemoveParticles(preset.Value.Wildcard);
            }
        }

        private AdvancedParticleProperties[] GetParticles(string particlesKey)
        {
            return LoadedConfig.Particles.TryGetValue(particlesKey, out var particles)
                ? particles
                : Array.Empty<AdvancedParticleProperties>();
        }
    }
 
}
