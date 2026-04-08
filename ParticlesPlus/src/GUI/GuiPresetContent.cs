using ImGuiNET;
using System.Linq;

namespace ParticlesPlus.GUI
{
    internal class GuiPresetContent
    {
        private readonly ModConfig _modConfig;

        private string _selectedComboKey = "";
        private string _selectedPresetKey = "";
        private PresetConfig _selectedPreset;

        public GuiPresetContent(ModConfig modConfig)
        {
            _modConfig = modConfig;
            SetDefaultPreset();
        }
        private void SetDefaultPreset()
        {
            string initialPresetKey = _modConfig.Presets.Keys.FirstOrDefault();

            if (!string.IsNullOrEmpty(initialPresetKey))
            {
                _selectedComboKey = initialPresetKey;
                _selectedPresetKey = initialPresetKey;
                _selectedPreset = _modConfig.Presets[initialPresetKey];
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
            if (ImGui.BeginCombo("", comboPlaceholder))
            {
                foreach (string preset in _modConfig.Presets.Keys)
                {
                    bool isSelected = (_selectedComboKey == preset);

                    if (ImGui.Selectable(preset, isSelected))
                    {
                        _selectedComboKey = preset;
                        _selectedPresetKey = preset;
                        _selectedPreset = _modConfig.Presets[preset];
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
                _selectedPreset = _modConfig.Presets[_selectedComboKey];
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
                            _selectedPreset.Particles = particleKey;
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
                    _selectedPreset.Wildcard = wildcard;
                }

                ImGui.Spacing();
                // Toggle
                bool enabled = _selectedPreset.Enabled;
                if (ImGui.Checkbox("Enabled", ref enabled))
                {
                    _selectedPreset.Enabled = enabled;
                }

                ImGui.Spacing();
                // Save
                if (ImGui.Button("Save"))
                {
                    if (_modConfig.UpdatePreset(_selectedComboKey, _selectedPreset, _selectedPresetKey) && !string.IsNullOrEmpty(_selectedPresetKey))
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
                    _modConfig.RemovePreset(_selectedComboKey);
                    SetDefaultPreset();
                }
            }
            else
            {
                ImGui.Text("No preset is selected.");
            }
        }
    }
}
