﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using dashboard_elite.Hubs;
using dashboard_elite.Services;
using EliteJournalReader.Events;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Serilog;
//using System.Windows.Forms;

namespace dashboard_elite.EliteData
{
    public class History
    {
        private readonly IHubContext<MyHub> _myHub;
        private readonly ButtonCacheService _buttonCacheService;
        private readonly ProfileCacheService _profileCacheService;
        private readonly Material _material;

        public History(IHubContext<MyHub> myHub, ButtonCacheService buttonCacheService, ProfileCacheService profileCacheService, Material material)
        {
            _myHub = myHub;
            _buttonCacheService = buttonCacheService;
            _profileCacheService = profileCacheService;
            _material = material;
        }

        // 1478 x 1125

        public const double SpaceMinXL = -41715.0;
        public const double SpaceMaxXL = 41205.0;
        public const double SpaceMinZL = -19737.0;
        public const double SpaceMaxZL = 68073.0;

        // 1125 x 1478

        public const double SpaceMinXP = -31812.8;
        public const double SpaceMaxXP = 31302.85;
        public const double SpaceMinZP = -33513.4;
        public const double SpaceMaxZP = 81849.41;

        public class FSDJumpInfo
        {
            public bool Taxi { get; set; }
            public string StarSystem { get; set; }
            public List<double> StarPos { get; set; }
        }

        public class CarrierJumpInfo
        {
            public bool Taxi { get; set; }

            public bool Docked { get; set; }
            public string StarSystem { get; set; }
            public List<double> StarPos { get; set; }
        }

        public List<PointF> TravelHistoryPointsL = new List<PointF>();
        public List<PointF> TravelHistoryPointsP = new List<PointF>();

        public class LocationInfo
        {
            public bool Taxi { get; set; }
            public bool OnFoot { get; set; }

            //"Docked":true, "StationName":"Jameson Memorial", "StarSystem":"Shinrarta Dezhra",  "StarPos":[55.71875,17.59375,27.15625], 

            public bool Docked { get; set; }
            public string StationName { get; set; }
            public string StarSystem { get; set; }
            public List<double> StarPos { get; set; }

        }

        public class DockedInfo
        {
            public bool Taxi { get; set; }

            // "StationName":"Jameson Memorial", "StarSystem":"Shinrarta Dezhra"

            public string StationName { get; set; }
            public string StarSystem { get; set; }
        }

        public Dictionary<string, List<double>> VisitedSystemList = new Dictionary<string, List<double>>();

        public double GalaxyImageLWidth { get; set; }
        public double GalaxyImageLHeight { get; set; }
        public double GalaxyImagePWidth { get; set; }
        public double GalaxyImagePHeight { get; set; }

        private class UnsafeNativeMethods
        {
            [DllImport("Shell32.dll")]
            public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags,
                IntPtr hToken, out IntPtr ppszPath);
        }


