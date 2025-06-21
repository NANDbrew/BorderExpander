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
            //tradeWindRegions.Sort((a, b) => a.GetArea().CompareTo(b.GetArea()));
            tradeWindRegions.Sort((a, b) => a.Area.CompareTo(b.Area));
        }

        public static TradeWindRegion GetCurrentTradeWindRegion()
        {
/*            float longitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).x;
            float latitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).z;*/
            var pos = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform);
            Vector2 coords = new Vector2(pos.x, pos.z);

            foreach (var region in tradeWindRegions)
            {
/*                if (longitude > region.westBorder && longitude < region.eastBorder && latitude > region.southBorder && latitude < region.northBorder)
                {
                    return region;
                }*/
                if (region.limits.Contains(coords)) return region;
            }
            return default;
        }
    }

    public struct TradeWindRegion
    {
        public Rect limits;
        public Vector3 direction;
        public float influence;// = 0.25f;
        public float Area => limits.width * limits.height;
        //public static TradeWindRegion Default => new TradeWindRegion { limits = Rect.zero, direction = Vector3.right, influence = 0.25f};
/*        public float GetArea()
        {
            //if (limits.Contains(Vector2.one)
            //return (northBorder - southBorder) * (eastBorder - westBorder);
            TradeWinds.AddRegion(new TradeWindRegion { limits = Rect.MinMaxRect(-10, 0, 10, 10), direction = Vector3.right, influence = 0.5f });
        }*/
    }
}
