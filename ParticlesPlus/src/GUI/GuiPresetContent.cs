using ImGuiNET;
using System.Linq;

namespace ParticlesPlus.GUI
{
    internal class GuiPresetContent
    {
        private readonly ModConfig _modConfig;
        private readonly GuiSystem _guiSystem;

        private string _selectedComboKey = "";
        private string _selectedPresetKey = "";
        private PresetConfig _selectedPreset;

        public GuiPresetContent(ModConfig modConfig, GuiSystem guiSystem)
        {
            _modConfig = modConfig;
            _guiSystem = guiSystem;
            SetDefaultPreset();
        }
        public void SetDefaultPreset()
        {
            string defaultPresetKey = _modConfig.Presets.Keys.FirstOrDefault();

            if (!string.IsNullOrEmpty(defaultPresetKey))
            {
                _selectedComboKey = defaultPresetKey;
                _selectedPresetKey = defaultPresetKey;
                _selectedPreset = _modConfig.Presets[defaultPresetKey] with { };
            }
            else
            {
                _selectedComboKey = "";
                _selectedPresetKey = "";
                _selectedPreset = null;
            }
        }
        public void Draw()
        {
            string comboPlaceholder = string.IsNullOrEmpty(_selectedComboKey) ? "No Presets" : _selectedComboKey;

            ImGui.SeparatorText("Preset:");
            // Preset select combobox
            ImGui.Spacing();
            if (ImGui.BeginCombo("##Preset", comboPlaceholder))
            {
                foreach (string presetKey in _modConfig.Presets.Keys)
                {
                    bool isSelected = (_selectedComboKey == presetKey);

                    if (ImGui.Selectable(presetKey, isSelected))
                    {
                        _selectedComboKey = presetKey;
                        _selectedPresetKey = presetKey;
                        _selectedPreset = _modConfig.Presets[presetKey] with { };
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            // Add New Button
            ImGui.SameLine();
            if (ImGui.Button("Add New"))
            {
                _selectedComboKey = _modConfig.CreatePreset();
                _selectedPresetKey = _selectedComboKey;
                _selectedPreset = _modConfig.Presets[_selectedComboKey] with { };
            }
            ImGui.SeparatorText("Preset Properties:");
            if (!string.IsNullOrEmpty(_selectedComboKey))
            {
                // Name
                string presetKey = _selectedPresetKey;
                if (ImGui.InputText("Name", ref presetKey, 100))
                {
                    _selectedPresetKey = presetKey;
                }
                ImGui.Spacing();
                // Particles
                if (ImGui.BeginCombo("Particles", _selectedPreset.Particles))
                {
                    foreach (string particleKey in _modConfig.Particles.Keys)
                    {
                        bool isSelected = (_selectedPreset.Particles == particleKey);

                        if (ImGui.Selectable(particleKey, isSelected))
                        {
                            _selectedPreset = _selectedPreset with { Particles = particleKey };
                        }

                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
                ImGui.Spacing();
                // Wildcard
                string wildcard = _selectedPreset.Wildcard;
                if (ImGui.InputText("Wildcard", ref wildcard, 100))
                {
                    _selectedPreset = _selectedPreset with { Wildcard = wildcard };
                }

                ImGui.Spacing();
                // Toggle
                bool enabled = _selectedPreset.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    _selectedPreset = _selectedPreset with { Enabled = enabled };
                }

                ImGui.Spacing();
                // Save
                if (ImGui.Button("Save"))
                {
                    if (_modConfig.UpdatePreset(_selectedComboKey, _selectedPreset, _selectedPresetKey))
                    {
                        _selectedComboKey = _selectedPresetKey;
                    }
                    else
                    {
                        _selectedPresetKey = _selectedComboKey;
                    }
                }
                ImGui.SameLine();
                // Delete
                if (ImGui.Button("Delete"))
                {
                    _guiSystem.confirmModal.Show(
                        $"Delete '{_selectedPresetKey}' preset?",
                        onConfirm: () =>
                        {
                            _modConfig.RemovePreset(_selectedComboKey);
                            SetDefaultPreset();
                        });
                }
            }
            else
            {
                ImGui.Text("No preset is selected.");
            }
        }
    }
}
