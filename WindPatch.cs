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
        static Wind wind;
        static float defaultMinMagnitude;
        static TradeWindRegion northern = new TradeWindRegion 
        { 
            name = "northern",
            limits = Rect.MinMaxRect(-200f, 33f, 200f, 90f),
            direction = new Vector3(1f, 0f, 0.5f), 
            influence = 0.25f 
        };
        static TradeWindRegion alankh = new TradeWindRegion 
        { 
            name = "al'ankh",
            limits = Rect.MinMaxRect(-200, 30f, -2f, 33f), 
            direction = new Vector3(0.75f, 0f, 0.75f), 
            influence = 0.25f 
        };
        static TradeWindRegion emerald = new TradeWindRegion 
        { 
            name = "emerald",
            limits = Rect.MinMaxRect(-2f, 30f, 200f, 32f), 
            direction = new Vector3(-1f, 0f, -0.5f), 
            influence = 0.25f 
        };

        static TradeWindRegion southernEasterlies = new TradeWindRegion
        {
            name = "southern easterlies",
            limits = Rect.MinMaxRect(-200, -30f, 200, 15),
            direction = new Vector3(-1f, 0f, 0.5f),
            influence = 0.25f
        };
        static TradeWindRegion southernWesterlies = new TradeWindRegion
        {
            name = "southern westerlies",
            limits = Rect.MinMaxRect(-200, -90, 200, -33),
            direction = new Vector3(1f, 0f, -0.5f),
            influence = 0.25f
        };
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(Wind __instance, float ___tradeWindInfluence, float ___minimumMagnitude)
        {
            wind = __instance;
            TradeWinds.defaultTradeWindInfluence = ___tradeWindInfluence;
            defaultMinMagnitude = ___minimumMagnitude;
            TradeWinds.AddRegion(northern);
            TradeWinds.AddRegion(alankh);
            TradeWinds.AddRegion(emerald);
            TradeWinds.AddRegion(southernEasterlies);
            TradeWinds.AddRegion(southernWesterlies);
        }

        [HarmonyPatch("GetCurrentTradeWind")]
        [HarmonyPrefix]
        public static bool GetTradeWindPatch(ref Vector3 __result, ref float ___tradeWindInfluence, ref float ___minimumMagnitude)
        {
            var region = TradeWinds.GetCurrentTradeWindRegion();
            ___minimumMagnitude = defaultMinMagnitude;
            if (region.name != "none")
            {
                ___tradeWindInfluence = region.influence;
                __result = region.direction.normalized;
            }
            else
            {
                ___minimumMagnitude = 0f;
                ___tradeWindInfluence = TradeWinds.defaultTradeWindInfluence;
                __result = Vector3.zero;
            }
            Debug.Log($"Tradewinds: current region = {region.name}");
            return false;
            //return true;
        }

        public static void SampleWinds(int iterations)
        {
            float min = 100f;
            float max = 0f;
            float sum = 0f;
            for (int i = 0; i < iterations; i++)
            {
                AccessTools.Method(typeof(Wind), "SetNewWindTarget").Invoke(wind, null);
                float current = wind.outCurrentBaseWind.magnitude;
                if (current < min) min = current;
                if (current > max) max = current;
                sum += current;
                if (current < 0.5)
                {
                    Debug.Log($"Wind magnitude = {wind.outCurrentBaseWind.magnitude}, breaking");
                    break;
                }
            }

            //Debug.Log($"Wind magnitudes: min = {min}, max = {max}, avg = {sum / iterations}");
        }
    }

}
