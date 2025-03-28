﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using dashboard_elite.EliteData;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

// ReSharper disable StringLiteralTypo

namespace dashboard_elite.ImportData
{
    public static class JsonReaderExtensions
    {
        private static void DeleteExpiredFile(string fullPath, int minutes)
        {
            new FileInfo(fullPath).Directory?.Create();

            if (File.Exists(fullPath))
            {
                var modification = File.GetLastWriteTime(fullPath);

                if ((DateTime.Now - modification).TotalMinutes >= minutes)
                {
                    File.Delete(fullPath);
                }
            }
        }

        private static string GetExePath()
        {
            var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(strExeFilePath);
        }

        public static IEnumerable<T> ParseJson<T>(string path)
        {
            path = Path.Combine(GetExePath(), path);

            if (File.Exists(path))
            {
                var serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                using (var s = File.Open(path, FileMode.Open))
                {
                    using (var sr = new StreamReader(s))
                    {
                        using (var reader = new JsonTextReader(sr))
                        {
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonToken.StartObject)
                                {
                                    yield return serializer.Deserialize<T>(reader);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static async Task<bool> DownloadJson<T>(string url, string path, bool wasUpdated, bool gzip)
        {
            path = Path.Combine(GetExePath(), path);

            DeleteExpiredFile(path, 1440);

            if (!File.Exists(path))
            {
                var serializer = new JsonSerializer
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore
                };

                using (var sw = new StreamWriter(File.Open(path, FileMode.Create)))
                {
                    using var jsonWriter = new JsonTextWriter(sw);

                    //Common.WebClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                    //Common.WebClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                    
                    using var response = await Common.WebClient.GetAsync(url);
                    using var stream = await response.Content.ReadAsStreamAsync();
                    if (gzip)
                    {
                        using var decompressed = new GZipStream(stream, CompressionMode.Decompress);
                        using var sr = new StreamReader(decompressed);
                        using var jsonReader = new JsonTextReader(sr);
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType == JsonToken.StartArray)
                            {
                                jsonWriter.WriteStartArray();
                            }
                            else if (jsonReader.TokenType == JsonToken.EndArray)
                            {
                                jsonWriter.WriteEndArray();
                            }
                            else if (jsonReader.TokenType == JsonToken.StartObject)
                            {
                                var sd = serializer.Deserialize<T>(jsonReader);

                                serializer.Serialize(jsonWriter, sd);
                            }
                        }

                    }
                    else
                    {
                        using var sr = new StreamReader(stream);
                        using var jsonReader = new JsonTextReader(sr);
                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType == JsonToken.StartArray)
                            {
                                jsonWriter.WriteStartArray();
                            }
                            else if (jsonReader.TokenType == JsonToken.EndArray)
                            {
                                jsonWriter.WriteEndArray();
                            }
                            else if (jsonReader.TokenType == JsonToken.StartObject)
                            {
                                var sd = serializer.Deserialize<T>(jsonReader);

                                serializer.Serialize(jsonWriter, sd);
                            }
                        }
                    }

                }
            
                wasUpdated = true;

            }

            return wasUpdated;
        }
    }

    public class ImportData
    {
        private static string GetExePath()
        {
            var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(strExeFilePath);
        }


        private List<dashboard_elite.EliteData.StationData> GetStationsData(List<StationEDSM> stations)
        {
            return stations.Select(x => new StationData
            {
                Name = x.Name,
                DistanceToArrival = x.DistanceToArrival ?? 0,
                Type = x.Type,

                SystemName = x.SystemName,
                SystemSecurity = x.Security,
                SystemPopulation = x.Population ?? 0,

                PowerplayState = x.PowerplayEDSM?.powerState,
                Powers = x.PowerplayEDSM?.power,

                Allegiance = x.Allegiance,
                Government = x.Government,
                Economy = x.Economy,
                Economies = string.Join(",", x.Economies ?? new List<string>() { x.Economy }),
                Faction = x.ControllingFaction?.Name,

                X = x.X ?? 0,
                Y = x.Y ?? 0,
                Z = x.Z ?? 0,

                Body = x.Body,

                MarketId = x.MarketId ?? 0,

                SystemState = string.Join(",", x.States ?? new List<string>() {"None"})

            }).ToList();

        }

