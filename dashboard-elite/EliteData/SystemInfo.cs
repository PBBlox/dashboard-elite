﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dashboard_elite.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json; //using System.Windows.Media.Imaging;
//using System.Windows.Threading;

namespace dashboard_elite.EliteData
{

    public class Discovery
    {
        public string commander { get; set; }
        public string date { get; set; }
    }

    public class Belt
    {
        public string name { get; set; } // Shinrarta Dezhra A A Belt
        [JsonProperty("type")]
        public string beltType { get; set; } // Rocky
        public long mass { get; set; }
        public float innerRadius { get; set; }
        public float outerRadius { get; set; }
    }

    public class Ring
    {
        public string name { get; set; } // Maia A 1 A Ring
        [JsonProperty("type")]
        public string ringType { get; set; } // Metal Rich
        public long mass { get; set; }
        public float innerRadius { get; set; }
        public float outerRadius { get; set; }
    }

    public class Body
    {
        public int id { get; set; }
        public long id64 { get; set; }
        public long bodyId { get; set; }
        public string name { get; set; } // Maia A
        public Discovery discovery { get; set; }
        [JsonProperty("type")]
        public string bodyType { get; set; } // Star
        public string subType { get; set; } // B (Blue-White) Star
        public string updateTime { get; set; } // 2020-07-25 14:32:33
        public List<Dictionary<string, int>> parents { get; set; } // [{"Null":8},{"Star":1},{"Null":0}]

        public int distanceToArrival { get; set; }

        public bool? isMainStar { get; set; }
        public bool? isScoopable { get; set; }
        public int? age { get; set; }
        public string spectralClass { get; set; } // T8
        public string luminosity { get; set; } // V
        public float? absoluteMagnitude { get; set; }
        public float? solarMasses { get; set; }
        public float? solarRadius { get; set; }
        public float? surfaceTemperature { get; set; }
        public float? orbitalPeriod { get; set; }
        public float? semiMajorAxis { get; set; }
        public float? orbitalEccentricity { get; set; }
        public float? orbitalInclination { get; set; }
        public float? argOfPeriapsis { get; set; }
        public float? rotationalPeriod { get; set; }
        public bool? rotationalPeriodTidallyLocked { get; set; }
        public float? axialTilt { get; set; }
        public bool? isLandable { get; set; }
        public float? gravity { get; set; }
        public float? earthMasses { get; set; }
        public float? radius { get; set; }
        public float? surfacePressure { get; set; }

        public string volcanismType { get; set; } // No volcanism
        public string atmosphereType { get; set; } // No atmosphere
        public string terraformingState { get; set; } // Not terraformable
        public string reserveLevel { get; set; } // Pristine

        public Dictionary<string,float> atmosphereComposition { get; set; }
        public Dictionary<string,float> solidComposition { get; set; }
        public Dictionary<string,float> materials { get; set; }

        public List<Belt> belts { get; set; }
        public List<Ring> rings { get; set; }
    }

    public class ScanData
    {
        public int id { get; set; }
        public long id64 { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public int bodyCount { get; set; }
        public List<Body> bodies { get; set; }
    }


    public class SystemData
    {
        public string StarSystem { get; set; }

        public ScanData Data { get; set; }

        public double Progress { get; set; }

        public DateTime? LastRefresh { get; set; }

    }


    public class SystemInfo
    {
        private readonly IHubContext<MyHub> _myHub;

        public SystemInfo(IHubContext<MyHub> myHub)
        {
            _myHub = myHub;
        }
        
