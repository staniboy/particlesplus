using ImGuiNET;
using System.Linq;
using Vintagestory.API.Client;
using VSImGui;
using VSImGui.API;

namespace ParticlesPlus;

public class GuiSystem
{
    private bool _showGui = false;
    private readonly ModSystem _modSystem;
    private readonly ImGuiModSystem _guiSystem;
    private ICoreClientAPI API => _modSystem.capi;

    private string _selectedComboKey = "";
    private string _selectedPresetKey = "";
    private PresetConfig _selectedPreset;

    public GuiSystem(ModSystem modSystem)
    {
        _modSystem = modSystem;
        _guiSystem = API.ModLoader.GetModSystem<ImGuiModSystem>();

        if (_guiSystem == null) return;

        _guiSystem.Draw += DrawMenu;

        API.Input.RegisterHotKey("togglemenu", "Toggle Mod Menu", GlKeys.U, HotkeyType.GUIOrOtherControls, ctrlPressed: false);
        API.Input.SetHotKeyHandler("togglemenu", _ =>
        {
            _showGui = !_showGui;
            return true;
        });
        SetDefaultPreset();
    }

    private void SetDefaultPreset()
    {
        string initialPresetKey = _modSystem.ModConfig.Presets.Keys.FirstOrDefault();

        if (!string.IsNullOrEmpty(initialPresetKey))
        {
            _selectedComboKey = initialPresetKey;
            _selectedPresetKey = initialPresetKey;
            _selectedPreset = _modSystem.ModConfig.Presets[initialPresetKey];
        }
        else
        {
            _selectedComboKey = "";
            _selectedPresetKey = "";
            _selectedPreset = null;
        }
    }
    private CallbackGUIStatus DrawMenu(float dt)
    {
        if (!_showGui) return CallbackGUIStatus.Closed;

        var modConfig = _modSystem.ModConfig;

        if (ImGui.Begin("Particles Plus", ref _showGui, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Spacing();
            // Global Toggle
            bool globalEnabled = modConfig.Global;
            if (ImGui.Checkbox("Custom Particles Enabled", ref globalEnabled))
            {
                modConfig.SetGlobal(!modConfig.Global);
            }

            string comboPlaceholder = string.IsNullOrEmpty(_selectedComboKey) ? "No Presets" : _selectedComboKey;

            ImGui.SeparatorText("Preset:");
            // Preset select combobox
            ImGui.Spacing();
            if (ImGui.BeginCombo("", comboPlaceholder))
            {
                foreach (string preset in modConfig.Presets.Keys)
                {
                    bool isSelected = (_selectedComboKey == preset);

                    if (ImGui.Selectable(preset, isSelected))
                    {
                        _selectedComboKey = preset;
                        _selectedPresetKey = preset;
                        _selectedPreset = modConfig.Presets[preset];
                    }

                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            // Add New Button
            ImGui.SameLine();
            if (ImGui.Button("Add New"))
            {
                _selectedComboKey = modConfig.CreatePreset();
                _selectedPresetKey = _selectedComboKey;
                _selectedPreset = modConfig.Presets[_selectedComboKey];
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
                    foreach (string particleKey in modConfig.Particles.Keys)
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
                    if (modConfig.UpdatePreset(_selectedComboKey, _selectedPreset, _selectedPresetKey) && !string.IsNullOrEmpty(_selectedPresetKey))
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
                    modConfig.RemovePreset(_selectedComboKey);
                    SetDefaultPreset();
                }
            }
            else
            {
                ImGui.Text("No preset is selected.");
            }
        }
        ImGui.End();
        return CallbackGUIStatus.GrabMouse;
    }
}