        private void DeleteExpiredFile(string fullPath, int minutes)
        {
            new FileInfo(fullPath).Directory?.Create();

            if (File.Exists(fullPath))
            {
                var modification = File.GetLastWriteTime(fullPath);

                if ((DateTime.Now - modification).TotalMinutes >= minutes)
                {
                    File.Delete(fullPath);
                }
            }
        }

        private void StationSerialize(List<StationEDSM> stations, string path)
        {
            path = Path.Combine(GetExePath(), path);

            new FileInfo(path).Directory?.Create();

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            using (var sw = new StreamWriter(path))
            using (var writer = new JsonTextWriter(sw))
            {
                var stationsData = GetStationsData(stations);

                serializer.Serialize(writer, stationsData);
            }
        }


        private bool NeedToUpdateFile(string path, int minutes)
        {
            path = Path.Combine(GetExePath(), path);

            if (File.Exists(path))
            {
                var modification = File.GetLastWriteTime(path);

                if ((DateTime.Now - modification).TotalMinutes <= minutes)
                {
                    return false;
                }
            }
            else
            {
                new FileInfo(path).Directory?.Create();
            }

            return true;
        }


        public class CNBSystemData
        {
            [JsonProperty("x")]
            [DefaultValue(0)]
            public double X { get; set; }

            [JsonProperty("y")]
            [DefaultValue(0)]
            public double Y { get; set; }

            [JsonProperty("z")]
            [DefaultValue(0)]
            public double Z { get; set; }

            [JsonProperty("beac")]
            public string CompromisedNavBeacon { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("systemsecurity")]
            public string SystemSecurity { get; set; }

            [JsonProperty("systempopulation")]
            [DefaultValue(0)]
            public long SystemPopulation { get; set; }


            [JsonProperty("allegiance")]
            public string Allegiance { get; set; }

            [JsonProperty("primary_economy")]
            public string PrimaryEconomy { get; set; }

            [JsonProperty("government")]
            public string Government { get; set; }

        }

        private static async Task<List<CNBSystemData>> GetCnbSystems(string url)
        {
            try
            {
                var data = await Common.WebClient.GetStringAsync(url);

                var jObj = JObject.Parse(data);

                var systemInfo = jObj.ToObject<Dictionary<string, CNBSystemData>>();

                return systemInfo.Where(x => x.Value.CompromisedNavBeacon == "1").Select(x => new CNBSystemData
                {
                    CompromisedNavBeacon = x.Value.CompromisedNavBeacon,
                    X = x.Value.X,
                    Y = x.Value.Y,
                    Z = x.Value.Z,
                    Name = x.Key
                }).ToList();


            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }

            return new List<CNBSystemData>();

        }

        private void CnbSystemsSerialize(List<CNBSystemData> systems, string fullPath)
        {
            new FileInfo(fullPath).Directory?.Create();

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            using (var sw = new StreamWriter(fullPath))
            using (var writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, systems);
            }
        }

        private async Task DownloadCnbSystems(string path, Dictionary<string, SystemSpansh> systemSpanshListByName)
        {
            path = Path.Combine(GetExePath(), path);

            DeleteExpiredFile(path, 1440);

            if (!File.Exists(path))
            {
                Log.Logger.Information("looking up Compromised Nav Beacons");

                var cnbSystems = await GetCnbSystems("http://edtools.cc/res.json");

                cnbSystems.ForEach(z =>
                {
                    if (systemSpanshListByName.ContainsKey(z.Name))
                    {
                        var systemInfo = systemSpanshListByName[z.Name];

                        z.SystemSecurity = systemInfo.Security;
                        z.SystemPopulation = systemInfo.Population;
                        z.PrimaryEconomy = systemInfo.PrimaryEconomy;
                        z.Government = systemInfo.Government;
                        z.Allegiance = systemInfo.Allegiance;
                    }
                });

                CnbSystemsSerialize(cnbSystems, path);
            }
        }

