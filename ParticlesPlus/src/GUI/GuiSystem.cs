using ImGuiNET;
using Vintagestory.API.Client;
using VSImGui;
using VSImGui.API;

namespace ParticlesPlus.GUI;

public class GuiSystem
{
    private bool _showGui = false;
    private readonly ModSystem _modSystem;
    private readonly ImGuiModSystem _guiSystem;
    private ICoreClientAPI API => _modSystem.capi;

    // GUI Content
    private GuiPresetContent _guiPresetContent;

    public GuiSystem(ModSystem modSystem)
    {
        _modSystem = modSystem;
        _guiSystem = API.ModLoader.GetModSystem<ImGuiModSystem>();

        if (_guiSystem == null) return;

        _guiPresetContent = new(_modSystem.ModConfig);

        _guiSystem.Draw += DrawMenu;
        API.Input.RegisterHotKey("togglemenu", "Toggle Mod Menu", GlKeys.U, HotkeyType.GUIOrOtherControls, ctrlPressed: false);
        API.Input.SetHotKeyHandler("togglemenu", _ =>
        {
            _showGui = !_showGui;
            return true;
        });
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

            _guiPresetContent.Draw();

        }
        ImGui.End();
        return CallbackGUIStatus.GrabMouse;
    }
}