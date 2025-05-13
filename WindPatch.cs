using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(Wind))]
    public static class WindPatch
    {
        internal static float defaultTradeWindInfluence;
        public static List<TradeWindRegion> tradeWindRegions = new List<TradeWindRegion>();

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartPatch(float ___tradeWindInfluence)
        {
            defaultTradeWindInfluence = ___tradeWindInfluence;
        }

        [HarmonyPatch("GetCurrentTradeWind")]
        [HarmonyPrefix]
        public static bool GetTradeWindPatch(ref Vector3 __result, ref float ___tradeWindInfluence)
        {
            float longitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).x;
            float latitude = FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).z;

            foreach (var region in tradeWindRegions)
            {
                if (longitude > region.westBorder && longitude < region.eastBorder && latitude > region.southBorder && latitude < region.northBorder)
                {
                    ___tradeWindInfluence = region.influence;
                    __result = region.direction.normalized;
                    return false;
                }
            }
            ___tradeWindInfluence = defaultTradeWindInfluence;
            return true;
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
    }
}
