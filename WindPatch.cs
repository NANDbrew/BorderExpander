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
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void AwakePatch(float ___tradeWindInfluence)
        {
            TradeWinds.defaultTradeWindInfluence = ___tradeWindInfluence;
        }

        [HarmonyPatch("GetCurrentTradeWind")]
        [HarmonyPrefix]
        public static bool GetTradeWindPatch(ref Vector3 __result, ref float ___tradeWindInfluence)
        {
            if (TradeWinds.GetCurrentTradeWindRegion() is TradeWindRegion region)
            {
                ___tradeWindInfluence = region.influence;
                __result = region.direction.normalized;
                return false;
            }
            ___tradeWindInfluence = TradeWinds.defaultTradeWindInfluence;
            return true;
        }
    }

}
