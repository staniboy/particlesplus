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

        _guiPresetContent = new(_modSystem.modConfig);

        _guiSystem.Draw += DrawMenu;
        API.Input.RegisterHotKey("particlesplusgui", "Toggle Particles Plus GUI", GlKeys.P, HotkeyType.GUIOrOtherControls, ctrlPressed: false);
        API.Input.RegisterHotKey("particlesplusglobal", "Toggle Particles Plus Global", GlKeys.P, HotkeyType.GUIOrOtherControls, ctrlPressed: true);
        API.Input.SetHotKeyHandler("particlesplusgui", _ =>
        {
            _showGui = !_showGui;
            return true;
        });
        API.Input.SetHotKeyHandler("particlesplusglobal", _ =>
        {
            modSystem.modConfig.ToggleGlobal();
            return true;
        });
    }

    private CallbackGUIStatus DrawMenu(float dt)
    {
        if (!_showGui) return CallbackGUIStatus.Closed;

        var modConfig = _modSystem.modConfig;

        if (ImGui.Begin("Particles Plus", ref _showGui, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTabBar("MyTabBar"))
            {
                if (ImGui.BeginTabItem("Presets"))
                {
                    _guiPresetContent.Draw();

                    ImGui.EndTabItem();
                }

                //if (ImGui.BeginTabItem("Particles"))
                //{
                //    ImGui.Text("Something will be here");
                //    ImGui.EndTabItem();
                //}

                if (ImGui.BeginTabItem("Options"))
                {
                    // Global Toggle
                    bool globalEnabled = modConfig.Global;
                    if (ImGui.Checkbox("Custom Particles Enabled", ref globalEnabled))
                    {
                        modConfig.ToggleGlobal();
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        return CallbackGUIStatus.GrabMouse;
    }
}