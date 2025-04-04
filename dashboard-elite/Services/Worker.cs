﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Serialization;
using dashboard_elite.EliteData;
using dashboard_elite.Helpers;
using dashboard_elite.Hubs;
using EliteJournalReader;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Material = dashboard_elite.EliteData.Material;

namespace dashboard_elite
{
    public class KeyBindingFileEvent : EventArgs
    {

    }

    public class KeyBindingWatcher : FileSystemWatcher
    {
        public event EventHandler KeyBindingUpdated;

        protected KeyBindingWatcher()
        {

        }

        public KeyBindingWatcher(string path, string fileName)
        {
            Filter = fileName;
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            Path = path;
        }

        public virtual void StartWatching()
        {
            if (EnableRaisingEvents)
            {
                return;
            }

            Changed -= UpdateStatus;
            Changed += UpdateStatus;

            EnableRaisingEvents = true;
        }

        public virtual void StopWatching()
        {
            try
            {
                if (EnableRaisingEvents)
                {
                    Changed -= UpdateStatus;

                    EnableRaisingEvents = false;
                }
            }
            catch (Exception e)
            {
                Log.Error($"Error while stopping Status watcher: {e.Message}");
                Log.Error(e.StackTrace);
            }
        }

        protected void UpdateStatus(object sender, FileSystemEventArgs e)
        {
            Thread.Sleep(50);

            KeyBindingUpdated?.Invoke(this, EventArgs.Empty);
        }


    }


    public class Worker : BackgroundService
    {
        private readonly IHubContext<MyHub> _myHub;
        private readonly Data _data;
        private readonly Galnet _galnet;
        private readonly Poi _poi;
        private readonly HWInfo _hwinfo;
        private readonly Ships _ships;
        private readonly EliteData.Module _module;
        private readonly History _history;
        private readonly Engineer _engineer;
        private readonly Material _material;

        public Worker(IHubContext<MyHub> myHub, Data data, Galnet galnet, Poi poi, HWInfo hwinfo, History history, Ships ships, EliteData.Module module, Engineer engineer, Material material)
        {
            _myHub = myHub;
            _data = data;
            _galnet = galnet;
            _poi = poi;
            _hwinfo = hwinfo;
            _history = history;
            _ships = ships;
            _module= module;
            _engineer = engineer;
            _material = material;
        }

        public static FifoExecution KeyWatcherJob = new FifoExecution();

        public static KeyBindingWatcher KeyBindingWatcherStartPreset;
        public static StatusWatcher StatusWatcher;
        public static CargoWatcher CargoWatcher;
        public static JournalWatcher JournalWatcher;

        public static NavRouteWatcher NavRouteWatcher;
        public static BackPackWatcher BackPackWatcher;
        public static ShipLockerWatcher ShipLockerWatcher;

        public static Task JsonTask;
        private static CancellationTokenSource _jsonTokenSource = new CancellationTokenSource();

        public static Task HWInfoTask;
        private static CancellationTokenSource _hwInfoTokenSource = new CancellationTokenSource();


        public static KeyBindingWatcher[] KeyBindingWatcher = new KeyBindingWatcher[4];

        private class UnsafeNativeMethods
        {
            [DllImport("Shell32.dll")]
            public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
        }

        /// <summary>
        /// The standard Directory of the Player Journal files (C:\Users\%username%\Saved Games\Frontier Developments\Elite Dangerous).
        /// </summary>
        public static DirectoryInfo StandardDirectory
        {
            get
            {
                int result = UnsafeNativeMethods.SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out IntPtr path);
                if (result >= 0)
                {
                    try { return new DirectoryInfo(Marshal.PtrToStringUni(path) + @"\Frontier Developments\Elite Dangerous"); }
                    catch { return new DirectoryInfo(Directory.GetCurrentDirectory()); }
                }
                else
                {
                    return new DirectoryInfo(Directory.GetCurrentDirectory());
                }
            }
        }

