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
        static TradeWindRegion northern = new TradeWindRegion 
        { 
            limits = Rect.MinMaxRect(float.NegativeInfinity, 33f, float.PositiveInfinity, float.PositiveInfinity),
            direction = new Vector3(1f, 0f, 0.5f), 
            influence = 0.25f 
        };
        static TradeWindRegion alankh = new TradeWindRegion 
        { 
            limits = Rect.MinMaxRect(float.NegativeInfinity, 30f, 33f, -2f), 
            direction = new Vector3(0.75f, 0f, 0.75f), 
            influence = 0.25f 
        };
        static TradeWindRegion emerald = new TradeWindRegion 
        { 
            limits = Rect.MinMaxRect(-2f, 30f, 32f, float.PositiveInfinity), 
            direction = new Vector3(-1f, 0f, -0.5f), 
            influence = 0.25f 
        };

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(float ___tradeWindInfluence)
        {
            TradeWinds.defaultTradeWindInfluence = ___tradeWindInfluence;

            TradeWinds.AddRegion(northern);
            TradeWinds.AddRegion(alankh);
            TradeWinds.AddRegion(emerald);
        }

        [HarmonyPatch("GetCurrentTradeWind")]
        [HarmonyPrefix]
        public static bool GetTradeWindPatch(ref Vector3 __result, ref float ___tradeWindInfluence)
        {
            var region = TradeWinds.GetCurrentTradeWindRegion();
            if (region.limits != null)
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