        public static readonly Dictionary<string, string> PeriodicElements = new Dictionary<string, string>
        {
            ["Actinium"] = "Ac",
            ["Aluminum"] = "Al",
            ["Americium"] = "Am",
            ["Antimony"] = "Sb",
            ["Argon"] = "Ar",
            ["Arsenic"] = "As",
            ["Astatine"] = "At",
            ["Barium"] = "Ba",
            ["Berkelium"] = "Bk",
            ["Beryllium"] = "Be",
            ["Bismuth"] = "Bi",
            ["Bohrium"] = "Bh",
            ["Boron"] = "B",
            ["Bromine"] = "Br",
            ["Cadmium"] = "Cd",
            ["Caesium"] = "Cs",
            ["Calcium"] = "Ca",
            ["Californium"] = "Cf",
            ["Carbon"] = "C",
            ["Cerium"] = "Ce",
            ["Chlorine"] = "Cl",
            ["Chromium"] = "Cr",
            ["Cobalt"] = "Co",
            ["Copernicium"] = "Cn",
            ["Copper"] = "Cu",
            ["Curium"] = "Cm",
            ["Darmstadtium"] = "Ds",
            ["Dubnium"] = "Db",
            ["Dysprosium"] = "Dy",
            ["Einsteinium"] = "Es",
            ["Erbium"] = "Er",
            ["Europium"] = "Eu",
            ["Fermium"] = "Fm",
            ["Flerovium"] = "Fl",
            ["Fluorine"] = "F",
            ["Francium"] = "Fr",
            ["Gadolinium"] = "Gd",
            ["Gallium"] = "Ga",
            ["Germanium"] = "Ge",
            ["Gold"] = "Au",
            ["Hafnium"] = "Hf",
            ["Hassium"] = "Hs",
            ["Helium"] = "He",
            ["Holmium"] = "Ho",
            ["Hydrogen"] = "H",
            ["Indium"] = "In",
            ["Iodine"] = "I",
            ["Iridium"] = "Ir",
            ["Iron"] = "Fe",
            ["Krypton"] = "Kr",
            ["Lanthanum"] = "La",
            ["Lawrencium"] = "Lr",
            ["Lead"] = "Pb",
            ["Lithium"] = "Li",
            ["Livermorium"] = "Lv",
            ["Lutetium"] = "Lu",
            ["Magnesium"] = "Mg",
            ["Manganese"] = "Mn",
            ["Meitnerium"] = "Mt",
            ["Mendelevium"] = "Md",
            ["Mercury"] = "Hg",
            ["Molybdenum"] = "Mo",
            ["Neodymium"] = "Nd",
            ["Neon"] = "Ne",
            ["Neptunium"] = "Np",
            ["Nickel"] = "Ni",
            ["Niobium"] = "Nb",
            ["Nitrogen"] = "N",
            ["Nobelium"] = "No",
            ["Osmium"] = "Os",
            ["Oxygen"] = "O",
            ["Palladium"] = "Pd",
            ["Phosphorus"] = "P",
            ["Platinum"] = "Pt",
            ["Plutonium"] = "Pu",
            ["Polonium"] = "Po",
            ["Potassium"] = "K",
            ["Praseodymium"] = "Pr",
            ["Promethium"] = "Pm",
            ["Protactinium"] = "Pa",
            ["Radium"] = "Ra",
            ["Radon"] = "Rn",
            ["Rhenium"] = "Re",
            ["Rhodium"] = "Rh",
            ["Roentgenium"] = "Rg",
            ["Rubidium"] = "Rb",
            ["Ruthenium"] = "Ru",
            ["Rutherfordium"] = "Rf",
            ["Samarium"] = "Sm",
            ["Scandium"] = "Sc",
            ["Seaborgium"] = "Sg",
            ["Selenium"] = "Se",
            ["Silicon"] = "Si",
            ["Silver"] = "Ag",
            ["Sodium"] = "Na",
            ["Strontium"] = "Sr",
            ["Sulphur"] = "S",
            ["Tantalum"] = "Ta",
            ["Technetium"] = "Tc",
            ["Tellurium"] = "Te",
            ["Terbium"] = "Tb",
            ["Thallium"] = "Tl",
            ["Thorium"] = "Th",
            ["Thulium"] = "Tm",
            ["Tin"] = "Sn",
            ["Titanium"] = "Ti",
            ["Tungsten"] = "W",
            ["Ununoctium"] = "Uuo",
            ["Ununpentium"] = "Uup",
            ["Ununseptium"] = "Uus",
            ["Ununtrium"] = "Uut",
            ["Uranium"] = "U",
            ["Vanadium"] = "V",
            ["Xenon"] = "Xe",
            ["Ytterbium"] = "Yb",
            ["Yttrium"] = "Y",
            ["Zinc"] = "Zn",
            ["Zirconium"] = "Zr"

        };

        public  Task SystemDataTask;
        private  CancellationTokenSource _systemDataTokenSource = new CancellationTokenSource();

        public  SystemData SystemData = new SystemData();

        private static readonly object _refreshSystemInfoLock = new object();


