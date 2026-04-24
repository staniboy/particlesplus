using ImGuiNET;
using Vintagestory.API.Client;
using VSImGui;
using VSImGui.API;

namespace ParticlesPlus.GUI;

public class GuiSystem
{

    private readonly ModSystem _modSystem;
    private ICoreClientAPI API => _modSystem.API;
    private ModConfig ModConfig => _modSystem.ModConfig;
    private ImGuiModSystem ImGuiSystem => API.ModLoader.GetModSystem<ImGuiModSystem>();
    public readonly ConfirmModal confirmModal = new("Confirm##particlesplus");


    // GUI Content
    private bool _showGui = false;
    private GuiPresetContent _guiPresetContent;

    public GuiSystem(ModSystem modSystem)
    {
        _modSystem = modSystem;

        if (ImGuiSystem == null) return;

        _guiPresetContent = new(ModConfig, this);

        ImGuiSystem.Draw += DrawMenu;
        API.Input.RegisterHotKey("particlesplusgui", "Toggle Particles Plus GUI", GlKeys.P, HotkeyType.GUIOrOtherControls, ctrlPressed: false);
        API.Input.RegisterHotKey("particlesplusglobal", "Toggle Particles Plus Global", GlKeys.P, HotkeyType.GUIOrOtherControls, ctrlPressed: true);
        API.Input.SetHotKeyHandler("particlesplusgui", _ =>
        {
            _showGui = !_showGui;
            return true;
        });
        API.Input.SetHotKeyHandler("particlesplusglobal", _ =>
        {
            ModConfig.ToggleGlobal();
            return true;
        });
    }

    private CallbackGUIStatus DrawMenu(float dt)
    {
        if (!_showGui) return CallbackGUIStatus.Closed;

        var modConfig = ModConfig;

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
                    // Load Default Config
                    if (ImGui.Button("Load Default Config"))
                    {
                        confirmModal.Show(
                            "Are you sure you want to load the default config? This will overwrite your current config.",
                            onConfirm: () =>
                        {
                            modConfig.LoadDefaultModConfig();
                            _guiPresetContent.SetDefaultPreset();
                        });
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }
        ImGui.End();
        confirmModal.Draw();
        return CallbackGUIStatus.GrabMouse;
    }
}