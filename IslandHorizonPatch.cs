using HarmonyLib;

namespace BorderExpander
{
    [HarmonyPatch(typeof(IslandHorizon), "SetHeight")]
    internal static class IslandHorizonPatch
    {
        private static void Prefix(ref float y)
        {
            if (y < -1000)
            {
                y = -1000;
            }
        }
    }

}
