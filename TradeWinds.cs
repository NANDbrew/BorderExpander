using BorderExpander;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BorderExpander
{
    public class TradeWinds
    {
        internal static float defaultTradeWindInfluence;
        private static List<TradeWindRegion> tradeWindRegions = new List<TradeWindRegion>();

        public static void AddRegion(TradeWindRegion region)
        {
            tradeWindRegions.Add(region);
            tradeWindRegions.Sort((a, b) => a.Area.CompareTo(b.Area));
        }

        public static TradeWindRegion GetCurrentTradeWindRegion()
        {
            var pos = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform);
            Vector2 coords = new Vector2(pos.x, pos.z);

            foreach (var region in tradeWindRegions)
            {
                if (region.limits.Contains(coords)) return region;
            }
            return TradeWindRegion.None;
        }
    }

    public struct TradeWindRegion
    {
        public Rect limits;
        public Vector3 direction;
        public float influence;// = 0.25f;
        public string name;
        public float Area => limits.width * limits.height;

        public static TradeWindRegion None => new TradeWindRegion { name = "none", direction = Vector3.zero, limits = Rect.zero, influence = 0f };
    }
}
