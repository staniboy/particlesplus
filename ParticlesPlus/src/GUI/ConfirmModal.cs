using ImGuiNET;
using System;
using System.Numerics;

namespace ParticlesPlus.GUI
{
    public class ConfirmModal
    {
        private bool _open = false;
        private bool _pendingOpen = false;
        private string _title;
        private string _message;
        private Action _onConfirm;
        private Action _onCancel;

        public ConfirmModal(string title = "Confirm")
        {
            _title = title;
        }

        public void Show(string message, Action onConfirm, Action onCancel = null)
        {
            _message = message;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _pendingOpen = true;
        }

        public void Draw()
        {
            if (_pendingOpen)
            {
                ImGui.OpenPopup(_title);
                _open = true;
                _pendingOpen = false;
            }

            ImGuiViewportPtr vp = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(vp.GetCenter(), ImGuiCond.Always, new Vector2(0.5f, 0.5f));

            if (ImGui.BeginPopupModal(_title, ref _open, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            {
                ImGui.TextUnformatted(_message);
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                if (ImGui.Button("OK", new Vector2(120, 0)))
                {
                    _onConfirm?.Invoke();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    _onCancel?.Invoke();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }
        }
    }
}
