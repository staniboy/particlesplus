using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{

    public record PresetConfig
    {
        public bool Enabled { get; set; }
        public string Wildcard { get; set; } = "";
        public string Particles { get; set; } = "";
    }
    public class ModConfig
    {
        public int Version { get; set; }
        public bool Global { get; set; } = true;
        public Dictionary<string, PresetConfig> Presets { get; set; } = new();
        public Dictionary<string, AdvancedParticleProperties[]> Particles { get; set; } = new();


        private readonly ModSystem _modSystem;

        private ICoreClientAPI API => _modSystem.capi;
        private ParticlesManager ParticlesManager => new (API);
        private string ConfigFileName => $"{_modSystem.Mod.Info.ModID}.json";

        public ModConfig() { }
        public ModConfig(ModSystem modSystem)
        {
            _modSystem = modSystem;

            try
            {
                var loadedConfig = API.LoadModConfig<ModConfig>(ConfigFileName);

                if (loadedConfig == null) // If config doesn't exist create and write default one
                {
                    var defaultConfigAsset = API.Assets.Get(new AssetLocation(modSystem.Mod.Info.ModID, "config/particlesplus.json"));
                    string defaultConfigText = defaultConfigAsset.ToText();

                    loadedConfig = JsonConvert.DeserializeObject<ModConfig>(defaultConfigText);

                    CopyFrom(loadedConfig);
                    WriteConfig();
                }
                else
                {
                    if (loadedConfig.Version == 0) // If version mismatch throw an error
                    {
                        string errorMsg = $"[{modSystem.Mod.Info.Name}] Config file is missing required 'Version' field (old or malformed config). Please regenerate or update it."; 
                        API.Logger.Error(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                    CopyFrom(loadedConfig); // Otherwise load existing config
                }
            }
            catch (Exception e) // Catch anything else
            {
                string errorMsg = $"[{modSystem.Mod.Info.Name}] Failed to load config file: {e.Message}";
                API.Logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg, e);
            }
        }
        public void Initialize()
        {
            if (!Global) return;
            ApplyEnabledParticles();
        }

        public void SetGlobal(bool enabled) 
        {
            if (Global == enabled) return;
            
            Global = enabled;
            WriteConfig();

            if (Global)
            {
                ApplyEnabledParticles();
            }
            else
            {
                RemoveEnabledParticles();
            }
        }
        private void CopyFrom(ModConfig modConfig)
        {
            if (modConfig == null) return;

            Version = modConfig.Version;
            Global = modConfig.Global;
            Presets = modConfig.Presets ?? new();
            Particles = modConfig.Particles ?? new();
        }
        public bool UpdatePreset(string key, PresetConfig updatedPreset)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("[Particles Plus]: Preset name cannot be null or empty", nameof(key));

            if (!Presets.TryGetValue(key, out PresetConfig targetPreset))
                return false;

            if (targetPreset == updatedPreset)
                return true;

            if (targetPreset.Enabled)
            {
                ParticlesManager.RemoveParticles(targetPreset.Wildcard);
            }

            if (updatedPreset.Enabled && Global)
            {
                AdvancedParticleProperties[] particles = GetConfigParticles(updatedPreset.Particles);
                ParticlesManager.AddParticles(updatedPreset.Wildcard, particles);
            }

            Presets[key] = updatedPreset;
            WriteConfig();

            return true;
        }
        public bool RemovePreset(string key)
        {
            if (key == "<none>")
            {
                return false;
            }

            ParticlesManager.RemoveParticles(Presets[key].Wildcard);
            Presets.Remove(key);
            WriteConfig();

            return true;
        }
        public void WriteConfig()
        {
            API.StoreModConfig(this, ConfigFileName);
        }
        public void ApplyEnabledParticles()
        {
            if (Presets == null || Particles == null) return;

            foreach (var preset in Presets)
            {
                if (!preset.Value.Enabled) continue;
                AdvancedParticleProperties[] particles = GetConfigParticles(preset.Value.Particles);
                ParticlesManager.AddParticles(preset.Value.Wildcard, particles);
            }
        }
        public void RemoveEnabledParticles()
        {
            foreach (var preset in Presets)
            {
                if (!preset.Value.Enabled) continue;

                ParticlesManager.RemoveParticles(preset.Value.Wildcard);
            }
        }
        private AdvancedParticleProperties[] GetConfigParticles(string particlesKey)
        {
            return Particles.TryGetValue(particlesKey, out var particles)
                ? particles
                : Array.Empty<AdvancedParticleProperties>();
        }
    }
}
