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
        private ParticlesManager ParticlesManager => new(API);
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

        private void SyncParticles(string wildcard, PresetConfig config)
        {

            ParticlesManager.RemoveParticles(wildcard);

            if (config.Enabled && Global)
            {
                var particles = GetConfigParticles(config.Particles);
                ParticlesManager.AddParticles(config.Wildcard, particles);
            }
        }

        public string CreatePreset()
        {

            int suffix = 1;
            string newKey = $"new-preset-{suffix}";

            while (Presets.ContainsKey(newKey))
            {
                suffix++;
                newKey = $"new-preset-{suffix}";
            }

            PresetConfig boilerplate = new PresetConfig
            {
                Enabled = false,
                Wildcard = "entity/*",
                Particles = ""
            };

            Presets[newKey] = boilerplate;
            WriteConfig();

            return newKey;
        }

        public bool UpdatePreset(string oldKey, PresetConfig updatedPreset, string newKey = null)
        {
            if (!Presets.TryGetValue(oldKey, out var oldConfig)) return false;

            string finalKey = oldKey;

            if (!string.IsNullOrEmpty(newKey) && newKey != oldKey)
            {
                if (Presets.ContainsKey(newKey)) return false;
                Presets.Remove(oldKey);
                finalKey = newKey;
            }

            Presets[finalKey] = updatedPreset;

            SyncParticles(oldConfig.Wildcard, updatedPreset);

            WriteConfig();
            return true;
        }

        public bool RemovePreset(string key)
        {
            if (string.IsNullOrEmpty(key) || !Presets.TryGetValue(key, out var config))
            {
                return false;
            }

            if (config.Enabled)
            {
                ParticlesManager.RemoveParticles(config.Wildcard);
            }

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
