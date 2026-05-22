using HarmonyLib;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(OceanFishes))]
    internal static class FishPickerPatch
    {
        [HarmonyPatch("GetClosestFish")]
        public static void Prefix(ref float latitude)
        {
            if (!Plugin.fishPickerPatch.Value) return;
            latitude = Mathf.Abs(latitude);
        }
    }
}
