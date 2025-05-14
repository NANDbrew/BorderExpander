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
            tradeWindRegions.Sort((a, b) => a.GetArea().CompareTo(b.GetArea()));
        }

        public static TradeWindRegion GetCurrentTradeWindRegion()
        {
            float longitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).x;
            float latitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).z;

            foreach (var region in tradeWindRegions)
            {
                if (longitude > region.westBorder && longitude < region.eastBorder && latitude > region.southBorder && latitude < region.northBorder)
                {
                    return region;
                }
            }
            return null;
        }
    }

    public class TradeWindRegion
    {
        public float northBorder = 0f;
        public float southBorder = 0f;
        public float eastBorder = 0f;
        public float westBorder = 0f;
        public Vector3 direction = Vector3.zero;
        public float influence = 0.25f;
    
        public float GetArea() 
        {
            return (northBorder - southBorder) * (eastBorder - westBorder); 
        }
    }
}
