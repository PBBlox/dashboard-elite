﻿using System.Threading;
using dashboard_elite.Audio;
using dashboard_elite.EliteData;
using dashboard_elite.Helpers;
using dashboard_elite.JsInterop;
using dashboard_elite.Services;
using EliteJournalReader.Events;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace dashboard_elite.Components.Buttons
{
    public partial class ToggleButton
    {
        [Inject] private NavigationManager NavigationManager { get; set; }

        [Inject] private SvgCacheService SvgCacheService { get; set; }

        [Parameter] public Data Data { get; set; }

        [Parameter] public ButtonData ButtonData { get; set; }

        private string PrimaryIcon => SvgCacheService.ButtonIcon(ButtonData.PrimaryIcon); // off

        private string SecondaryIcon => SvgCacheService.ButtonIcon(ButtonData.SecondaryIcon); // on

        private CachedSound _clickSound = null;

        private MudTripleIconButtonState ToggleState
        {
            get
            {
                var isOn = false;

                switch (ButtonData.Function.ToLower())
                {
                    case "focuscommspanel":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.CommsPanel;
                        break;
                    case "focusradarpanel":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.RolePanel;
                        break;
                    case "focusrightpanel":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.InternalPanel;
                        break;
                    case "focusleftpanel":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.ExternalPanel;
                        break;

                    case "galaxymap":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.GalaxyMap;
                        break;
                    case "systemmap":
                        isOn = Data.StatusData.GuiFocus == StatusGuiFocus.SystemMap ||
                               Data.StatusData.GuiFocus == StatusGuiFocus.Orrery;
                        break;

                    case "togglecargoscoop":
                        isOn = Data.StatusData.CargoScoopDeployed;
                        break;
                    case "landinggeartoggle":
                        isOn = Data.StatusData.LandingGearDown;
                        break;

                    case "toggleflightassist":
                        isOn = !Data.StatusData.FlightAssistOff;
                        break;

                    case "shipspotlighttoggle":
                        isOn = Data.StatusData.LightsOn || Data.StatusData.SrvHighBeam;
                        break;

                    case "nightvisiontoggle":
                        isOn = Data.StatusData.NightVision;
                        break;

                    case "playerhudmodetoggle":
                        isOn = Data.StatusData.HudInAnalysisMode;
                        break;

                    case "deployhardpointtoggle":
                        isOn = Data.StatusData.HardpointsDeployed;
                        break;

                    case "supercruise":
                        isOn = Data.StatusData.Supercruise;
                        break;

                    case "togglebuttonupinput":
                        isOn = Data.StatusData.SilentRunning;
                        break;

                    case "togglebuggyturretbutton":
                        isOn = Data.StatusData.SrvTurret;
                        break;

                    case "toggledriveassist":
                        isOn = Data.StatusData.SrvDriveAssist;
                        break;

                    case "autobreakbuggybutton":
                        isOn = Data.StatusData.SrvHandbrake;
                        break;
                }

                return isOn ? MudTripleIconButtonState.Secondary : MudTripleIconButtonState.Primary;
            }
        }

        private void ButtonClick()
        {
            var focusChange = NavigationManager.Uri.Contains("127.0.0.1");

            if (focusChange)
            {
                InteropMouse.JsMouseUp();

                Thread.Sleep(100);
            }

            Common.PlaySound(ref _clickSound, ButtonData.ClickSound);

            switch (ButtonData.Function.ToLower())
            {
                case "focuscommspanel":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].FocusCommsPanel_Buggy, focusChange);
                    else if (Data.StatusData.OnFoot)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.OnFoot].FocusCommsPanel_Humanoid, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].FocusCommsPanel, focusChange);
                    break;
                case "focusleftpanel":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].FocusLeftPanel_Buggy, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].FocusLeftPanel, focusChange);
                    break;
                case "focusradarpanel":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].FocusRadarPanel_Buggy, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].FocusRadarPanel, focusChange);
                    break;
                case "focusrightpanel":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].FocusRightPanel_Buggy, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].FocusRightPanel, focusChange);
                    break;

                case "galaxymap":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].GalaxyMapOpen_Buggy, focusChange);
                    else if (Data.StatusData.OnFoot)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.OnFoot].GalaxyMapOpen_Humanoid, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].GalaxyMapOpen, focusChange);
                    break;
                case "systemmap":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].SystemMapOpen_Buggy, focusChange);
                    else if (Data.StatusData.OnFoot)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.OnFoot].SystemMapOpen_Humanoid, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].SystemMapOpen, focusChange);
                    break;

                case "togglecargoscoop":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].ToggleCargoScoop_Buggy, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].ToggleCargoScoop, focusChange);
                    break;
                case "landinggeartoggle":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].LandingGearToggle, focusChange);
                    break;

                case "toggleflightassist":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].ToggleFlightAssist, focusChange);
                    break;

                case "shipspotlighttoggle":
                    if (Data.StatusData.InSRV)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].HeadlightsBuggyButton, focusChange);
                    else if (Data.StatusData.OnFoot)
                        CommandTools.SendKeypressQueue(
                            Common.Binding[BindingType.OnFoot].HumanoidToggleFlashlightButton, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].ShipSpotLightToggle, focusChange);
                    break;

                case "nightvisiontoggle":
                    if (Data.StatusData.OnFoot)
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.OnFoot]
                            .HumanoidToggleNightVisionButton, focusChange);
                    else
                        CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].NightVisionToggle, focusChange);
                    break;

                case "playerhudmodetoggle":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].PlayerHUDModeToggle, focusChange);
                    break;

                case "deployhardpointtoggle":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].DeployHardpointToggle, focusChange);
                    break;

                case "supercruise":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].Supercruise, focusChange);
                    break;

                case "togglebuttonupinput":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Ship].ToggleButtonUpInput, focusChange);
                    break;

                case "togglebuggyturretbutton":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].ToggleBuggyTurretButton, focusChange);
                    break;

                case "toggledriveassist":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].ToggleDriveAssist, focusChange);
                    break;

                case "autobreakbuggybutton":
                    CommandTools.SendKeypressQueue(Common.Binding[BindingType.Srv].AutoBreakBuggyButton, focusChange);
                    break;
            }

        }
    }

}