        private  byte[] Decompress(byte[] gzip)
        {
            using (var stream = new GZipStream(new MemoryStream(gzip),
                CompressionMode.Decompress))
            {
                const int size = 100000;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private  async Task<string> GetJson(string url)
        {
            var utf8 = await Common.WebClient.GetStringAsync(url);

            return utf8;
        }

        private  string GetExePath()
        {
            var strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(strExeFilePath);
        }

        public  void GetSystemData(string starSystem)
        {
            lock (_refreshSystemInfoLock)
            {
                if (SystemDataTask != null && !SystemDataTask.IsCompleted) 
                     return;

                if (SystemData.StarSystem == starSystem && SystemData.Data != null && !(SystemData.Progress < 100.0)) 
                    return;

                if ((SystemData.Data == null || (SystemData.Progress < 100.0)) && SystemData.LastRefresh != null && !(SystemData.LastRefresh < DateTime.Now.AddMinutes(-5)))
                    return;

                try
                {
                    SystemData.LastRefresh = DateTime.Now;

                    var fullPath = Path.Combine(GetExePath(), "Data\\Bodies\\" + starSystem + ".json");

                    new FileInfo(fullPath).Directory?.Create();

                    if (File.Exists(fullPath))
                    {
                        var json = File.ReadAllText(fullPath);

                        if (!string.IsNullOrEmpty(json))
                        {
                            var data = JsonConvert.DeserializeObject<ScanData>(json);

                            var progress = (double) data.bodies.Count / (double) data.bodyCount * 100.0;

                            lock (dashboard_elite.Common.RefreshSystemLock)
                            {
                                SystemData.StarSystem = starSystem;
                                SystemData.Progress = progress;
                                SystemData.Data = data;
                            }
                        }
                    }
                    else
                    {
                        var systemDataToken = _systemDataTokenSource.Token;

                        SystemDataTask = Task.Run(async () =>
                        {
                            if (systemDataToken.IsCancellationRequested)
                            {
                                systemDataToken.ThrowIfCancellationRequested();
                            }

                            var json = await GetJson("https://www.edsm.net/api-system-v1/bodies?systemName=" +
                                               starSystem);
                            
                            //var test = "Maia";
                            //test = "Synuefe XR-H d11-102"; // https://www.edsm.net/en/system/bodies/id/6379187/name/Synuefe+XR-H+d11-102
                            //test = "Flyiedge HC-R b23-6";  // https://www.edsm.net/en/system/bodies/id/54965727/name/Flyiedge+HC-R+b23-6
                            //json = GetJson("https://www.edsm.net/api-system-v1/bodies?systemName=" + test);

                            if (json?.Length > 10)
                            {
                                var data = JsonConvert.DeserializeObject<ScanData>(json);

                                var progress = (double) data.bodies.Count / (double) data.bodyCount * 100.0;
                                //progress = 50.0;
                                if (!string.IsNullOrEmpty(json) && Math.Abs(progress - 100.0) < 0.01)
                                {
                                    File.WriteAllText(fullPath, json);
                                }

                                lock (dashboard_elite.Common.RefreshSystemLock)
                                {
                                    SystemData.StarSystem = starSystem;
                                    SystemData.Progress = progress;
                                    SystemData.Data = data;
                                }
                            }
                            else
                            {
                                lock (dashboard_elite.Common.RefreshSystemLock)
                                {
                                    SystemData.StarSystem = starSystem;
                                    SystemData.Progress = 0;
                                    SystemData.Data = null; // not found
                                }

                            }

							await _myHub.Clients.All.SendAsync("EliteRefresh");

                        }, systemDataToken);
                    }

                }
                catch 
                {
                    // do nothing
                }
            }


        }
        
        public string MaterialsString(int i)
        {
            var ms = "";

            var materials = SystemData.Data.bodies[i].materials;

            foreach (var m in materials)
            {
                PeriodicElements.TryGetValue(m.Key, out var elem);
                if (!string.IsNullOrEmpty(elem))
                {
                    ms += elem;
                }
                else
                {
                    ms += m.Key;
                }
                ms += "&nbsp;(" + m.Value.ToString("N1") + "%) ";

            }

            return ms.Trim();

        }

        public string BodyTreeElement1(int i)
        {
            var parents = SystemData.Data.bodies[i].parents;

            var colcount = parents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;

            int[] topLineHide = new int[100];

            for (int k = i; k < SystemData.Data.bodies.Count; k++)
            {
                var rowParents = SystemData.Data.bodies[k].parents;
                var rowColcount = rowParents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;

                for (int j = 0; j < rowColcount; j++)
                {
                    if (topLineHide[j] != 2)
                    {
                        if (rowColcount > 0 && j < rowColcount - 1)
                        {
                            topLineHide[j] = 1;
                        }
                        else
                        {
                            topLineHide[j] = 2;
                        }
                    }
                }

                if (rowColcount == 0)
                    break;
            }

            var s = "";

            for (int j = 0; j < colcount; j++)
            {
                s += "<td style=\"";
                s += "width:10px;";
                s += "\">&nbsp;</td>";

                var leftLine = "border-left: 2px solid #ffffff;";

                if (j < colcount - 1 && i == SystemData.Data.bodies.Count - 1)
                {
                    leftLine = "";
                }

                if (topLineHide[j] == 1)
                {
                    leftLine = "";
                }

                s += "<td style=\"";
                s += "width:10px;";
                s += leftLine;
                s += "\">&nbsp;</td>";
            }


            return s;
        }

        public string BodyTreeElement2(int i)
        {
            var parents = SystemData.Data.bodies[i].parents;

            var colcount = parents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;
            var nextColcount = colcount;

            if (i < SystemData.Data.bodies.Count - 1)
            {
                var nextParents = SystemData.Data.bodies[i + 1].parents;
                nextColcount = nextParents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;
            }

            int[] topLineHide = new int[100];

            for (int k = i + 1; k < SystemData.Data.bodies.Count; k++)
            {
                var rowParents = SystemData.Data.bodies[k].parents;
                var rowColcount = rowParents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;

                for (int j = 0; j < rowColcount; j++)
                {
                    if (topLineHide[j] != 2)
                    {
                        if (rowColcount > 0 && j < rowColcount - 1)
                        {
                            topLineHide[j] = 1;
                        }
                        else
                        {
                            topLineHide[j] = 2;
                        }
                    }
                }

                if (rowColcount == 0)
                    break;
            }

            var s = "";

            for (int j = 0; j < colcount; j++)
            {
                s += "<td style=\"";
                s += "width:10px;";
                s += "\">&nbsp;</td>";


                var topLine = "border-top: 2px solid #ffffff; ";
                if (colcount > 1 && j < colcount - 1)
                {
                    topLine = "";
                }

                var leftLine = "border-left: 2px solid #ffffff;";

                if (nextColcount == 0 || (j == colcount - 1 && nextColcount < colcount) || i == SystemData.Data.bodies.Count - 1)
                {
                    leftLine = "";
                }

                if (topLineHide[j] == 1)
                {
                    leftLine = "";
                }


                s += "<td style=\"";
                s += "width:10px;";
                s += leftLine + topLine;
                s += "\">&nbsp;</td>";
            }

            return s;
        }


        public string BodyTreeElement3(int i)
        {
            var parents = SystemData.Data.bodies[i].parents;

            var colcount = parents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;
            var nextColcount = colcount;

            if (i < SystemData.Data.bodies.Count - 1)
            {
                var nextParents = SystemData.Data.bodies[i + 1].parents;
                nextColcount = nextParents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;
            }

            int[] topLineHide = new int[100];

            for (int k = i + 1; k < SystemData.Data.bodies.Count; k++)
            {
                var rowParents = SystemData.Data.bodies[k].parents;
                var rowColcount = rowParents?.Where(x => !x.ContainsKey("Null")).ToList().Count ?? 0;

                for (int j = 0; j < rowColcount; j++)
                {
                    if (topLineHide[j] != 2)
                    {
                        if (rowColcount > 0 && j < rowColcount - 1)
                        {
                            topLineHide[j] = 1;
                        }
                        else
                        {
                            topLineHide[j] = 2;
                        }
                    }
                }

                if (rowColcount == 0)
                    break;
            }

            var s = "";

            for (int j = 0; j < colcount; j++)
            {
                s += "<td style=\"font-size: 3px; ";
                s += "width:10px;";
                s += "\">&nbsp;</td>";

                var leftLine = "border-left: 2px solid #ffffff;";

                if (nextColcount == 0 || (j == colcount - 1 && nextColcount < colcount) || i == SystemData.Data.bodies.Count - 1)
                {
                    leftLine = "";
                }

                if (topLineHide[j] == 1)
                {
                    leftLine = "";
                }

                s += "<td style=\"font-size: 3px; ";
                s += "width:10px;";
                s += leftLine;
                s += "\">&nbsp;</td>";
            }

            return s;
        }


    }
}