        private async Task DownloadHotspotSystems(string path, string url, string material)
        {
            try
            {
                path = Path.Combine(GetExePath(), path);

                DeleteExpiredFile(path, 1440);

                if (!File.Exists(path))
                {
                    Log.Logger.Information("looking up " + material + " Hotspots");

                    var data = await Common.WebClient.GetStringAsync(url + material);

                    File.WriteAllText(path, data);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }
        }

        private async Task DownloadPoiGEC(string path, string url)
        {
            try
            {
                path = Path.Combine(GetExePath(), path);

                DeleteExpiredFile(path, 1440);

                if (!File.Exists(path))
                {
                    Log.Logger.Information("looking up GEC POIs");

                    var data = await Common.WebClient.GetStringAsync(url);

                    File.WriteAllText(path, data);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }
        }

        private void GalnetSerialize(List<GalnetData> galnet, string fullPath)
        {
            new FileInfo(fullPath).Directory?.Create();

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            using (var sw = new StreamWriter(fullPath))
            using (var writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, galnet);
            }
        }

        private void CommunityGoalSerialize(List<CommunityGoal> cg, string fullPath)
        {
            new FileInfo(fullPath).Directory?.Create();

            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            using (var sw = new StreamWriter(fullPath))
            using (var writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, cg);
            }
        }

