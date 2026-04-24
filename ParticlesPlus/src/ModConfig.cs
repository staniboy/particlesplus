using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
        private ChatMessenger ChatMessenger => _modSystem.ChatMessenger;

        private string ConfigFileName => $"{_modSystem.Mod.Info.ModID}.json";

        [JsonConstructor]
        private ModConfig() { }
        public ModConfig(ModSystem modSystem)
        {
            _modSystem = modSystem;

            try
            {
                var loadedConfig = API.LoadModConfig<ModConfig>(ConfigFileName);

                if (loadedConfig == null)
                {
                    SetAsCurrentModConfig(GetDefaultConfig());
                }
                else
                {
                    if (loadedConfig.Version == 0)
                    {
                        string errorMsg = $"[{modSystem.Mod.Info.Name}] Config file is missing required 'Version' field (old or malformed config). Please regenerate or update it.";
                        API.Logger.Error(errorMsg);
                        throw new InvalidOperationException(errorMsg);
                    }
                    SetAsCurrentModConfig(loadedConfig);
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception e)
            {
                string errorMsg = $"[{modSystem.Mod.Info.Name}] Failed to load config file: {e.Message}";
                API.Logger.Error(errorMsg);

                try
                {
                    string configPath = API.GetOrCreateDataPath("ModConfig") + "/" + ConfigFileName;
                    File.Move(configPath, Path.ChangeExtension(configPath, ".bad.json"), overwrite: true);
                }
                catch { /* best effort */ }

                SetAsCurrentModConfig(GetDefaultConfig());
                WriteConfig();
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
            ChatMessenger.ShowMessege(Constants.ChatMessages.GlobalStatus + Global, MessegeType.Success);
            return Global;
        }

        private ModConfig GetDefaultConfig()
        {
            var defaultConfigAsset = API.Assets.Get(new AssetLocation(_modSystem.Mod.Info.ModID, "config/particlesplus.json"));
            string defaultConfigText = defaultConfigAsset.ToText();
            ModConfig defaultModConfig = JsonConvert.DeserializeObject<ModConfig>(defaultConfigText);
            return defaultModConfig;
        }
        public void LoadDefaultModConfig()
        {
            if (Global) RemoveEnabledParticles();
            SetAsCurrentModConfig(GetDefaultConfig());
            if (Global) ApplyEnabledParticles();
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
                ChatMessenger.ShowMessege(Constants.ChatMessages.PresetNotFound, MessegeType.Error);
                return false;
            }

            string keyToUpdate = presetKey;

            if (string.IsNullOrEmpty(presetName))
            {
                ChatMessenger.ShowMessege(Constants.ChatMessages.EmptyName, MessegeType.Error);
                return false;
            }

            if (presetKey != presetName)
            {
                if (Presets.ContainsKey(presetName))
                {
                    ChatMessenger.ShowMessege(Constants.ChatMessages.DuplicateNameError, MessegeType.Error);
                    return false;
                }
                Presets.Remove(presetKey);
                keyToUpdate = presetName;
            }

            SyncParticles(oldPreset.Wildcard, updatedPreset);
            Presets[keyToUpdate] = updatedPreset with { };
            WriteConfig();

            ChatMessenger.ShowMessege(Constants.ChatMessages.PresetSaved, MessegeType.Success);

            return true;
        }
        public bool RemovePreset(string key)
        {
            if (string.IsNullOrEmpty(key) || !Presets.TryGetValue(key, out var config))
            {
                ChatMessenger.ShowMessege(Constants.ChatMessages.PresetNotFound, MessegeType.Error);
                return false;
            }

            if (config.Enabled)
            {
                ParticlesManager.RemoveParticles(config.Wildcard);
            }

            Presets.Remove(key);
            WriteConfig();

            ChatMessenger.ShowMessege(Constants.ChatMessages.PresetRemoved, MessegeType.Success);
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
