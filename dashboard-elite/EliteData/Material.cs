﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EDEngineer.Models;
using EliteJournalReader;
using EliteJournalReader.Events;

namespace dashboard_elite.EliteData
{
    public class Material
    {
        private readonly Engineer _engineer;

        public Material(Engineer engineer)
        {
            _engineer = engineer;
        }

        public class MaterialItem
        {
            public string Name { get; set; }
            public string Category { get; set; }
            public int Count { get; set; }
            public int MaximumCapacity { get; set; }
            public string Group { get; set; }
            public string BluePrintType { get; set; }

            public string MissionID { get; set; }

            public string MissionName { get; set; } // only filled in FipPanel.cs
            public string System { get; set; } // only filled in FipPanel.cs
            public string Station { get; set; } // only filled in FipPanel.cs

        }

        public class MaterialHistoryItem
        {
            public string Name { get; set; }

            public string System { get; set; }
            public int Count { get; set; }
        }

        public Dictionary<string, MaterialItem> ShipLockerList = new Dictionary<string, MaterialItem>();

        public Dictionary<string, MaterialItem> BackPackList = new Dictionary<string, MaterialItem>();

        public Dictionary<string, MaterialItem> MaterialList = new Dictionary<string, MaterialItem>();

        public Dictionary<string, Dictionary<string,MaterialHistoryItem>> MaterialHistoryList = new Dictionary<string, Dictionary<string,MaterialHistoryItem>>();

        public void AddHistory(string name, string system, int count)
        {
            MaterialHistoryList.TryGetValue(name, out var materialData);
            if (materialData == null)
            {
                var m = new Dictionary<string, MaterialHistoryItem>();
                var mhi = new MaterialHistoryItem
                {
                    Name = name, 
                    System = system, 
                    Count = count
                };
                m.Add(system, mhi);
                MaterialHistoryList.Add(name, m);
            }
            else
            {
                materialData.TryGetValue(system, out var systemData);
                if (systemData == null)
                {
                    var mhi = new MaterialHistoryItem
                    {
                        Name = name,
                        System = system,
                        Count = count
                    };
                    materialData.Add(system, mhi);
                }
                else
                {
                    systemData.Count += count;
                }
            }
        }

        private EntryData GetMaterialInfo(string name)
        {
            _engineer.EngineeringMaterialsByKey.TryGetValue(name, out var entry);

            return entry;
        }

        private int GetMaximumCapacity(EntryData entry)
        {
            var maximumCapacity = 0;

            if (entry != null)
            {
                maximumCapacity = entry.MaximumCapacity;
            }

            return maximumCapacity;
        }

        private string GetGroup(EntryData entry)
        {
            var group = string.Empty;

            if (entry != null)
            {
                group = entry.Group.StringValue();
            }

            return group;
        }

        private string GetBluePrintType(EntryData entry)
        {
            var type = string.Empty;

            if (entry != null)
            {
                _engineer.IngredientTypes.TryGetValue(entry.Name, out var material);

                if (material != null)
                {
                    if (material.Contains("Weapon"))
                    {
                        type += "W";
                    }
                    if (material.Contains("Suit"))
                    {
                        type += "S";
                    }
                }
            }

            return type;
        }

        public void HandleMaterialsEvent(MaterialsEvent.MaterialsEventArgs info)
        {
            MaterialList = new Dictionary<string, MaterialItem>();

            if (info.Encoded?.Any() == true)
            {
                foreach (var e in info.Encoded)
                {
                    var idxName = e.Name.ToLower();

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    MaterialList.Add(idxName, new MaterialItem{ Category = "Encoded" ,Name = name, Count = e.Count, 
                        MaximumCapacity = GetMaximumCapacity(entry),
                        Group = "",
                        BluePrintType = ""
                    });
                }
            }

            if (info.Manufactured?.Any() == true)
            {
                foreach (var e in info.Manufactured)
                {
                    var idxName = e.Name.ToLower();

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    MaterialList.Add(idxName, new MaterialItem { Category = "Manufactured", Name = name, Count = e.Count, 
                        MaximumCapacity = GetMaximumCapacity(entry),
                        Group = "",
                        BluePrintType = ""
                    });
                }
            }

            if (info.Raw?.Any() == true)
            {
                foreach (var e in info.Raw)
                {
                    var idxName = e.Name.ToLower();

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    MaterialList.Add(idxName, new MaterialItem { Category = "Raw", Name = name, Count = e.Count, 
                        MaximumCapacity = GetMaximumCapacity(entry),
                        Group = "",
                        BluePrintType = ""
                    });
                }
            }

        }

