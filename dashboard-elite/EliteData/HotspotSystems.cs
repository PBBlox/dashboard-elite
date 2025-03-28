﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Serilog;

// ReSharper disable IdentifierTypo

namespace dashboard_elite.EliteData
{

    public static class HotspotSystems
    {
        public enum MaterialTypes
        {
            Painite = 0,
            LTD = 1, //2,
            Platinum = 2 //4

        }

        public static Dictionary<MaterialTypes, List<HotspotSystemData>> FullHotspotSystemsList = new Dictionary<MaterialTypes, List<HotspotSystemData>>
        {
            {MaterialTypes.Painite, new List<HotspotSystemData>()},
            {MaterialTypes.LTD, new List<HotspotSystemData>()},
            {MaterialTypes.Platinum, new List<HotspotSystemData>()}
        };

        public class HotspotSystemCoordsData
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
        }

        public class HotspotSystemData
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("comment")]
            public string Comment { get; set; }

            [JsonProperty("coords")]
            public HotspotSystemCoordsData Coords { get; set; }

            [JsonIgnore]
            public double Distance { get; set; }
        }

        private static string StripHtml(string input)
        {
            return Regex.Replace(input, "<.*?>", string.Empty);
        }

        public static List<HotspotSystemData> GetAllHotspotSystems(string path)
        {
            try
            {
                path = Path.Combine(dashboard_elite.Common.ExePath, path);

                if (File.Exists(path))
                {
                    var data = JsonConvert.DeserializeObject<List<HotspotSystemData>>(File.ReadAllText(path));

                    data.ForEach(x => { x.Comment = StripHtml(x.Comment); });
                    return data;
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
            }

            return new List<HotspotSystemData>();
        }

        public static List<HotspotSystemData> GetNearestHotspotSystems(List<double> starPos, List<HotspotSystemData> data)
        {
            if (data?.Any() == true && starPos?.Count == 3)
            {
                data.ForEach(systemItem =>
                {
                    var xs = starPos[0];
                    var ys = starPos[1];
                    var zs = starPos[2];

                    var xd = systemItem.Coords.X;
                    var yd = systemItem.Coords.Y;
                    var zd = systemItem.Coords.Z;

                    var deltaX = xs - xd;
                    var deltaY = ys - yd;
                    var deltaZ = zs - zd;

                    systemItem.Distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
                });

                return data.Where(x => x.Distance >= 0).OrderBy(x => x.Distance)/*.Take(10)*/.ToList();
            }

            return new List<HotspotSystemData>();

        }
    }
}
