using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using System.Linq;

namespace ParticlesPlus
{
    public class ModConfig
    {
        public int Version { get; set; }
        public Dictionary<string, PresetConfig> Presets { get; set; } = new();
        public Dictionary<string, AdvancedParticleProperties[]> Particles { get; set; } = new();
    }

    public class PresetConfig
    {
        public bool Enabled { get; set; }
        public string Wildcard { get; set; }
        public string Particles { get; set; }
    }
    public class ParticlesPlusModSystem : ModSystem
    {
        public static ModConfig LoadedConfig { get; private set; }
        private string configFileName;
        private bool configValid = true;
        GuiDialog dialog;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
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
                    WriteConfig(api);
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
        }
        public override void AssetsFinalize(ICoreAPI api)
        {
            if (!configValid) return;
            if (LoadedConfig != null && LoadedConfig.Presets != null && LoadedConfig.Particles != null)
            {
                foreach (Block block in api.World.Blocks)
                {
                    foreach (var preset in LoadedConfig.Presets)
                    {
                        if (!preset.Value.Enabled) continue; // Skip if disabled

                        if (block.WildCardMatch(preset.Value.Wildcard))
                        {
                            if (LoadedConfig.Particles.TryGetValue(preset.Value.Particles, out var particles))
                            {
                                // Initialize if null
                                block.ParticleProperties ??= Array.Empty<AdvancedParticleProperties>();

                                // Append particles (creates new array)
                                block.ParticleProperties = block.ParticleProperties
                                    .Concat(particles)
                                    .ToArray();
                            }
                            else
                            {
                                api.Logger.Warning($"[{Mod.Info.Name}] No particles found for key '{preset.Value.Particles}' in config.");
                            }
                        }
                    }
                }
            }
            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        public void WriteConfig(ICoreClientAPI capi)
        {
            capi.StoreModConfig(LoadedConfig, configFileName);
        }
    }
 
}
