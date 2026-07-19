using HarmonyLib;

namespace BorderExpander
{
    [HarmonyPatch(typeof(IslandHorizon), "SetHeight")]
    internal static class IslandHorizonPatch
    {
        private static void Prefix(ref float y, float ___sunkHeight)
        {
            if (y < ___sunkHeight)
            {
                y = ___sunkHeight;
            }
        }
    }

}
