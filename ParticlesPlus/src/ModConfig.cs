using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{

    public record PresetConfig
    {
        public bool Enabled { get; init; }
        public string Wildcard { get; init; } = "";
        public string Particles { get; init; } = "";
    }
    public class ModConfig
    {
        public int Version { get; set; }
        public bool Global { get; set; } = true;
        public Dictionary<string, PresetConfig> Presets { get; set; } = new();
        public Dictionary<string, AdvancedParticleProperties[]> Particles { get; set; } = new();


        private readonly ModSystem _modSystem;
        private ICoreClientAPI API => _modSystem.API;
        private ParticlesManager ParticlesManager => _modSystem.ParticlesManager;
        private ChatMessanger ChatMessanger => _modSystem.ChatMessanger;

        private string ConfigFileName => $"{_modSystem.Mod.Info.ModID}.json";

        [JsonConstructor]
        private ModConfig() { }
        public ModConfig(ModSystem modSystem)
        {
            _modSystem = modSystem;

            try
            {
                var loadedConfig = API.LoadModConfig<ModConfig>(ConfigFileName);

                if (loadedConfig == null) // If config doesn't exist create and write default one
                {
                    LoadDefaultModConfig();
                }
                else
                {
                    if (loadedConfig.Version == 0) // If version mismatch throw an error
                    {
                        string errorMsg = $"[{modSystem.Mod.Info.Name}] Config file is missing required 'Version' field (old or malformed config). Please regenerate or update it.";
                        API.Logger.Error(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                    SetAsCurrentModConfig(loadedConfig); // Otherwise load existing config
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
        public bool ToggleGlobal()
        {
            Global = !Global;

            if (Global)
            {
                ApplyEnabledParticles();
            }
            else
            {
                RemoveEnabledParticles();
            }
            WriteConfig();
            ChatMessanger.ShowMessage(Constants.ChatMessages.GlobalStatus + Global, MessageType.Success);
            return Global;
        }
        private void LoadDefaultModConfig()
        {
            var defaultConfigAsset = API.Assets.Get(new AssetLocation(_modSystem.Mod.Info.ModID, "config/particlesplus.json"));
            string defaultConfigText = defaultConfigAsset.ToText();
            SetAsCurrentModConfig(JsonConvert.DeserializeObject<ModConfig>(defaultConfigText));
            WriteConfig();
        }
        private void SetAsCurrentModConfig(ModConfig modConfig)
        {
            if (modConfig == null) return;

            Version = modConfig.Version;
            Global = modConfig.Global;
            Presets = new(modConfig.Presets ?? new());
            Particles = new(modConfig.Particles ?? new());
        }
        private void SyncParticles(string wildcard, PresetConfig presetConfig)
        {
            ParticlesManager.RemoveParticles(wildcard);

            if (!string.IsNullOrEmpty(presetConfig.Wildcard) && (presetConfig.Enabled && Global))
            {
                var particles = GetConfigParticles(presetConfig.Particles);
                ParticlesManager.AddParticles(presetConfig.Wildcard, particles);
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
        public bool UpdatePreset(string presetKey, PresetConfig updatedPreset, string presetName)
        {
            if (!Presets.TryGetValue(presetKey, out var oldPreset))
            {
                ChatMessanger.ShowMessage(Constants.ChatMessages.PresetNotFound, MessageType.Error);
                return false;
            }

            string keyToUpdate = presetKey;

            if (string.IsNullOrEmpty(presetName))
            {
                ChatMessanger.ShowMessage(Constants.ChatMessages.EmptyName, MessageType.Error);
                return false;
            }

            if (presetKey != presetName)
            {
                if (Presets.ContainsKey(presetName))
                {
                    ChatMessanger.ShowMessage(Constants.ChatMessages.DuplicateNameError, MessageType.Error);
                    return false;
                }
                Presets.Remove(presetKey);
                keyToUpdate = presetName;
            }

            SyncParticles(oldPreset.Wildcard, updatedPreset);
            Presets[keyToUpdate] = updatedPreset with { };
            WriteConfig();

            ChatMessanger.ShowMessage(Constants.ChatMessages.PresetSaved, MessageType.Success);

            return true;
        }
        public bool RemovePreset(string key)
        {
            if (string.IsNullOrEmpty(key) || !Presets.TryGetValue(key, out var config))
            {
                ChatMessanger.ShowMessage(Constants.ChatMessages.PresetNotFound, MessageType.Error);
                return false;
            }

            if (config.Enabled)
            {
                ParticlesManager.RemoveParticles(config.Wildcard);
            }

            Presets.Remove(key);
            WriteConfig();

            ChatMessanger.ShowMessage(Constants.ChatMessages.PresetRemoved, MessageType.Success);
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
