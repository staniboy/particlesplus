using System;
using Vintagestory.API.Common;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using System.Linq;
using Vintagestory.GameContent;

namespace ParticlesPlus
{
    public class ModSystem : Vintagestory.API.Common.ModSystem
    {
        public static ModConfig Config { get; private set; }
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
                Config = api.LoadModConfig<ModConfig>(configFileName);
                if (Config == null)
                {
                    // Load embedded default config from mod assets
                    var defaultConfigAsset = api.Assets.Get(new AssetLocation(Mod.Info.ModID, "config/particlesplus.json"));
                    string defaultConfigText = defaultConfigAsset.ToText();

                    // Deserialize default config
                    Config = JsonConvert.DeserializeObject<ModConfig>(defaultConfigText);

                    // Save to mod config folder for future editing
                    WriteConfig();
                }
                else if (Config.Version == 0)
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
            dialog = new MainGuiDialog(api, Config, this);   
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
            if (Config != null && Config.Presets != null && Config.Particles != null)
            {
                ApplyConfigPresets(Config);
            }

            api.Logger.Event($"Started [{Mod.Info.Name}] mod");
        }
        public void WriteConfig()
        {
            capi.StoreModConfig(Config, configFileName);
        }
        private Block[] GetBlocks(string wildcard)
        {
            return capi.World.SearchBlocks(wildcard);
        }
        public void RemoveParticles(string wildcard)
        {
                Block[] blocks = GetBlocks(wildcard);
                foreach (Block block in blocks)
                {
                    block.ParticleProperties = Array.Empty<AdvancedParticleProperties>();
                }
        }
        public void AddParticles(string wildcard, AdvancedParticleProperties[] particles)
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
        private bool OnHotkeyToggleParticles(KeyCombination keyComb)
        {
            GuiElementSwitch globalSwitch = dialog.Composers["single"].GetSwitch("globalSwitch");
            ToggleParticles(Config.Global);
            globalSwitch.SetValue(Config.Global);
            return true;
        }
        public void ToggleParticles(bool enabled)
        {
            if (enabled)
            {
                RemoveConfigPresets(Config);
                Config.Global = false;
                WriteConfig();
            }
            else
            {
                Config.Global = true;
                ApplyConfigPresets(Config);
                WriteConfig();
            }
        }
        private void ApplyConfigPresets(ModConfig config)
        {
            foreach (var preset in config.Presets)
            {
                if (!preset.Value.Enabled) continue; // Skip if disabled

                AdvancedParticleProperties[] particles = GetParticles(preset.Value.Particles);

                AddParticles(preset.Value.Wildcard, particles);
            }
        }
        private void RemoveConfigPresets(ModConfig config)
        {
            foreach (var preset in config.Presets)
            {
                if (!preset.Value.Enabled) continue; // Skip if disabled

                RemoveParticles(preset.Value.Wildcard);
            }
        }
        private static AdvancedParticleProperties[] GetParticles(string particlesKey)
        {
            return Config.Particles.TryGetValue(particlesKey, out var particles)
                ? particles
                : Array.Empty<AdvancedParticleProperties>();
        }
        public bool UpdateConfigPreset(string key, PresetConfig updatedPreset)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("[Particles Plus]: Preset name cannot be null or empty", nameof(key));

            if (!Config.Presets.TryGetValue(key, out PresetConfig targetPreset))
                return false;

            if (targetPreset == updatedPreset)
                return true;
           
            if (targetPreset.Enabled)
            {
                RemoveParticles(targetPreset.Wildcard);
            }

            if (updatedPreset.Enabled && Config.Global)
            {
                AdvancedParticleProperties[] particles = GetParticles(updatedPreset.Particles);
                AddParticles(updatedPreset.Wildcard, particles);
            }

            Config.Presets[key] = updatedPreset;
            WriteConfig();

            return true;
        }
    }
}
