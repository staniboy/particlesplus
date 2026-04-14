using System.Linq;
using Vintagestory.API.Client;

namespace ParticlesPlus
{
    public class MainGuiDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "particlesplus";
        private readonly ModSystem _modSystem;
        private ICoreClientAPI API => _modSystem.API;
        private ModConfig ModConfig => _modSystem.ModConfig;
        private ChatMessanger ChatMessanger => _modSystem.ChatMessanger;
        private PresetConfig _selectedPreset;

        public MainGuiDialog(ModSystem modSystem) : base(modSystem.API)
        {
            this._modSystem = modSystem;
            _selectedPreset = null;

            SetupDialog();
            API.Gui.RegisterDialog(this);
            API.Input.RegisterHotKey(ToggleKeyCombinationCode, "Particles Plus GUI", GlKeys.P, HotkeyType.GUIOrOtherControls);
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

            SingleComposer = API.Gui.CreateCompo("ppMainDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Particles Plus Configuration", OnTitleBarCloseClicked)
                .AddSwitch(OnGlobalSwitch, globalSwitchBounds, "globalSwitch", 18)
                .AddStaticText("Preset:", CairoFont.WhiteSmallText(), presetDropdownLabelBounds)
                .AddDropDown(GetPresetNames(), GetPresetNames(), 0, OnPresetSelection, presetDropdownBounds, "presetDropdown")
                .AddButton("Add New", OnAddNew, addNewButtonBounds)
                .AddStaticText("Preset Name:", CairoFont.WhiteSmallText(), keyInputLabelBounds)
                .AddTextInput(keyInputBounds, (text) => { }, key: "keyInput", font: CairoFont.WhiteSmallText())
                .AddStaticText("Particles:", CairoFont.WhiteSmallText(), particlesDropdownLabelBounds)
                .AddDropDown(GetParticlesNames(), GetParticlesNames(), 0, null, particlesDropdownBounds, "particlesDropdown")
                .AddStaticText("Wildcard:", CairoFont.WhiteSmallText(), wildcardInputLabelBounds)
                .AddTextInput(wildcardInputBounds, (text) => { }, key: "wildcardInput", font: CairoFont.WhiteSmallText())
                .AddSwitch(null, switchBounds, size: 30, key: "enabledSwitch")
                .AddStaticText("Enabled", CairoFont.WhiteSmallText(), switchLabelBounds)
                .AddButton("Save", OnSave, saveButtonBounds)
                .AddButton("Delete", OnDelete, deleteButtonBounds)
                .Compose();

            SingleComposer.GetSwitch("globalSwitch").SetValue(ModConfig.Global);
            OnPresetSelection(GetPresetNames()[0], true);
        }
        private string[] GetPresetNames()
        {
            string[] presetNames = ModConfig.Presets.Keys.ToArray();

            if (presetNames.Length == 0) return new string[] { "<none>" };

            return presetNames;
        }
        private string[] GetParticlesNames()
        {
            return ModConfig.Particles.Keys.ToArray();
        }
        private void OnPresetSelection(string code, bool selected) // TODO: create a method for preset selection separate from control event handler
        {
            if (code == "<none>")
            {
                _selectedPreset = null;
                SetFormClear();
            }
            else
            {
                _selectedPreset = ModConfig.Presets[code];
                SingleComposer.GetDropDown("presetDropdown").SetSelectedValue(code);
                SingleComposer.GetDropDown("particlesDropdown").SetSelectedValue(_selectedPreset.Particles);
                SingleComposer.GetSwitch("enabledSwitch").SetValue(_selectedPreset.Enabled);
                SingleComposer.GetTextInput("wildcardInput").SetValue(_selectedPreset.Wildcard);
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

            if (string.IsNullOrWhiteSpace(newKeyName))
            {
                ChatMessanger.ShowMessage(Constants.ChatMessages.EmptyNameError, MessageType.Error);
                return false;
            }

            if (ModConfig.Presets.ContainsKey(newKeyName))
            {
                ChatMessanger.ShowMessage(Constants.ChatMessages.DuplicateNameError, MessageType.Error);
                return false;
            }

            ModConfig.Presets[newKeyName] = new PresetConfig
            {
                Enabled = false,
                Wildcard = "",
                Particles = null
            };

            GuiElementDropDown presetDropdown = SingleComposer.GetDropDown("presetDropdown");
            string[] presetNames = GetPresetNames();
            presetDropdown.SetList(presetNames, presetNames);
            OnPresetSelection(newKeyName, true);
            ModConfig.WriteConfig();
            keyInput.SetValue("");
            ChatMessanger.ShowMessage(Constants.ChatMessages.PresetAdded, MessageType.Success);
            return true;
        }
        private bool OnSave()
        {
            if (_selectedPreset == null) return false;

            // string name = SingleComposer.GetTextInput("keyInput").Text;
            GuiElementDropDown presetDropdown = SingleComposer.GetDropDown("presetDropdown");
            GuiElementDropDown particlesDropdown = SingleComposer.GetDropDown("particlesDropdown");
            GuiElementTextInput wildcardInput = SingleComposer.GetTextInput("wildcardInput");
            GuiElementSwitch enabledSwitch = SingleComposer.GetSwitch("enabledSwitch");

            string preset = presetDropdown.SelectedValue;
            string particles = particlesDropdown.SelectedValue;
            string wildcard = wildcardInput.GetText();
            bool enabled = enabledSwitch.On;

            if (!RegexValidator.IsValidRegex(wildcard))
            {
                OnPresetSelection(preset, true);
                ChatMessanger.ShowMessage(Constants.ChatMessages.RegexError, MessageType.Error);
                return false;
            }

            PresetConfig updatedPreset = new PresetConfig()
            {
                Enabled = enabled,
                Particles = particles,
                Wildcard = wildcard
            };

            ModConfig.UpdatePreset(preset, updatedPreset);
            ChatMessanger.ShowMessage(Constants.ChatMessages.PresetSaved, MessageType.Success);
            return true;

        }
        private bool OnDelete()
        {
            GuiElementDropDown presetDropdown = SingleComposer.GetDropDown("presetDropdown");
            string keyToDelete = presetDropdown.SelectedValue;

            ModConfig.RemovePreset(keyToDelete);

            string[] presetNames = GetPresetNames();
            // Set updated list to dropdown
            UpdatePresetDropdownList(presetDropdown);
            // Set form to first element in updated list if empty reset form.
            OnPresetSelection(presetNames[0], true);


            ChatMessanger.ShowMessage(Constants.ChatMessages.PresetRemoved, MessageType.Success);
            return true;
        }
        private void UpdatePresetDropdownList(GuiElementDropDown presetDropdown)
        {
            string[] presetNames = GetPresetNames();
            presetDropdown.SetList(presetNames, presetNames);
        }
        private void OnGlobalSwitch(bool enabled)
        {
            ModConfig.SetGlobal(enabled);
        }
    }
}