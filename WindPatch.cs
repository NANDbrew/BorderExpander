using HarmonyLib;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(Wind))]
    public static class WindPatch
    {
        static Wind wind;
        static float defaultMinMagnitude;

        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        public static void AwakePatch(Wind __instance, float ___tradeWindInfluence, float ___minimumMagnitude)
        {
            wind = __instance;
            TradeWinds.defaultTradeWindInfluence = ___tradeWindInfluence;
            defaultMinMagnitude = ___minimumMagnitude;

            TradeWinds.ReadDefs();
            TradeWinds.SortRegions();
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
                __result = new Vector3(region.dirX, 0, region.dirY).normalized;
            }
            else
            {
                ___minimumMagnitude = 0f;
                ___tradeWindInfluence = TradeWinds.defaultTradeWindInfluence;
                __result = Vector3.zero;
            }
            Debug.Log($"Tradewinds: current region = {region.name}");
            return false;
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
