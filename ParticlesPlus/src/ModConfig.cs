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
        private readonly ChatMessanger _chatMessanger;

        private readonly ICoreClientAPI _api;
        private ParticlesManager _particlesManager;
        private string ConfigFileName => $"{_modSystem.Mod.Info.ModID}.json";

        public ModConfig() { }
        public ModConfig(ModSystem modSystem)
        {
            _modSystem = modSystem;
            _api = _modSystem.capi;
            _particlesManager = new(_api);
            _chatMessanger = new(modSystem);

            try
            {
                var loadedConfig = _api.LoadModConfig<ModConfig>(ConfigFileName);

                if (loadedConfig == null) // If config doesn't exist create and write default one
                {
                    var defaultConfigAsset = _api.Assets.Get(new AssetLocation(modSystem.Mod.Info.ModID, "config/particlesplus.json"));
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
                        _api.Logger.Error(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                    CopyFrom(loadedConfig); // Otherwise load existing config
                }
            }
            catch (Exception e) // Catch anything else
            {
                string errorMsg = $"[{modSystem.Mod.Info.Name}] Failed to load config file: {e.Message}";
                _api.Logger.Error(errorMsg);
                throw new InvalidOperationException(errorMsg, e);
            }
            Initialize();
        }
        private void Initialize()
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
        private void CopyFrom(ModConfig modConfig)
        {
            if (modConfig == null) return;

            Version = modConfig.Version;
            Global = modConfig.Global;
            Presets = new(modConfig.Presets ?? new());
            Particles = new(modConfig.Particles ?? new());
        }

        private void SyncParticles(string wildcard, PresetConfig presetConfig)
        {
            _particlesManager.RemoveParticles(wildcard);

            if (!string.IsNullOrEmpty(presetConfig.Wildcard) && (presetConfig.Enabled && Global))
            {
                var particles = GetConfigParticles(presetConfig.Particles);
                _particlesManager.AddParticles(presetConfig.Wildcard, particles);
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
                _chatMessanger.ShowMessage(Constants.ChatMessages.PresetNotFound, MessageType.Error);
                return false;
            }

            string keyToUpdate = presetKey;

            if (string.IsNullOrEmpty(presetName))
            {
                _chatMessanger.ShowMessage(Constants.ChatMessages.EmptyName, MessageType.Error);
                return false;
            }

            if (presetKey != presetName)
            {
                // 2.1 Check if name already exists
                if (Presets.ContainsKey(presetName))
                {
                    _chatMessanger.ShowMessage(Constants.ChatMessages.DuplicateNameError, MessageType.Error);
                    return false;
                }

                // 2.2 Remove old preset
                Presets.Remove(presetKey);
                keyToUpdate = presetName;
            }

            SyncParticles(oldPreset.Wildcard, updatedPreset);
            Presets[keyToUpdate] = updatedPreset with { };
            WriteConfig();

            _chatMessanger.ShowMessage(Constants.ChatMessages.PresetSaved, MessageType.Success);

            return true;
        }

        public bool RemovePreset(string key)
        {
            if (string.IsNullOrEmpty(key) || !Presets.TryGetValue(key, out var config))
            {
                _chatMessanger.ShowMessage(Constants.ChatMessages.PresetNotFound, MessageType.Error);
                return false;
            }

            if (config.Enabled)
            {
                _particlesManager.RemoveParticles(config.Wildcard);
            }

            Presets.Remove(key);
            WriteConfig();

            _chatMessanger.ShowMessage(Constants.ChatMessages.PresetRemoved, MessageType.Success);
            return true;
        }

        public void WriteConfig()
        {
            _api.StoreModConfig(this, ConfigFileName);
        }
        public void ApplyEnabledParticles()
        {
            if (Presets == null || Particles == null) return;

            foreach (var preset in Presets)
            {
                if (!preset.Value.Enabled) continue;
                AdvancedParticleProperties[] particles = GetConfigParticles(preset.Value.Particles);
                _particlesManager.AddParticles(preset.Value.Wildcard, particles);
            }
        }
        public void RemoveEnabledParticles()
        {
            foreach (var preset in Presets)
            {
                if (!preset.Value.Enabled) continue;

                _particlesManager.RemoveParticles(preset.Value.Wildcard);
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