        public static void HandleKeyBindingEvents(object sender, object evt)
        {
            Log.Information( $"Reloading Key Bindings");

            KeyWatcherJob.QueueUserWorkItem(GetKeyBindings, null);
        }

        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                stream?.Close();
            }

            //file is not locked
            return false;
        }


        // copied from https://github.com/MagicMau/EliteJournalReader

        private static FileInfo FileInfo(string cargoPath)
        {
            try
            {
                var info = new FileInfo(cargoPath);
                if (info.Exists)
                {
                    // This info can be cached so force a refresh
                    info.Refresh();
                }
                return info;
            }
            catch { return null; }
        }


        // copied from https://github.com/MagicMau/EliteJournalReader
        public static string[] ReadStartPreset(string startPresetPath)
        {
            try
            {
                Thread.Sleep(100);

                FileInfo fileInfo = null;
                try
                {
                    fileInfo = FileInfo(startPresetPath);
                }
                catch (NotSupportedException)
                {
                    // do nothing
                }

                if (fileInfo != null)
                {
                    var maxTries = 6;
                    while (IsFileLocked(fileInfo))
                    {
                        Thread.Sleep(100);
                        maxTries--;
                        if (maxTries == 0)
                        {
                            return null;
                        }
                    }

                    using (var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        fs.Seek(0, SeekOrigin.Begin);
                        var bindsNames = reader.ReadToEnd();

                        if (string.IsNullOrEmpty(bindsNames))
                        {
                            return null;
                        }
                        return bindsNames.Split('\n');

                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            return null;
        }

        public static void HandleKeyBinding(BindingType bindingType, string bindingsPath, string bindsName)
        {
            Log.Information("handle key binding " + bindsName);

            if (KeyBindingWatcher[(int)bindingType] != null)
            {
                KeyBindingWatcher[(int)bindingType].StopWatching();
                KeyBindingWatcher[(int)bindingType].Dispose();
                KeyBindingWatcher[(int)bindingType] = null;
            }

            var fileName = Path.Combine(bindingsPath, bindsName + ".4.0.binds");

            if (!File.Exists(fileName))
            {
                Log.Error("file not found " + fileName);

                fileName = fileName.Replace(".4.0.binds", ".3.0.binds");

                if (!File.Exists(fileName))
                {
                    fileName = fileName.Replace(".3.0.binds", ".binds");

                    if (!File.Exists(fileName))
                    {
                        Log.Error("file not found " + fileName);
                    }
                }
            }

            // steam
            if (!File.Exists(fileName))
            {
                bindingsPath = SteamPath.FindSteamEliteDirectory();

                if (!string.IsNullOrEmpty(bindingsPath))
                {
                    fileName = Path.Combine(bindingsPath, bindsName + ".4.0.binds");

                    if (!File.Exists(fileName))
                    {
                        Log.Error("steam file not found " + fileName);

                        fileName = fileName.Replace(".4.0.binds", ".3.0.binds");

                        if (!File.Exists(fileName))
                        {
                            fileName = fileName.Replace(".3.0.binds", ".binds");

                            if (!File.Exists(fileName))
                            {
                                Log.Error("steam file not found " + fileName);
                            }
                        }
                    }
                }
            }

            // epic
            if (!File.Exists(fileName))
            {
                bindingsPath = EpicPath.FindEpicEliteDirectory();

                if (!string.IsNullOrEmpty(bindingsPath))
                {
                    fileName = Path.Combine(bindingsPath, bindsName + ".4.0.binds");

                    if (!File.Exists(fileName))
                    {
                        Log.Error("epic file not found " + fileName);

                        fileName = fileName.Replace(".4.0.binds", ".3.0.binds");

                        if (!File.Exists(fileName))
                        {
                            fileName = fileName.Replace(".3.0.binds", ".binds");

                            if (!File.Exists(fileName))
                            {
                                Log.Error("epic file not found " + fileName);
                            }
                        }
                    }
                }
            }

            if (File.Exists(fileName))
            {
                var serializer = new XmlSerializer(typeof(UserBindings));

                //Log.Information( "using " + fileName);

                var reader = new StreamReader(fileName);
                Common.Binding[bindingType] = (UserBindings)serializer.Deserialize(reader);
                reader.Close();


                var keyBindingPath = Path.GetDirectoryName(fileName);
                Log.Information("monitoring key binding path #2 " + keyBindingPath);
                var keyBindingFileName = Path.GetFileName(fileName);


                Log.Information("monitoring key binding file name #2 " + keyBindingFileName);

                KeyBindingWatcher[(int)bindingType] = new KeyBindingWatcher(keyBindingPath, keyBindingFileName);
                KeyBindingWatcher[(int)bindingType].KeyBindingUpdated += HandleKeyBindingEvents;
                KeyBindingWatcher[(int)bindingType].StartWatching();
            }
            else
            {
                Log.Error("file not found " + fileName);
            }
        }


        private static void GetKeyBindings(Object threadContext)
        {
            if (KeyBindingWatcherStartPreset != null)
            {
                KeyBindingWatcherStartPreset.StopWatching();
                KeyBindingWatcherStartPreset.Dispose();
                KeyBindingWatcherStartPreset = null;
            }

            var bindingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Frontier Developments\Elite Dangerous\Options\Bindings");

            if (!Directory.Exists(bindingsPath))
            {
                Log.Error($"Directory doesn't exist {bindingsPath}");
            }

            var startPresetPath = Path.Combine(bindingsPath, "StartPreset.4.start");

            if (!File.Exists(startPresetPath))
            {

                startPresetPath = Path.Combine(bindingsPath, "StartPreset.start");
            }

            //Log.Information( "bindings path " + bindingsPath);

            var bindsNames = ReadStartPreset(startPresetPath);

            Log.Information("key bindings " + string.Join(",", bindsNames));

            var keyBindingPath = Path.GetDirectoryName(startPresetPath);
            Log.Information("monitoring key binding path #1 " + keyBindingPath);
            var keyBindingFileName = Path.GetFileName(startPresetPath);

            Log.Information("monitoring key binding file name #1 " + keyBindingFileName);
            KeyBindingWatcherStartPreset = new KeyBindingWatcher(keyBindingPath, keyBindingFileName);
            KeyBindingWatcherStartPreset.KeyBindingUpdated += HandleKeyBindingEvents;
            KeyBindingWatcherStartPreset.StartWatching();

            if (bindsNames.Length == 4) // odyssey
            {
                Log.Information("odyssey key bindings");

                HandleKeyBinding(BindingType.General, bindingsPath, bindsNames[0]);
                HandleKeyBinding(BindingType.Ship, bindingsPath, bindsNames[1]);
                HandleKeyBinding(BindingType.Srv, bindingsPath, bindsNames[2]);
                HandleKeyBinding(BindingType.OnFoot, bindingsPath, bindsNames[3]);
            }
            else // horizon
            {
                Log.Information("horizon key bindings");

                HandleKeyBinding(BindingType.General, bindingsPath, bindsNames.First());
                HandleKeyBinding(BindingType.Ship, bindingsPath, bindsNames.First());
                HandleKeyBinding(BindingType.Srv, bindingsPath, bindsNames.First());
                HandleKeyBinding(BindingType.OnFoot, bindingsPath, bindsNames.First());
            }

        }

        private void RefreshJson()
        {
            lock (Common.RefreshJsonLock)
            {
                
                Station.FullStationList[Station.PoiTypes.InterStellarFactors] = Station.GetAllStations(@"Data\interstellarfactors.json");
                
                Station.FullStationList[Station.PoiTypes.RawMaterialTraders] = Station.GetAllStations(@"Data\rawmaterialtraders.json");
                
                Station.FullStationList[Station.PoiTypes.ManufacturedMaterialTraders] = Station.GetAllStations(@"Data\manufacturedmaterialtraders.json");
                
                Station.FullStationList[Station.PoiTypes.EncodedDataTraders] = Station.GetAllStations(@"Data\encodeddatatraders.json");
                
                Station.FullStationList[Station.PoiTypes.HumanTechnologyBrokers] = Station.GetAllStations(@"Data\humantechnologybrokers.json");
                
                Station.FullStationList[Station.PoiTypes.GuardianTechnologyBrokers] = Station.GetAllStations(@"Data\guardiantechnologybrokers.json");
                
                Station.FullPowerStationList[Station.PowerTypes.AislingDuval] = Station.GetAllStations(@"Data\aislingduval.json");
                
                Station.FullPowerStationList[Station.PowerTypes.ArchonDelaine] = Station.GetAllStations(@"Data\archondelaine.json");
                
                Station.FullPowerStationList[Station.PowerTypes.ArissaLavignyDuval] = Station.GetAllStations(@"Data\arissalavignyduval.json");
                
                Station.FullPowerStationList[Station.PowerTypes.DentonPatreus] = Station.GetAllStations(@"Data\dentonpatreus.json");
                
                Station.FullPowerStationList[Station.PowerTypes.EdmundMahon] = Station.GetAllStations(@"Data\edmundmahon.json");
                
                Station.FullPowerStationList[Station.PowerTypes.FeliciaWinters] = Station.GetAllStations(@"Data\feliciawinters.json");
                
                Station.FullPowerStationList[Station.PowerTypes.LiYongRui] = Station.GetAllStations(@"Data\liyongrui.json");
                
                Station.FullPowerStationList[Station.PowerTypes.PranavAntal] = Station.GetAllStations(@"Data\pranavantal.json");
                
                Station.FullPowerStationList[Station.PowerTypes.YuriGrom] = Station.GetAllStations(@"Data\yurigrom.json");
                
                Station.FullPowerStationList[Station.PowerTypes.ZacharyHudson] = Station.GetAllStations(@"Data\zacharyhudson.json");
                
                Station.FullPowerStationList[Station.PowerTypes.ZeminaTorval] = Station.GetAllStations(@"Data\zeminatorval.json");

               
                Station.SystemStations = Station.GetAllStations(@"Data\fullstationlist.json").GroupBy(x => x.SystemName)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.DistanceToArrival).ToList());

                Station.MarketIdStations = Station.GetAllStations(@"Data\fullstationlist.json").GroupBy(x => x.MarketId)
                    .ToDictionary(x => x.Key, x => x.FirstOrDefault());

                Station.ColoniaBridge = Station.GetAllStations(@"Data\coloniabridge.json");

                Station.OdysseySettlements = Station.GetAllStations(@"Data\odysseysettlements.json").GroupBy(x => x.SystemName)
                    .ToDictionary(x => x.Key, x => x.OrderBy(y => y.DistanceToArrival).ToList());


                CnbSystems.FullCnbSystemsList = CnbSystems.GetAllCnbSystems(@"Data\cnbsystems.json");

                
                PopulatedSystems.SystemList = PopulatedSystems.GetAllPopupulatedSystems(@"Data\populatedsystemsEDSM.json").GroupBy(x => x.Name)
                    .ToDictionary(x => x.Key, x => x.First());

                
                HotspotSystems.FullHotspotSystemsList[HotspotSystems.MaterialTypes.Painite] = HotspotSystems.GetAllHotspotSystems(@"Data\painitesystems.json");

                
                HotspotSystems.FullHotspotSystemsList[HotspotSystems.MaterialTypes.LTD] = HotspotSystems.GetAllHotspotSystems(@"Data\ltdsystems.json");

                
                HotspotSystems.FullHotspotSystemsList[HotspotSystems.MaterialTypes.Platinum] = HotspotSystems.GetAllHotspotSystems(@"Data\platinumsystems.json");

                

                
                var cg = CommunityGoals.GetCommunityGoals(@"Data\communitygoals.json");
                
                var g = _galnet.GetGalnet(@"Data\galnet.json");
                
                _galnet.GetGalnetImages(g);

                _galnet.GalnetList = cg.Concat(g).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            }
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var jsonToken = _jsonTokenSource.Token;
            var hwInfoToken = _hwInfoTokenSource.Token;

            while (UserHandler.ConnectedIds.Count == 0)
            {
                await Task.Delay(1000);
            }

            await Task.Delay(2000);

            await _myHub.Clients.All.SendAsync("LoadingMessage", "Init Elite Api");

            Log.Information("Init Elite Api");

            try
            {
                GetKeyBindings(null);
                
                var defaultFilter = @"Journal.*.log";
                //#if DEBUG
                //defaultFilter = @"JournalAlpha.*.log";
                //#endif

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading Engineers...");
                _data.EngineersList = Station.GetEngineers(@"Data\engineers.json");

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading Engineering Materials...");
                (_engineer.EngineeringMaterials, _engineer.EngineeringMaterialsByKey) = _engineer.GetAllEngineeringMaterials(@"Data\entryData.json");

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading Blueprints...");
                _engineer.Blueprints = _engineer.GetAllBlueprints(@"Data\blueprints.json", _engineer.EngineeringMaterials);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading Engineer Blueprints...");
                _engineer.EngineerBlueprints = _engineer.GetEngineerBlueprints(@"Data\blueprints.json", _engineer.EngineeringMaterials);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading Ingredient Types...");
                _engineer.IngredientTypes = _engineer.GetIngredientTypes(@"Data\blueprints.json", _engineer.EngineeringMaterials);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading POI Items...");
                _poi.FullPoiList = await _poi.GetAllPois(); //?.GroupBy(x => x.System.Trim().ToLower()).ToDictionary(x => x.Key, x => x.ToList());

                _poi.FullPoiList = _poi.GetGECPois(@"Data\poigec.json", _poi.FullPoiList);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading JSON Items...");
                RefreshJson();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Loading History...");
                var path = _history.GetEliteHistory(defaultFilter, _data, _ships, _module);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Getting Shopping List from EDEngineer...");
                await _engineer.GetCommanderName();
                _engineer.GetShoppingList();
                Log.Information("journal path " + path);

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Getting sensor data from HWInfo...");

                _hwinfo.ReadMem("HWINFO.INC");

                if (_hwinfo.SensorData.Any())
                {
                    _hwinfo.SaveDataToFile(@"Data\hwinfo.json");
                }

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite Journal Status Watcher...");
                StatusWatcher = new StatusWatcher(path);

                StatusWatcher.StatusUpdated += _data.HandleStatusEvents;

                StatusWatcher.StartWatching();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite Journal Watcher...");

                JournalWatcher = new JournalWatcher(path, defaultFilter);

                JournalWatcher.AllEventHandler += _data.HandleEliteEvents;

                JournalWatcher.StartWatching().Wait();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite Cargo Watcher...");
                CargoWatcher = new CargoWatcher(path);

                CargoWatcher.CargoUpdated += _data.HandleCargoEvent;

                CargoWatcher.StartWatching();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite NavRoute Watcher...");
                NavRouteWatcher = new NavRouteWatcher(path);

                NavRouteWatcher.NavRouteUpdated += _data.HandleNavRouteEvent;

                NavRouteWatcher.StartWatching();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite BackPack Watcher...");
                BackPackWatcher = new BackPackWatcher(path);

                BackPackWatcher.BackPackUpdated += _data.HandleBackPackEvent;

                BackPackWatcher.StartWatching();

                await _myHub.Clients.All.SendAsync("LoadingMessage", "Starting Elite ShipLocker Watcher...");
                ShipLockerWatcher = new ShipLockerWatcher(path);

                ShipLockerWatcher.ShipLockerUpdated += _data.HandleShipLockerEvent;

                ShipLockerWatcher.StartWatching();

                JsonTask = Task.Run(async () =>
                {
                    Log.Information("json task started");

                    while (true)
                    {
                        if (jsonToken.IsCancellationRequested)
                        {
                            jsonToken.ThrowIfCancellationRequested();
                        }

                        var importData = new ImportData.ImportData();

                        _data.ImportData = true;

                        await _myHub.Clients.All.SendAsync("EliteRefresh", jsonToken);

                        await importData.Import();

                        _data.ImportData = false;

                        await _myHub.Clients.All.SendAsync("EliteRefresh", jsonToken);

                        RefreshJson();

                        _data.HandleJson();

                        await Task.Delay(30 * 60 * 1000, _jsonTokenSource.Token); // repeat every 30 minutes
                    }

                }, jsonToken);


                HWInfoTask = Task.Run(async () =>
                {
                    Log.Information("HWInfo task started");

                    while (true)
                    {
                        if (hwInfoToken.IsCancellationRequested)
                        {
                            hwInfoToken.ThrowIfCancellationRequested();
                        }

                        _hwinfo.ReadMem("HWINFO.INC");

                        if (_hwinfo.SensorData.Any())
                        {
                            await _myHub.Clients.All.SendAsync("EliteRefresh", hwInfoToken);
                        }

                        await Task.Delay(5 * 1000, _hwInfoTokenSource.Token); // repeat every 5 seconds
                    }


                }, hwInfoToken);


                await _myHub.Clients.All.SendAsync("LoadingMessage", "Init Elite Api Done");

            }
            catch (Exception ex)
            {
                Log.Error( $"Elite Api: {ex}");
            }

            await _myHub.Clients.All.SendAsync("LoadingDone");

            Common.MainWindow.Dispatcher.Invoke(() => {

                if (Common.MainWindow.WindowStyle != WindowStyle.None && Common.Minimized)
                {
                    Common.MainWindow.WindowState = WindowState.Minimized;
                }
            });

            stoppingToken.WaitHandle.WaitOne();

            StatusWatcher.StatusUpdated -= _data.HandleStatusEvents;

            StatusWatcher.StopWatching();

            JournalWatcher.AllEventHandler -= _data.HandleEliteEvents;

            JournalWatcher.StopWatching();

            CargoWatcher.CargoUpdated -= _data.HandleCargoEvent;

            CargoWatcher.StopWatching();

            NavRouteWatcher.NavRouteUpdated -= _data.HandleNavRouteEvent;

            NavRouteWatcher.StopWatching();

            BackPackWatcher.BackPackUpdated -= _data.HandleBackPackEvent;

            BackPackWatcher.StopWatching();

            ShipLockerWatcher.ShipLockerUpdated -= _data.HandleShipLockerEvent;

            ShipLockerWatcher.StopWatching();

            _jsonTokenSource.Cancel();

            try
            {
                JsonTask?.Wait(jsonToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("json background task ended");
            }
            finally
            {
                _jsonTokenSource.Dispose();
            }

            _hwInfoTokenSource.Cancel();

            try
            {
                HWInfoTask?.Wait(hwInfoToken);
            }
            catch (OperationCanceledException)
            {
                Log.Information("HWInfo background task ended");
            }
            finally
            {
                _hwInfoTokenSource.Dispose();
            }

            Log.Information("Shutdown Worker");

        }
    }
}