        private async Task DownloadGalnet(string path, string url)
        {
            try
            {
                path = Path.Combine(GetExePath(), path);

                DeleteExpiredFile(path, 720);

                if (!File.Exists(path))
                {
                    Log.Logger.Information("looking up galnet");

                    var data = await Common.WebClient.GetStringAsync(url);

                    var galnetJson = JsonConvert.DeserializeObject<GalnetRoot>(data)?.Data.Select(x => x.Attributes).ToList();

                    if (galnetJson?.Any() == true)
                    {
                        galnetJson.ForEach(x =>
                        {
                            x.ImageList = new List<string>();

                            if (!string.IsNullOrEmpty(x.Image))
                            {
                                foreach (var i in x.Image.TrimStart(',').Split(',').ToList())
                                {
                                    if (!string.IsNullOrEmpty(i))
                                    {
                                        x.ImageList.Add(i);
                                    }
                                }
                            }

                            x.Image = null;

                            if (x.BodyItem != null)
                            {
                                x.Body = x.BodyItem.Value.Replace("\r\n", "<br>");

                                x.BodyItem = null;
                            }
                        });

                        GalnetSerialize(galnetJson, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }
        }

        private async Task DownloadCommunityGoals(string path, string url)
        {
            try
            {
                path = Path.Combine(GetExePath(), path);

                DeleteExpiredFile(path, 180);

                if (!File.Exists(path))
                {
                    Log.Logger.Information("looking up community goals");

                    var data = await Common.WebClient.GetStringAsync(url);

                    var cgJson = JsonConvert.DeserializeObject<CommunityGoalsData>(data);

                    if (cgJson != null)
                    {
                        CommunityGoalSerialize(cgJson.ActiveInitiatives, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }
        }


        public async Task Import()
        {
            Log.Logger.Information("ImportData started");

            try
            {
                List<StationEDSM> stationsEDSM;
                List<PopulatedSystemEDSM> populatedSystemsEDSM;
                Dictionary<long, PowerplayEDSM> powerplayEDSM;
                List<SystemSpansh> systemSpansh;

                Dictionary<long, SystemSpansh> systemSpanshDictionary;

                var wasAnyUpdated = false;

                Log.Logger.Information("downloading populated systems from EDSM");
                wasAnyUpdated = await JsonReaderExtensions.DownloadJson<PopulatedSystemEDSM>("https://www.edsm.net/dump/systemsPopulated.json.gz", @"Data\populatedsystemsEDSM.json", wasAnyUpdated, true);

                Log.Logger.Information("downloading powerplay systems from EDSM");
                wasAnyUpdated = await JsonReaderExtensions.DownloadJson<PowerplayEDSM>("https://www.edsm.net/dump/powerPlay.json.gz", @"Data\powerplayEDSM.json", wasAnyUpdated, true);

                Log.Logger.Information("downloading station list from EDSM");
                wasAnyUpdated = await JsonReaderExtensions.DownloadJson<StationEDSM>("https://www.edsm.net/dump/stations.json.gz", @"Data\stationsEDSM.json", wasAnyUpdated, true);

                Log.Logger.Information("downloading station list from Spansh");
                wasAnyUpdated = await JsonReaderExtensions.DownloadJson<SystemSpansh>("https://downloads.spansh.co.uk/galaxy_stations.json.gz", @"Data\stationsSpansh.json", wasAnyUpdated, true);

                Log.Logger.Information("looking up edsm populated systems");

                populatedSystemsEDSM = JsonReaderExtensions.ParseJson<PopulatedSystemEDSM>(@"Data\populatedsystemsEDSM.json").ToList();

                Log.Logger.Information("looking up edsm powerplay systems");

                powerplayEDSM = JsonReaderExtensions.ParseJson<PowerplayEDSM>(@"Data\powerplayEDSM.json").ToList()
                    .GroupBy(x => x.id64).Select(x => x.First())
                    .ToDictionary(x => x.id64);

                Log.Logger.Information("looking up edsm stations");

                stationsEDSM = JsonReaderExtensions.ParseJson<StationEDSM>(@"Data\stationsEDSM.json").ToList();

                stationsEDSM = stationsEDSM.Where(x =>
                        x.Type != "Fleet Carrier" &&
                        x.Government != "Fleet Carrier" &&
                        x.Economy != "Fleet Carrier" &&
                        !string.IsNullOrEmpty(x.Type) &&
                        !string.IsNullOrEmpty(x.Government) &&
                        !string.IsNullOrEmpty(x.Economy))
                    .ToList();

                Log.Logger.Information("looking up spansh stations");

                systemSpansh = JsonReaderExtensions.ParseJson<SystemSpansh>(@"Data\stationsSpansh.json").ToList();

                systemSpanshDictionary = systemSpansh
                    .GroupBy(x => x.Id64).Select(x => x.First())
                    .ToDictionary(x => x.Id64);

                var systemSpanshListByName = systemSpansh
                    .ToDictionary(x => x.Name);

                if (NeedToUpdateFile(@"Data\cnbsystems.json", 1440))
                {
                    await DownloadCnbSystems(@"Data\cnbsystems.json", systemSpanshListByName);
                }

                stationsEDSM.ForEach(z =>
                {
                    powerplayEDSM.TryGetValue(z.SystemId64, out var powerplay);
                    z.PowerplayEDSM = powerplay;

                    systemSpanshDictionary.TryGetValue(z.SystemId64, out var spansh);

                    if (spansh != null)
                    {
                        //z.Economy = spansh.PrimaryEconomy;
                        //z.SecondEconomy = spansh.SecondaryEconomy;

                        if (!string.IsNullOrEmpty(z.Economy))
                        {
                            z.Economies = new List<string>() { z.Economy };
                            if (!string.IsNullOrEmpty(z.SecondEconomy))
                            {
                                z.Economies.Add(z.SecondEconomy);
                            }
                        }

                        z.SystemName = spansh.Name;
                        z.X = spansh.Coords?.X ?? 0;
                        z.Y = spansh.Coords?.Y ?? 0;
                        z.Z = spansh.Coords?.Z ?? 0;

                        z.Security = spansh.Security;
                        z.Population = spansh.Population;

                        if (spansh.Factions?.Any() == true)
                        {
                            z.States = spansh.Factions.Where(x => x.State != "None").Select(x => x.State).Distinct().ToList();
                            if (!z.States.Any())
                            {
                                z.States = new List<string>() { "None" };
                            }
                        }

                        if (spansh.Stations?.Any() == true)
                        {
                            var spanshStation =
                                spansh.Stations.FirstOrDefault(x =>
                                    x.Id == z.MarketId); // .FirstOrDefault(x => x.Name == z.Name);

                            if (spanshStation != null)
                            {
                                z.SpanshStation = spanshStation;
                            }

                        }
                    }

                });
                
                if (wasAnyUpdated)
                {
                    Log.Logger.Information("finding Engineers stations");
                    var engineers = stationsEDSM
                        .Where(x =>
                            x.Government == "Workshop (Engineer)" &&
                            x.SystemName != "Maia").ToList(); // gets rid of second professor palin

                    StationSerialize(engineers, @"Data\engineers.json");

                    Log.Logger.Information("finding Aisling Duval stations");
                    var aislingDuval = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Aisling Duval" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(aislingDuval, @"Data\aislingduval.json");

                    Log.Logger.Information("finding Archon Delaine stations");
                    var archonDelaine = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Archon Delaine" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(archonDelaine, @"Data\archondelaine.json");

                    Log.Logger.Information("finding Arissa Lavigny-Duval stations");
                    var arissaLavignyDuval = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "A. Lavigny-Duval" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(arissaLavignyDuval, @"Data\arissalavignyduval.json");

                    Log.Logger.Information("finding Denton Patreus stations");
                    var dentonPatreus = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Denton Patreus" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(dentonPatreus, @"Data\dentonpatreus.json");

                    Log.Logger.Information("finding Edmund Mahon stations");
                    var edmundMahon = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Edmund Mahon" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(edmundMahon, @"Data\edmundmahon.json");

                    Log.Logger.Information("finding Felicia Winters stations");
                    var feliciaWinters = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Felicia Winters" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(feliciaWinters, @"Data\feliciawinters.json");

                    Log.Logger.Information("finding Li Yong-Rui stations");
                    var liYongRui = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Li Yong-Rui" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(liYongRui, @"Data\liyongrui.json");

                    Log.Logger.Information("finding Pranav Antal stations");
                    var pranavAntal = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Pranav Antal" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(pranavAntal, @"Data\pranavantal.json");

                    Log.Logger.Information("finding Yuri Grom stations");
                    var yuriGrom = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Yuri Grom" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(yuriGrom, @"Data\yurigrom.json");

                    Log.Logger.Information("finding Zachary Hudson stations");
                    var zacharyHudson = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Zachary Hudson" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(zacharyHudson, @"Data\zacharyhudson.json");

                    Log.Logger.Information("finding Zemina Torval stations");
                    var zeminaTorval = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.PowerplayEDSM != null &&
                            x.PowerplayEDSM.power == "Zemina Torval" &&
                            x.PowerplayEDSM.powerState == "Controlled" &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();
                    StationSerialize(zeminaTorval, @"Data\zeminatorval.json");


                    //Odyssey Settlements
                    var odysseySettlements = stationsEDSM
                        .Where(x =>
                            x.Type == "Odyssey Settlement"
                        ).ToList();

                    StationSerialize(odysseySettlements, @"Data\odysseysettlements.json");


                    Log.Logger.Information("finding interstellar factors");

                    var interStellarFactors = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&
                            x.OtherServices.Any(y => y == "Interstellar Factors Contact") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(interStellarFactors, @"Data\interstellarfactors.json");

                    // see https://github.com/EDCD/FDevIDs/blob/a2655d9836fa32c4d9a8041edd8b2a4a7ed9d15b/How%20to%20determine%20MatTrader%20and%20Broker%20type

                    Log.Logger.Information("finding encoded data traders");

                    //Encoded data trader
                    //Found in systems with medium-high security, a 'high tech' or 'military' economy
                    var encodedDataTraders = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            x.Economy != "Extraction" &&
                            x.Economy != "Refinery" &&
                            x.Economy != "Industrial" &&

                            (x.Economy == "High Tech" || x.Economy == "Military" || x.SecondEconomy == "High Tech" || x.SecondEconomy == "Military") &&

                            x.OtherServices.Any(y => y == "Material Trader") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(encodedDataTraders, @"Data\encodeddatatraders.json");

                    Log.Logger.Information("finding raw material traders");

                    //Raw material trader
                    //Found in systems with medium-high security, an 'extraction' or 'refinery' economy
                    var rawMaterialTraders = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            x.Economy != "High Tech" &&
                            x.Economy != "Military" &&
                            x.Economy != "Industrial" &&

                            (x.Economy == "Extraction" || x.Economy == "Refinery" || x.SecondEconomy == "Extraction" || x.SecondEconomy == "Refinery") &&

                            x.OtherServices.Any(y => y == "Material Trader") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(rawMaterialTraders, @"Data\rawmaterialtraders.json");

                    Log.Logger.Information("finding manufactured material traders");

                    //Manufactured material trader
                    //Found in systems with medium-high security, an 'industrial' economy
                    var manufacturedMaterialTraders = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            x.Economy != "High Tech" &&
                            x.Economy != "Military" &&
                            x.Economy != "Extraction" &&
                            x.Economy != "Refinery" &&

                            (x.Economy == "Industrial" || x.SecondEconomy == "Industrial") &&

                            x.OtherServices.Any(y => y == "Material Trader") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(manufacturedMaterialTraders, @"Data\manufacturedmaterialtraders.json");

                    Log.Logger.Information("finding human technology brokers");

                    //Human Technology Broker
                    //Found in systems with an 'Industrial' economy
                    var humanTechnologyBrokers = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            !(x.Economy == "High Tech" || (x.Economy != "Industrial" && x.SecondEconomy == "High Tech")) &&

                            x.OtherServices.Any(y => y == "Technology Broker") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(humanTechnologyBrokers, @"Data\humantechnologybrokers.json");

                    Log.Logger.Information("finding guardian technology brokers");

                    //Guardian Technology Broker
                    //Found in systems with a 'high tech' economy
                    var guardianTechnologyBrokers = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            (x.Economy == "High Tech" || (x.Economy != "Industrial" && x.SecondEconomy == "High Tech")) &&

                            x.OtherServices.Any(y => y == "Technology Broker") &&
                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(guardianTechnologyBrokers, @"Data\guardiantechnologybrokers.json");

                    Log.Logger.Information("finding full station list");

                    //Full Station List
                    var fullStationList = stationsEDSM
                        .Where(x =>
                            x.Type != "Planetary Outpost" &&
                            x.Type != "Outpost" &&
                            x.Type != "Planetary Port" &&
                            x.Type != "Odyssey Settlement" &&

                            x.SpanshStation?.LandingPads?.Large > 0).ToList();

                    StationSerialize(fullStationList, @"Data\fullstationlist.json");

                    //Colonia Bridge
                    var coloniaBridge = stationsEDSM
                        .Where(x =>
                                x.Name.StartsWith("CB-") &&
                                x.Type == "Mega ship" &&
                                x.ControllingFaction?.Id == 82837//77645
                        ).ToList();

                    StationSerialize(coloniaBridge, @"Data\coloniabridge.json");
                }

                await DownloadHotspotSystems(@"Data\painitesystems.json", "http://edtools.cc/miner?a=r&n=", "Painite");
                await DownloadHotspotSystems(@"Data\ltdsystems.json", "http://edtools.cc/miner?a=r&n=", "LTD");
                await DownloadHotspotSystems(@"Data\platinumsystems.json", "http://edtools.cc/miner?a=r&n=", "Platinum");

                await DownloadPoiGEC(@"Data\poigec.json", "https://edastro.com/poi/json/all");

                // https://gist.github.com/corenting/b6ac5cf8f446f54856e08b6e287fe835


                // stopped woring 29/05/2021
                //"https://elitedangerous-website-backend-production.elitedangerous.com/api/galnet?_format=json"

                await DownloadGalnet(@"Data\galnet.json", "https://cms.zaonce.net/en-GB/jsonapi/node/galnet_article?&sort=-published_at&page[offset]=0&page[limit]=100");

                // stopped working 1 dec 2020
                //DownloadCommunityGoals(@"Data\communitygoals.json", "https://elitedangerous-website-backend-production.elitedangerous.com/api/initiatives/list?_format=json&lang=en"); 
                await DownloadCommunityGoals(@"Data\communitygoals.json", "https://api.orerve.net/2.0/website/initiatives/list?lang=en");

            }
            catch (Exception ex)
            {
                Log.Logger.Information(ex.ToString());
            }

            Log.Logger.Information("ImportData ended");
        }
    }
}