        public void HandleMaterialCollectedEvent(MaterialCollectedEvent.MaterialCollectedEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (MaterialList.ContainsKey(idxName))
            {
                MaterialList[idxName].Count += info.Count;
            }
            else
            {
                var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                var entry = GetMaterialInfo(idxName);

                MaterialList.Add(idxName, new MaterialItem { Category = info.Category, Name = name, Count = info.Count, 
                    MaximumCapacity = GetMaximumCapacity(entry),
                    Group = "",
                    BluePrintType = ""
                });
            }

        }

        public void HandleMaterialDiscardedEvent(MaterialDiscardedEvent.MaterialDiscardedEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (MaterialList.ContainsKey(idxName))
            {
                MaterialList[idxName].Count -= info.Count;
            }
        }

        public void HandleScientificResearchEvent(ScientificResearchEvent.ScientificResearchEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (MaterialList.ContainsKey(idxName))
            {
                MaterialList[idxName].Count -= info.Count;
            }
        }

        public void HandleMaterialTradedEvent(MaterialTradeEvent.MaterialTradeEventArgs info)
        {
            var idxPaidName = info.Paid.Material.ToLower();

            if (MaterialList.ContainsKey(idxPaidName))
            {
                MaterialList[idxPaidName].Count -= info.Paid.Quantity;
            }

            var idxRecName = info.Received.Material.ToLower();

            if (MaterialList.ContainsKey(idxRecName))
            {
                MaterialList[idxRecName].Count += info.Received.Quantity;
            }
            else
            {
                var name = (info.Received.Material_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxRecName)).Trim();

                var entry = GetMaterialInfo(idxRecName);

                MaterialList.Add(idxRecName, new MaterialItem { Category = info.Received.Category_Localised ?? info.Received.Category, Name = name, Count = info.Received.Quantity, 
                    MaximumCapacity = GetMaximumCapacity(entry),
                    Group = "",
                    BluePrintType = ""
                });
            }
        }

        public void HandleSynthesisedEvent(SynthesisEvent.SynthesisEventArgs info)
        {
            if (info.Materials?.Any() == true)
            {
                foreach (var i in info.Materials)
                {
                    var idxName = i.Name.ToLower();

                    if (MaterialList.ContainsKey(idxName))
                    {
                        MaterialList[idxName].Count -= i.Count;
                    }
                }
            }
        }

        public void HandleEngineerCraftEvent(EngineerCraftEvent.EngineerCraftEventArgs info)
        {

            if (info.Ingredients?.Any() == true)
            {
                foreach (var i in info.Ingredients)
                {
                    var idxName = i.Name.ToLower();

                    if (MaterialList.ContainsKey(idxName))
                    {
                        MaterialList[idxName].Count -= i.Count;
                    }
                }
            }
        }

        public void HandleTechnologyBrokerEvent(TechnologyBrokerEvent.TechnologyBrokerEventArgs info)
        {
            if (info.Materials?.Any() == true)
            {
                foreach (var i in info.Materials)
                {
                    var idxName = i.Name.ToLower();

                    if (MaterialList.ContainsKey(idxName))
                    {
                        MaterialList[idxName].Count -= i.Count;
                    }
                }
            }
        }

        public void HandleEngineerContributionEvent(EngineerContributionEvent.EngineerContributionEventArgs info)
        {

            if (info.Type == "Materials" && info.Material != null)
            {
                var idxName = info.Material.ToLower();

                if (MaterialList.ContainsKey(idxName))
                {
                    MaterialList[idxName].Count -= info.Quantity;
                }
            }

        }

        //{ "timestamp":"2021-05-21T14:32:02Z", "event":"MissionCompleted", "Faction":"Future of Arro Naga", "Name":"Mission_OnFoot_Collect_MB_name", "MissionID":770352328,
        //"Commodity":"$PersonalDocuments_Name;", "Commodity_Localised":"Personal Documents", "Count":1,
        //"Reward":36674,
        //"MaterialsReward":[ { "Name":"MiningAnalytics", "Name_Localised":"Mining Analytics", "Category":"$MICRORESOURCE_CATEGORY_Data;", "Category_Localised":"Data", "Count":2 } ],
        //"FactionEffects":[ { "Faction":"Future of Arro Naga", "Effects":[  ], "Influence":[ { "SystemAddress":3932277478106, "Trend":"UpGood", "Influence":"+" } ], "ReputationTrend":"UpGood", "Reputation":"+" } ] }


