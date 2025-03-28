﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Serilog;

// ReSharper disable IdentifierTypo

namespace dashboard_elite.EliteData
{

    public static class PopulatedSystems
    {
        public static Dictionary<string, PopulatedSystem> SystemList = new Dictionary<string, PopulatedSystems.PopulatedSystem>();

      public class PopulatedSystem
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("state")]
            public string State { get; set; }
        }

        public static List<PopulatedSystem> GetAllPopupulatedSystems(string path)
        {
            try
            {
                path = Path.Combine(dashboard_elite.Common.ExePath, path);

                if (File.Exists(path))
                {
                    return JsonConvert.DeserializeObject<List<PopulatedSystem>>(File.ReadAllText(path));
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex.ToString());
            }

            return new List<PopulatedSystem>();
        }


        public static string GetSystemState(string name)
        {
            SystemList.TryGetValue(name, out var value);

            if (!string.IsNullOrEmpty(value?.State))
            {
                return value.State;
            }

            return "";

        }


    }
}
