﻿using System;
using System.Collections.Generic;
using System.Linq;
using EliteJournalReader.Events;

namespace dashboard_elite.EliteData
{
    public class RouteItem
    {
        public string StarSystem { get; set; }
        public long SystemAddress { get; set; }
        public List<double> StarPos { get; set; }
        public string StarClass { get; set; }
        public string IsFuelStar
        {
            get
            {
                string[] fuelStars = { "K", "G", "B", "F", "O", "A", "M" };

                return fuelStars.Contains(StarClass) ? "(Fuel Star)" : "";
            }
        }

        public double FuelCost { get; set; }
        public string FuelWarning { get; set; }

        public double Distance { get; set; }
        public  List<StationData> StationsInSystem { get; set; } 
    }

    public class Route
    {
        public static readonly object RefreshRouteLock = new object();


        public List<RouteItem> RouteList = new List<RouteItem>();

        public void HandleRouteEvent(NavRouteEvent.NavRouteEventArgs info)
        {
            lock (RefreshRouteLock)
            {

                if (info?.Route == null || info.Route.Length < 2)
                {
                    RouteList = new List<RouteItem>();
                }
                else
                {
                    RouteList = info.Route.Select(
                        x => new RouteItem
                        {
                            StarClass = x.StarClass,
                            StarPos = x.StarPos.ToList(),
                            StarSystem = x.StarSystem,
                            SystemAddress = x.SystemAddress,
                        }).Skip(1).ToList();

                    var lastLocation = info.Route[0].StarPos.ToList();

                    foreach (var route in RouteList)
                    {
                        Station.SystemStations.TryGetValue(route.StarSystem, out var stationsInSystem);
                        route.StationsInSystem = stationsInSystem;

                        var xs = lastLocation[0];
                        var ys = lastLocation[1];
                        var zs = lastLocation[2];

                        var xd = route.StarPos[0];
                        var yd = route.StarPos[1];
                        var zd = route.StarPos[2];

                        var deltaX = xs - xd;
                        var deltaY = ys - yd;
                        var deltaZ = zs - zd;

                        route.Distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                        lastLocation = route.StarPos;
                    }

                }
            }

        }
        

    }
}