        public void HandleMissionCompletedEvent(MissionCompletedEvent.MissionCompletedEventArgs info)
        {
            if (!string.IsNullOrEmpty(info.Commodity_Localised) && ShipLockerList.Any(x => x.Value?.Name == info.Commodity_Localised))
            {
                var key = ShipLockerList.FirstOrDefault(x => x.Value.Name == info.Commodity_Localised).Key;

                /*
                ShipLockerList[key].Count -= info.Count ?? 0;

                if (ShipLockerList[key].Count <= 0)
                {
                    ShipLockerList.Remove(key);
                }*/
            }

            if (info.MaterialsReward?.Any() == true)
            {
                foreach (var i in info.MaterialsReward)
                {
                    var idxName = i.Name.ToLower();

                    var entry = GetMaterialInfo(idxName);

                    if (i.Category.StartsWith("$MICRORESOURCE_CATEGORY_"))
                    {
                        var category = i.Category.Replace("$MICRORESOURCE_CATEGORY_", "").Replace(";", ""); ;
                        /*
                        if (ShipLockerList.ContainsKey(idxName))
                        {
                            ShipLockerList[idxName].Count += i.Count;
                        }
                        else
                        {
                            var name = (i.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                            ShipLockerList.Add(idxName, new MaterialItem { Category = category, Name = name, Count = i.Count, MissionID = null, 
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                        }*/

                    }
                    else
                    {
                        if (MaterialList.ContainsKey(idxName))
                        {
                            MaterialList[idxName].Count += i.Count;
                        }
                        else
                        {
                            var name = (i.Name_Localised ??
                                        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                            MaterialList.Add(i.Name,
                                new MaterialItem
                                {
                                    Category = i.Category_Localised ?? i.Category, Name = name, Count = i.Count,
                                    MaximumCapacity = GetMaximumCapacity(entry),
                                    Group = "",
                                    BluePrintType = ""
                                });
                        }
                    }

                }

            }
        }

        public void HandleBackPackEvent(BackPackEvent.BackPackEventArgs info)
        {
            BackPackList = new Dictionary<string, MaterialItem>();

            if (info.Items?.Any() == true)
            {
                foreach (var e in info.Items)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (BackPackList.ContainsKey(idxName + idxMissionID))
                    {
                        BackPackList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        BackPackList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Item", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

            if (info.Components?.Any() == true)
            {
                foreach (var e in info.Components)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (BackPackList.ContainsKey(idxName + idxMissionID))
                    {
                        BackPackList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        BackPackList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Component", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

            if (info.Consumables?.Any() == true)
            {
                foreach (var e in info.Consumables)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (BackPackList.ContainsKey(idxName + idxMissionID))
                    {
                        BackPackList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        BackPackList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Consumable", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = ""
                            });
                    }
                }
            }

            if (info.Data?.Any() == true)
            {
                foreach (var e in info.Data)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (BackPackList.ContainsKey(idxName + idxMissionID))
                    {
                        BackPackList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        BackPackList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Data", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

        }

        public void HandleShipLockerMaterialsEvent(ShipLockerMaterialsEvent.ShipLockerMaterialsEventArgs info)
        {
            ShipLockerList = new Dictionary<string, MaterialItem>();

            if (info.Items?.Any() == true)
            {
                foreach (var e in info.Items)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (ShipLockerList.ContainsKey(idxName + idxMissionID))
                    {
                        ShipLockerList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        ShipLockerList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Item", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

            if (info.Components?.Any() == true)
            {
                foreach (var e in info.Components)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (ShipLockerList.ContainsKey(idxName + idxMissionID))
                    {
                        ShipLockerList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        ShipLockerList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Component", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

            if (info.Consumables?.Any() == true)
            {
                foreach (var e in info.Consumables)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (ShipLockerList.ContainsKey(idxName + idxMissionID))
                    {
                        ShipLockerList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        ShipLockerList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Consumable", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = ""
                            });
                    }
                }
            }

            if (info.Data?.Any() == true)
            {
                foreach (var e in info.Data)
                {
                    var idxName = e.Name.ToLower();
                    var idxMissionID = e.MissionID;

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    var entry = GetMaterialInfo(idxName);

                    if (ShipLockerList.ContainsKey(idxName + idxMissionID))
                    {
                        ShipLockerList[idxName + idxMissionID].Count += e.Count;
                    }
                    else
                    {
                        ShipLockerList.Add(idxName + idxMissionID,
                            new MaterialItem
                            {
                                Category = "Data", Name = name, Count = e.Count, MissionID = idxMissionID,
                                MaximumCapacity = GetMaximumCapacity(entry),
                                Group = GetGroup(entry),
                                BluePrintType = GetBluePrintType(entry)
                            });
                    }
                }
            }

        }

        //"Transfers":[ { "Name":"healthpack", "Name_Localised":"Medkit", "Category":"Consumable", "LockerOldCount":1, "LockerNewCount":0, "Direction":"ToBackpack" },
        //{ "Name":"energycell", "Name_Localised":"Energy Cell", "Category":"Consumable", "LockerOldCount":2, "LockerNewCount":0, "Direction":"ToBackpack" }, { "Name":"amm_grenade_emp", "Name_Localised":"Shield Disruptor", "Category":"Consumable", "LockerOldCount":1, "LockerNewCount":0, "Direction":"ToBackpack" },
        //{ "Name":"amm_grenade_frag", "Name_Localised":"Frag Grenade", "Category":"Consumable", "LockerOldCount":1, "LockerNewCount":0, "Direction":"ToBackpack" }, { "Name":"amm_grenade_shield", "Name_Localised":"Shield Projector", "Category":"Consumable", "LockerOldCount":1, "LockerNewCount":0, "Direction":"ToBackpack" }
        //] }
        public void HandleTransferMicroResourcesEvent(TransferMicroResourcesEvent.TransferMicroResourcesEventArgs info)
        {
            foreach (var e in info.Transfers)
            {
                var lockerCount = e.LockerNewCount;

                var idxName = e.Name.ToLower();

                if (e.Direction == "ToShipLocker")
                {
                    string missionID = null;

                    if (ShipLockerList.ContainsKey(idxName))
                    {
                        ShipLockerList[idxName].Count = lockerCount;
                    }
                    else
                    {
                        var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                        var entry = GetMaterialInfo(idxName);


                        if (!string.IsNullOrEmpty(name) && BackPackList.Any(x => x.Value?.Name == name))
                        {
                            var key = BackPackList.FirstOrDefault(x => x.Value.Name == name).Key;

                            missionID = BackPackList[key].MissionID;
                        }
                        
                        ShipLockerList.Add(idxName + missionID, new MaterialItem { Category = e.Category, Name = name, Count = lockerCount, MissionID = missionID, 
                            MaximumCapacity = GetMaximumCapacity(entry),
                            Group = GetGroup(entry),
                            BluePrintType = GetBluePrintType(entry)
                        });
                    }

                    //-------------------------

                    // not handled by backpack.json !!!!!!!!!!!!!!!

                    if (BackPackList.ContainsKey(idxName + missionID))
                    {
                        var backpackCount = e.LockerNewCount - e.LockerOldCount;

                        BackPackList[idxName + missionID].Count -= backpackCount;

                        if (BackPackList[idxName + missionID].Count <= 0)
                        {
                            BackPackList.Remove(idxName + missionID);
                        }
                    } 

                }
                else if (e.Direction == "ToBackpack")
                {
                    if (ShipLockerList.ContainsKey(idxName))
                    {
                        ShipLockerList[idxName].Count = lockerCount;

                        if (ShipLockerList[idxName].Count <= 0)
                        {
                            ShipLockerList.Remove(idxName);
                        }
                    }


                }
            }

        }
        /* already handled in shiplocker.json
                        
        //"BuyMicroResources", "Name":"healthpack", "Name_Localised":"Medkit", "Category":"Consumable", "Count":1, "Price":1000, "MarketID":3221524992 }

        public void HandleBuyMicroResourcesEvent(BuyMicroResourcesEvent.BuyMicroResourcesEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (ShipLockerList.ContainsKey(idxName))
            {
                ShipLockerList[idxName].Count += info.Count;
            }
            else
            {
                var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                var entry = GetMaterialInfo(idxName);

                ShipLockerList.Add(idxName, new MaterialItem { Category = info.Category, Name = name, Count = info.Count, MissionID = null, 
                    MaximumCapacity = GetMaximumCapacity(entry),
                    Group = GetGroup(entry),
                    BluePrintType = GetBluePrintType(entry)
                });
            }

        }

        public void HandleSellMicroResourcesEvent(SellMicroResourcesEvent.SellMicroResourcesEventArgs info)
        {
            foreach (var e in info.MicroResources)
            {
                var idxName = e.Name.ToLower();

                if (ShipLockerList.ContainsKey(idxName))
                {
                    ShipLockerList[idxName].Count -= e.Count;

                    if (ShipLockerList[idxName].Count <= 0)
                    {
                        ShipLockerList.Remove(idxName);
                    }
                }
            }
        }
        
        public void HandleTradeMicroResourcesEvent(TradeMicroResourcesEvent.TradeMicroResourcesEventArgs info)
        {
            foreach (var e in info.Offered)
            {
                var idxName = e.Name.ToLower();

                if (ShipLockerList.ContainsKey(idxName))
                {
                    ShipLockerList[idxName].Count -= e.Count;

                    if (ShipLockerList[idxName].Count <= 0)
                    {
                        ShipLockerList.Remove(idxName);
                    }
                }
            }

            var idxRecName = info.Received.ToLower();

            //????????????????var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxRecName)).Trim();

            var entry = GetMaterialInfo(idxRecName);

            if (BackPackList.ContainsKey(idxRecName))
            {
                ShipLockerList[idxRecName].Count += info.Count;
            }
            else
            {
                ShipLockerList.Add(idxRecName, new MaterialItem
                {
                    Category = info.Category, Name = info.Received, Count = info.Count, MissionID = null,
                    MaximumCapacity = GetMaximumCapacity(entry),
                    Group = GetGroup(entry),
                    BluePrintType = GetBluePrintType(entry)
                });
            }

            //???????????????

        }

         */

        /* already handled in backpack.json
        public void HandleBackPackChangeEvent(BackPackChangeEvent.BackPackChangeEventArgs info)
        {
            if (info.Added?.Any() == true)
            {
                foreach (var e in info.Added)
                {
                   var idxName = e.Name.ToLower();

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    if (BackPackList.ContainsKey(idxName))
                    {
                        BackPackList[idxName].Count += e.Count;
                    }
                    else
                    {
                        BackPackList.Add(idxName, new MaterialItem { Category = e.Type, Name = name, Count = e.Count });
                    }

                }
            }

            if (info.Removed?.Any() == true)
            {
                foreach (var e in info.Removed)
                {
                    var idxName = e.Name.ToLower();

                    var name = (e.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                    if (BackPackList.ContainsKey(idxName))
                    {
                        BackPackList[idxName].Count -= e.Count;

                        if (BackPackList[idxName].Count <= 0)
                        {
                            BackPackList.Remove(idxName);
                        }
                    }
                }
            }

        } 

        public void HandleDropItemsEvent(DropItemsEvent.DropItemsEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (BackPackList.ContainsKey(idxName))
            {
                BackPackList[idxName].Count -= info.Count;

                if (BackPackList[idxName].Count == 0)
                {
                    BackPackList.Remove(idxName);
                }
            }

        }

        public void HandleCollectItemsEvent(CollectItemsEvent.CollectItemsEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (BackPackList.ContainsKey(idxName))
            {
                BackPackList[idxName].Count += info.Count;
            }
            else
            {
                var name = (info.Name_Localised ?? CultureInfo.CurrentCulture.TextInfo.ToTitleCase(idxName)).Trim();

                BackPackList.Add(idxName, new MaterialItem { Category = info.Type, Name = name, Count = info.Count, MissionID = null });
            }
        }

        public void HandleUseConsumableEvent(UseConsumableEvent.UseConsumableEventArgs info)
        {
            var idxName = info.Name.ToLower();

            if (BackPackList.ContainsKey(idxName))
            {
                BackPackList[idxName].Count -= 1;

                if (BackPackList[idxName].Count == 0)
                {
                    BackPackList.Remove(idxName);
                }
            }

        } */


        public void RefreshMaterialList()
        {
            if (_engineer.IngredientShoppingList?.Any() == true && MaterialList?.Any() == true)
            {
                foreach (var i in _engineer.IngredientShoppingList)
                {
                    var materialData = MaterialList.FirstOrDefault(x => x.Value.Name == i.Name).Value;

                    i.Inventory = materialData?.Count ?? 0;
                }
            }

            if (_engineer.IngredientShoppingList?.Any() == true && ShipLockerList?.Any() == true)
            {
                foreach (var i in _engineer.IngredientShoppingList)
                {
                    var materialData = ShipLockerList.FirstOrDefault(x => x.Value.Name == i.Name).Value;

                    if (materialData != null)
                    {
                        i.Inventory = materialData.Count;
                    }
                }
            }
        }



    }

}