        /// <summary>
        /// The standard Directory of the Player Journal files (C:\Users\%username%\Saved Games\Frontier Developments\Elite Dangerous).
        /// </summary>
        private DirectoryInfo StandardDirectory
        {
            get
            {
                //#if DEBUG
                //return new DirectoryInfo(@"C:\Users\Marcel\Desktop\Elite Dangerous");
                //#endif
                var result = UnsafeNativeMethods.SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"),
                    0, new IntPtr(0), out var path);
                if (result >= 0)
                {
                    try
                    {
                        return new DirectoryInfo(Marshal.PtrToStringUni(path) +
                                                 @"\Frontier Developments\Elite Dangerous");
                    }
                    catch
                    {
                        return new DirectoryInfo(Directory.GetCurrentDirectory());
                    }
                }
                else
                {
                    return new DirectoryInfo(Directory.GetCurrentDirectory());
                }
            }
        }

        public void AddTravelPos(List<double> starPos)
        {
            if (starPos?.Count == 3)
            {
                var x = starPos[0];
                var y = starPos[1];
                var z = starPos[2];

                var imgX = (x - SpaceMinXL) / (SpaceMaxXL - SpaceMinXL) * GalaxyImageLWidth;
                var imgY = (SpaceMaxZL - z) / (SpaceMaxZL - SpaceMinZL) * GalaxyImageLHeight;

                TravelHistoryPointsL.Add(new PointF
                {
                    X = (float) imgX,
                    Y = (float) imgY
                });

                imgX = (x - SpaceMinXP) / (SpaceMaxXP - SpaceMinXP) * GalaxyImagePWidth;
                imgY = (SpaceMaxZP - z) / (SpaceMaxZP - SpaceMinZP) * GalaxyImagePHeight;

                TravelHistoryPointsP.Add(new PointF
                {
                    X = (float)imgX,
                    Y = (float)imgY
                });
            }
        }


        public string GetEliteHistory(string defaultFilter, Data data, Ships ships, Module module)
        {
            var journalDirectory = StandardDirectory;

            if (!Directory.Exists(journalDirectory.FullName))
            {
				Log.Logger.Error($"Directory {journalDirectory.FullName} not found.");
                return null;
            }

            try
            {
                var journalFiles = journalDirectory.GetFiles(defaultFilter).OrderBy(x => x.LastWriteTime);

                string lastJumpedSystem = string.Empty;
                string lastJumpedSettlement = string.Empty;

                foreach (var journalFile in journalFiles)
                {
                    using (var fileStream =
                        journalFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var streamReader = new StreamReader(fileStream))
                        {
                            while (!streamReader.EndOfStream)
                            {
                                try
                                {
                                    var json = streamReader.ReadLine();

                                    if (json?.Contains("\"event\":\"CarrierJump\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<CarrierJumpInfo>(json);

                                        if (info.Docked)
                                        {
                                            if (info.StarPos?.Count == 3)
                                            {
                                                AddTravelPos(info.StarPos);
                                                lastJumpedSystem = info.StarSystem;
                                            }

                                            if (!info.Taxi)
                                            {
                                                ships.HandleShipFsdJump(info.StarSystem, info.StarPos.ToList());
                                            }
                                        }
                                    }
                                    else
                                    if (json?.Contains("\"event\":\"FSDJump\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<FSDJumpInfo>(json);

                                        if (info.StarPos?.Count == 3)
                                        {
                                            AddTravelPos(info.StarPos);
                                            lastJumpedSystem = info.StarSystem;
                                        }

                                        if (!info.Taxi)
                                        {
                                            ships.HandleShipFsdJump(info.StarSystem, info.StarPos.ToList());
                                        }
                                    }
                                    else if (json?.Contains("\"event\":\"LoadGame\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<LoadGameEvent.LoadGameEventArgs>(json);

                                        if (!string.IsNullOrEmpty(info.Ship))
                                        {
                                            ships.HandleLoadGame(info.ShipID, info.Ship, info.ShipName);
                                        }
                                    }
                                    else if (json?.Contains("\"event\":\"EngineerProgress\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<EngineerProgressEvent.EngineerProgressEventArgs>(json);

                                        data.HandleEngineerProgressEvent(info);
                                    }
                                    else if (json?.Contains("\"event\":\"SetUserShipName\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<SetUserShipNameEvent.SetUserShipNameEventArgs>(json);

                                        ships.HandleSetUserShipName(info.ShipID, info.UserShipName, info.Ship);
                                    }
                                    else if (json?.Contains("\"event\":\"HullDamage\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<HullDamageEvent.HullDamageEventArgs>(json);

                                        ships.HandleHullDamage(info.Health);
                                    }
                                    else if (json?.Contains("\"event\":\"Location\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<LocationInfo>(json);

                                        if (!info.OnFoot && !info.Taxi && info.Docked)
                                        {
                                            ships.HandleShipLocation(info.StarSystem, info.StationName,
                                                info.StarPos);
                                        }
                                    }
                                    else if (json?.Contains("\"event\":\"Docked\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<DockedInfo>(json);

                                        if (!info.Taxi)
                                        {
                                            ships.HandleShipDocked(info.StarSystem, info.StationName);
                                        }
                                    }
                                    else if (json?.Contains("\"event\":\"ShipyardNew\",") == true)
                                    {
                                        var info =
                                            JsonConvert.DeserializeObject<ShipyardNewEvent.ShipyardNewEventArgs>(
                                                json);

                                        ships.HandleShipyardNew(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ShipyardBuy\",") == true)
                                    {
                                        var info =
                                            JsonConvert.DeserializeObject<ShipyardBuyEvent.ShipyardBuyEventArgs>(
                                                json);

                                        ships.HandleShipyardBuy(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ShipyardSell\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ShipyardSellEvent.ShipyardSellEventArgs>(json);

                                        ships.HandleShipyardSell(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ShipyardSwap\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ShipyardSwapEvent.ShipyardSwapEventArgs>(json);

                                        ships.HandleShipyardSwap(info);
                                    }
                                    else if (json?.Contains("\"event\":\"StoredShips\",") == true)
                                    {
                                        var info =
                                            JsonConvert.DeserializeObject<StoredShipsEvent.StoredShipsEventArgs>(
                                                json);

                                        ships.HandleStoredShips(info);
                                    }
                                    /*else if (json?.Contains("\"event\":\"ShipyardTransfer\",") == true)
                                    {
                                        var info = JsonConvert.DeserializeObject<ShipyardTransferEvent.ShipyardTransferEventArgs>(json);
                                    }*/
                                    else if (json?.Contains("\"event\":\"Loadout\",") == true)
                                    {
                                        var info =
                                            JsonConvert.DeserializeObject<LoadoutEvent.LoadoutEventArgs>(json);

                                        ships.HandleLoadout(info);
                                        module.HandleLoadout(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleBuy\",") == true)
                                    {
                                        var info =
                                            JsonConvert.DeserializeObject<ModuleBuyEvent.ModuleBuyEventArgs>(json);

                                        module.HandleModuleBuy(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleSell\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ModuleSellEvent.ModuleSellEventArgs>(json);

                                        module.HandleModuleSell(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleSellRemote\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ModuleSellRemoteEvent.ModuleSellRemoteEventArgs>(
                                                json);

                                        module.HandleModuleSellRemote(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleStore\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ModuleStoreEvent.ModuleStoreEventArgs>(json);

                                        module.HandleModuleStore(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleSwap\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ModuleSwapEvent.ModuleSwapEventArgs>(json);

                                        module.HandleModuleSwap(info);
                                    }
                                    else if (json?.Contains("\"event\":\"ModuleRetrieve\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ModuleRetrieveEvent.ModuleRetrieveEventArgs>(json);

                                        module.HandleModuleRetrieve(info);
                                    }
                                    else if (json?.Contains("\"event\":\"MassModuleStore\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<MassModuleStoreEvent.MassModuleStoreEventArgs>(json);

                                        module.HandleMassModuleStore(info);
                                    }
                                    else if (json?.Contains("\"event\":\"FetchRemoteModule\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<FetchRemoteModuleEvent.FetchRemoteModuleEventArgs>(json);

                                        module.HandleFetchRemoteModule(info);
                                    }
                                    else if (json?.Contains("\"event\":\"StoredModules\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<StoredModulesEvent.StoredModulesEventArgs>(json);

                                        module.HandleStoredModules(info);
                                    }

                                    /*else if (json?.Contains("\"event\":\"MissionCompleted\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<MissionCompletedEvent.MissionCompletedEventArgs>(json);

                                    }*/
                                    else if (json?.Contains("\"event\":\"MaterialCollected\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<MaterialCollectedEvent.MaterialCollectedEventArgs>(json);

                                        if (!string.IsNullOrEmpty(lastJumpedSystem))
                                        {
                                            var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(info.Name.ToLower())).Trim();

                                            // { "timestamp":"2018-08-11T15:29:20Z", "event":"MaterialCollected", "Category":"Encoded", "Name":"shielddensityreports", "Name_Localised":"Untypical Shield Scans ", "Count":3 }

                                            _material.AddHistory(name, lastJumpedSystem, info.Count);
                                        }
                                    }
                                    else if (json?.Contains("\"event\":\"ApproachSettlement\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<ApproachSettlementEvent.ApproachSettlementEventArgs>(json);

                                        lastJumpedSettlement = info.BodyName + "@" + info.Name;

                                    }

                                    else if (json?.Contains("\"event\":\"CollectItems\",") == true)
                                    {
                                        var info = JsonConvert
                                            .DeserializeObject<CollectItemsEvent.CollectItemsEventArgs>(json);

                                        if (!string.IsNullOrEmpty(lastJumpedSettlement))
                                        {
                                            var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(info.Name.ToLower())).Trim();

                                            // { "timestamp":"2021-06-11T16:24:21Z", "event":"CollectItems", "Name":"syntheticpathogen", "Name_Localised":"Synthetic Pathogen", "Type":"Item", "OwnerID":0, "Count":1, "Stolen":false }

                                            _material.AddHistory(name, lastJumpedSettlement, info.Count);
                                        }
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Log.Logger.Error(ex.ToString());
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
            }

            return journalDirectory.FullName;
        }
    }
}

