using OVRSimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BorderExpander
{
    public static class TradeWinds
    {
        internal static float defaultTradeWindInfluence;
        internal static List<TradeWindRegion> tradeWindRegions = new List<TradeWindRegion>
        {
            new TradeWindRegion { name = "northern", minLon = -200, minLat = 33, maxLon = 200, maxLat = 90, dirX = 1f, dirY = 0.5f, influence = 0.25f },
            new TradeWindRegion { name = "al'ankh", minLon = -200, minLat = 30f, maxLon = -2f, maxLat = 33f, dirX = 0.75f, dirY = 0.75f, influence = 0.25f },
            new TradeWindRegion { name = "emerald", minLon = -2f, minLat = 30f, maxLon = 200f, maxLat = 32f, dirX = -1f, dirY = -0.5f, influence = 0.25f },
            new TradeWindRegion { name = "southern easterlies", minLon = -200, minLat = -30f, maxLon = 200, maxLat = 15, dirX = -1f, dirY = 0.5f, influence = 0.25f },
            new TradeWindRegion { name = "southern westerlies", minLon = -200, minLat = -60, maxLon = 200, maxLat = -33, dirX = 1f, dirY = -0.5f, influence = 0.25f }
        };

        public static void AddRegion(TradeWindRegion region)
        {
            tradeWindRegions.Add(region);
            SortRegions();
        }

        public static bool RemoveRegion(string name)
        {
            for (int i = 0; i < tradeWindRegions.Count; i++)
            {
                if (tradeWindRegions[i].name == name)
                {
                    tradeWindRegions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public static void SortRegions()
        {
            tradeWindRegions.Sort((a, b) => a.Area.CompareTo(b.Area));

        }

        public static TradeWindRegion GetCurrentTradeWindRegion()
        {
            var pos = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform);
            Vector2 coords = new Vector2(pos.x, pos.z);

            foreach (var region in tradeWindRegions)
            {
                if (region.Contains(coords)) return region;
            }
            return TradeWindRegion.None;
        }

        internal static void ReadDefs()
        {
            string path = Path.Combine(Directory.GetParent(Plugin.instance.Info.Location).FullName, "tradewinds.json");
            if (!File.Exists(path))
            {
                WriteDefs();
                return;
            }

            string input = File.ReadAllText(path);
            var regg = new List<TradeWindRegion>();
            JSONArray rra = JSON.Parse(input).AsArray;
            foreach (var r in rra)
            {
                TradeWindRegion reg = new TradeWindRegion();
                var p = r.Value.Linq;
                foreach (var q in p)
                {
                    if (q.Key == "name") reg.name = q.Value;
                    else if (q.Key == "minLon") reg.minLon = q.Value;
                    else if (q.Key == "minLat") reg.minLat = q.Value;
                    else if (q.Key == "maxLon") reg.maxLon = q.Value;
                    else if (q.Key == "maxLat") reg.maxLat = q.Value;
                    else if (q.Key == "dirX") reg.dirX = q.Value;
                    else if (q.Key == "dirY") reg.dirY = q.Value;
                    else if (q.Key == "influence") reg.influence = q.Value;
                }
                regg.Add(reg);
            }
            tradeWindRegions = regg;
        }

        internal static void WriteDefs()
        {
            OVRSimpleJSON.JSONArray arr = new OVRSimpleJSON.JSONArray();
            for (int i = 0; i < tradeWindRegions.Count; i++)
            {
                var reg = tradeWindRegions[i];
                JSONObject item = new JSONObject();
                item.Add("name", new JSONString(reg.name));
                item.Add("minLon", new JSONNumber(reg.minLon));
                item.Add("minLat", new JSONNumber(reg.minLat));
                item.Add("maxLon", new JSONNumber(reg.maxLon));
                item.Add("maxLat", new JSONNumber(reg.maxLat));
                item.Add("dirX", new JSONNumber(reg.dirX));
                item.Add("dirY", new JSONNumber(reg.dirY));
                item.Add("influence", new JSONNumber(reg.influence));
                arr.Add(item);
            }
            //Debug.Log(arr.ToString());
            //Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(TradeWinds.tradeWindRegions));
            File.WriteAllText(Path.Combine(Directory.GetParent(Plugin.instance.Info.Location).FullName, "tradewinds.json"), arr.ToString(4));
        }
    }

    public struct TradeWindRegion
    {
        //public Rect limits;
        public float minLat;
        public float maxLat;
        public float minLon;
        public float maxLon;
        public float dirX;
        public float dirY;
        public float influence;// = 0.25f;
        public string name;
        public float Area => (maxLon - minLon) * (maxLat - minLat);
        public bool Contains(Vector2 point)
        {
            return point.x >= minLon && point.x < maxLon && point.y >= minLat && point.y < maxLat;
        }
        public static TradeWindRegion None => new TradeWindRegion { name = "none", dirX = 0f, dirY = 0f, influence = 0f };
    }
}
