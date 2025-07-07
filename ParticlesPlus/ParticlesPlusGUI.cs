using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace ParticlesPlus
{
    public class MainGuiDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "particlesplus";
        private readonly ModConfig modConfig;

        public MainGuiDialog(ICoreClientAPI capi, ModConfig config) : base(capi)
        {
            modConfig = config;
            SetupDialog();
            capi.Gui.RegisterDialog(this);
            capi.Input.RegisterHotKey(ToggleKeyCombinationCode, "Particles Plus GUI", GlKeys.U, HotkeyType.GUIOrOtherControls);

        }
        private void SetupDialog()
        {
            int insetWidth = 900;
            int insetHeight = 300;
            int insetDepth = 3;
            int rowHeight = 30;
            int rowCount = 0;

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Bounds of main inset for scrolling content in the GUI
            ElementBounds insetBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight, insetWidth, insetHeight);
            ElementBounds scrollbarBounds = insetBounds.RightCopy().WithFixedWidth(20);

            // Create child elements bounds for within the inset
            ElementBounds clipBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerBounds = insetBounds.ForkContainingChild(GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding, GuiStyle.HalfPadding);
            ElementBounds containerRowBounds = ElementBounds.Fixed(0, 0, insetWidth, rowHeight).WithFixedMargin(10);

            // Dialog background bounds
            ElementBounds bgBounds = ElementBounds.Fill
                .WithFixedPadding(GuiStyle.ElementToDialogPadding)
                .WithSizing(ElementSizing.FitToChildren)
                .WithChildren(insetBounds, scrollbarBounds);

            // Create the dialog
            SingleComposer = capi.Gui.CreateCompo("demoScrollGui", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Particles Plus", OnTitleBarCloseClicked)
                .BeginChildElements()
                    .AddInset(insetBounds, insetDepth)
                    .BeginClip(clipBounds)
                        .AddContainer(containerBounds, "scroll-content")
                    .EndClip()
                    .AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
                .EndChildElements();



            // Add desired scrollable content to the container
            GuiElementContainer scrollArea = SingleComposer.GetContainer("scroll-content");
            foreach (var entry in modConfig.Presets)
            {
                scrollArea.Add(new GuiElementSwitch(capi, (bool boo) => { }, containerRowBounds));
                scrollArea.Add(new GuiElementTextButton(capi, entry.Key, CairoFont.SmallButtonText(), CairoFont.WhiteSmallText(), () => { return true; }, containerRowBounds.RightCopy()));
                containerRowBounds = containerRowBounds.BelowCopy();
                rowCount++;
                /*                scrollArea.Add(new GuiElementSwitch(capi, (bool boo) => { }, containerRowBounds.FlatCopy()));
                                scrollArea.Add(new GuiElementTextButton(capi, entry.Key, CairoFont.SmallButtonText(), CairoFont.WhiteSmallText(), () => { return true; }, containerRowBounds.FlatCopy().WithFixedOffset(40,0)));
                                containerRowBounds = containerRowBounds.BelowCopy();
                                rowCount++;*/
            }


            // Compose the dialog
            SingleComposer.Compose();

            // After composing dialog, need to set the scrolling area heights to enable scroll behavior
            float scrollVisibleHeight = (float)clipBounds.fixedHeight;
            float scrollTotalHeight = rowHeight * rowCount;
            SingleComposer.GetScrollbar("scrollbar").SetHeights(scrollVisibleHeight, scrollTotalHeight);

        }
        private void OnNewScrollbarValue(float value)
        {
            ElementBounds bounds = SingleComposer.GetContainer("scroll-content").Bounds;
            bounds.fixedY = 5 - value;
            bounds.CalcWorldBounds();
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
    } 
}