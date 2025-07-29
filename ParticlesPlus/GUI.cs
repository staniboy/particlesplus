using System.Linq;
using Vintagestory.API.Client;

namespace ParticlesPlus
{
    public class MainGuiDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "particlesplus";
        private readonly ModSystem modSystem;
        private readonly ModConfig modConfig;
        private PresetConfig selectedPreset;

        public MainGuiDialog(ICoreClientAPI capi, ModConfig config, ModSystem modSystem) : base(capi)
        {
            this.modConfig = config;
            this.modSystem = modSystem;
            selectedPreset = null;
            SetupDialog();
            capi.Gui.RegisterDialog(this);
            capi.Input.RegisterHotKey(ToggleKeyCombinationCode, "Particles Plus GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);

        }
        private void SetupDialog()
        {
            int elementWidth = 300;
            int labelHeight = 16;
            int dropdownHeight = 30;
            int buttonHeight = 40;
            int spacing = 10;
            int centeredLabelY = dropdownHeight / 2 - labelHeight / 2;

            ElementBounds globalSwitchBounds = ElementBounds.Fixed(260, 8, 25, 25);

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithAlignment(EnumDialogArea.CenterMiddle);
            // Key Input
            ElementBounds keyInputLabelBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, elementWidth, labelHeight);
            ElementBounds keyInputBounds = ElementBounds.Fixed(0, 0, elementWidth, dropdownHeight).FixedUnder(keyInputLabelBounds, spacing);

            // Add New Button
            ElementBounds addNewButtonBounds = ElementBounds.Fixed(0, 0, elementWidth, buttonHeight).FixedUnder(keyInputBounds, spacing);

            // Preset Dropdown
            ElementBounds presetDropdownLabelBounds = ElementBounds.Fixed(0, 0, elementWidth, labelHeight).FixedUnder(addNewButtonBounds, spacing * 2);
            ElementBounds presetDropdownBounds = ElementBounds.Fixed(0, 0, elementWidth, dropdownHeight).FixedUnder(presetDropdownLabelBounds, spacing);

            // Particles Dropdown
            ElementBounds particlesDropdownLabelBounds = ElementBounds.Fixed(0, 0, elementWidth, labelHeight).FixedUnder(presetDropdownBounds, spacing * 2);
            ElementBounds particlesDropdownBounds = ElementBounds.Fixed(0, 0, elementWidth, dropdownHeight).FixedUnder(particlesDropdownLabelBounds, spacing);

            // Wildcard Input
            ElementBounds wildcardInputLabelBounds = ElementBounds.Fixed(0, 0, elementWidth, labelHeight).FixedUnder(particlesDropdownBounds, spacing * 2);
            ElementBounds wildcardInputBounds = ElementBounds.Fixed(0, 0, elementWidth, dropdownHeight).FixedUnder(wildcardInputLabelBounds, spacing);

            //// Switch
            ElementBounds switchBounds = ElementBounds.Fixed(0, 0, dropdownHeight, dropdownHeight).FixedUnder(wildcardInputBounds, spacing * 2);
            ElementBounds switchLabelBounds = ElementBounds.Fixed(0, centeredLabelY, elementWidth - dropdownHeight - spacing, labelHeight)
                .FixedRightOf(switchBounds, spacing)
                .FixedUnder(wildcardInputBounds, spacing * 2);

            // Save Button
            ElementBounds saveButtonBounds = ElementBounds.Fixed(0, 0, elementWidth / 2, buttonHeight).FixedUnder(switchBounds, spacing * 2);

            // Delete Button
            ElementBounds deleteButtonBounds = ElementBounds.Fixed(elementWidth / 2, 0, elementWidth / 2, buttonHeight).FixedUnder(switchBounds, spacing * 2);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren
                (
                presetDropdownLabelBounds,
                presetDropdownBounds,
                addNewButtonBounds,
                keyInputLabelBounds,
                keyInputBounds,
                particlesDropdownLabelBounds,
                particlesDropdownBounds,
                wildcardInputLabelBounds,
                wildcardInputBounds,
                switchBounds,
                switchLabelBounds,
                saveButtonBounds,
                deleteButtonBounds
                );

            SingleComposer = capi.Gui.CreateCompo("ppMainDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Particles Plus Configuration", OnTitleBarCloseClicked)
                .AddSwitch(OnGlobalSwitch, globalSwitchBounds, "globalSwitch", 18)
                .AddStaticText("Preset:", CairoFont.WhiteSmallText(), presetDropdownLabelBounds)
                .AddDropDown(GetPresetNames(), GetPresetNames(), 0, OnPresetSelection, presetDropdownBounds, "presetDropdown")
                .AddButton("Add New", OnAddNew, addNewButtonBounds)
                .AddStaticText("Preset Name:", CairoFont.WhiteSmallText(), keyInputLabelBounds)
                .AddTextInput(keyInputBounds, (string text) => { }, key: "keyInput")
                .AddStaticText("Particles:", CairoFont.WhiteSmallText(), particlesDropdownLabelBounds)
                .AddDropDown(GetParticlesNames(), GetParticlesNames(), 0, null, particlesDropdownBounds, "particlesDropdown")
                .AddStaticText("Wildcard:", CairoFont.WhiteSmallText(), wildcardInputLabelBounds)
                .AddTextInput(wildcardInputBounds, (string text) => { }, key: "wildcardInput")
                .AddSwitch(null, switchBounds, size: 30, key: "enabledSwitch")
                .AddStaticText("Enabled", CairoFont.WhiteSmallText(), switchLabelBounds)
                .AddButton("Save", OnSave, saveButtonBounds)
                .AddButton("Delete", OnDelete, deleteButtonBounds)
                .Compose();

            SingleComposer.GetSwitch("globalSwitch").SetValue(modConfig.Global);
            OnPresetSelection(GetPresetNames()[0], true);
        }
        private string[] GetPresetNames()
        {
            string[] presetNames = modConfig.Presets.Keys.ToArray();

            if (presetNames.Length == 0) return new string[] { "<none>" };

            return presetNames; 
        }
        private string[] GetParticlesNames()
        {
            return modConfig.Particles.Keys.ToArray();
        }
        private void OnPresetSelection(string code, bool selected)
        {
            if (code == "<none>")
            {
                selectedPreset = null;
                SetFormClear();
            } 
            else
            {
                selectedPreset = modConfig.Presets[code];
                SingleComposer.GetDropDown("presetDropdown").SetSelectedValue(code);
                SingleComposer.GetDropDown("particlesDropdown").SetSelectedValue(selectedPreset.Particles);
                SingleComposer.GetSwitch("enabledSwitch").SetValue(selectedPreset.Enabled);
                SingleComposer.GetTextInput("wildcardInput").SetValue(selectedPreset.Wildcard);
            }
        }
        private void SetFormClear()
        {
            SingleComposer.GetDropDown("presetDropdown").SetSelectedIndex(0);
            SingleComposer.GetDropDown("particlesDropdown").SetSelectedIndex(0);
            SingleComposer.GetSwitch("enabledSwitch").SetValue(false);
            SingleComposer.GetTextInput("wildcardInput").SetValue("");
        }
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        private bool OnAddNew()
        {
            GuiElementTextInput keyInput = SingleComposer.GetTextInput("keyInput");
            string newKeyName = keyInput.GetText();
            if (!string.IsNullOrWhiteSpace(newKeyName))
            {
                if (!modConfig.Presets.ContainsKey(newKeyName))
                {
                    modConfig.Presets[newKeyName] = new PresetConfig
                    {
                        Enabled = false,
                        Wildcard = "",
                        Particles = null
                    };
                    GuiElementDropDown presetDropdown = SingleComposer.GetDropDown("presetDropdown");
                    string[] presetNames = GetPresetNames();
                    presetDropdown.SetList(presetNames, presetNames);
                    OnPresetSelection(newKeyName, true);
                }
            }
            keyInput.SetValue("");

            return true;
        }
        private bool OnSave()
        {
            if (selectedPreset == null) return false;

            string name = SingleComposer.GetTextInput("keyInput").Text;
            string particles = SingleComposer.GetDropDown("particlesDropdown").SelectedValue;
            string wildcard = SingleComposer.GetTextInput("wildcardInput").GetText();
            bool enabled = SingleComposer.GetSwitch("enabledSwitch").On;

            if (selectedPreset.Enabled && enabled)
            {
                if (selectedPreset.Wildcard != wildcard || selectedPreset.Particles != particles)
                {
                    modSystem.RemoveParticles(selectedPreset.Wildcard);
                    modSystem.AddParticles(wildcard, modConfig.Particles[particles]); 
                }
            }

            if (selectedPreset.Enabled && !enabled)
            {
                modSystem.RemoveParticles(wildcard);
            }

            if (!selectedPreset.Enabled && enabled)
            {
                modSystem.AddParticles(wildcard, modConfig.Particles[particles]);
            }

            selectedPreset.Particles = particles;
            selectedPreset.Wildcard = wildcard;
            selectedPreset.Enabled = enabled;
            modSystem.WriteConfig();
            return true;
            
        }
        private bool OnDelete()
        {
            GuiElementDropDown presetDropdown = SingleComposer.GetDropDown("presetDropdown");
            string keyToDelete = presetDropdown.SelectedValue;

            if (keyToDelete == "<none>")
            {
                return false;
            }

            // Remove key particles
            modSystem.RemoveParticles(modConfig.Presets[keyToDelete].Wildcard);
            // Remove Key
            modConfig.Presets.Remove(keyToDelete);
            // Update Presets List for Dropdown
            string[] presetNames = GetPresetNames();
            // Set updated list to dropdown
            UpdatePresetDropdownList(presetDropdown);
            // Set form to first element in updated list if empty reset form.
            OnPresetSelection(presetNames[0], true);

            modSystem.WriteConfig();
            return true;
        }
        private void UpdatePresetDropdownList(GuiElementDropDown presetDropdown)
        {
            string[] presetNames = GetPresetNames();
            presetDropdown.SetList(presetNames, presetNames);
        }
        private void OnGlobalSwitch(bool enabled)
        {
            modSystem.ToggleParticles(!enabled);
        }
    } 
}