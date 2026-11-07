using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BorderExpander
{
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
/*    [BepInDependency("com.raddude.climate", BepInDependency.DependencyFlags.SoftDependency)]*/
    //[BepInDependency("com.nandbrew.scrambledseas", BepInDependency.DependencyFlags.SoftDependency)]

    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.nandbrew.borderexpander";
        public const string PLUGIN_NAME = "Border Expander";
        public const string PLUGIN_VERSION = "0.6.1";

/*        internal static bool climateInstalled;*/
        internal static bool scrambledSeas;

        public static float northLimit = 46f;
        public static float southLimit = 26f;
        public static float eastLimit = 32f;
        public static float westLimit = -12f;
        public static float padding = 3f;
        internal static bool initialized = false;

        //--settings--
        internal static ConfigEntry<bool> freeSail;
        internal static ConfigEntry<bool> sunPatch;
        internal static ConfigEntry<bool> sunCompassPatch;
        internal static ConfigEntry<bool> fishPickerPatch;
        internal static ConfigEntry<bool> debugFreeSail;
        internal static ConfigEntry<bool> debugRegionHint;
        internal static ConfigEntry<bool> perfBoost;

        public static Plugin instance;
        private static Harmony harmony;

        private void Awake()
        {
            instance = this;
            harmony = new Harmony(PLUGIN_ID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            MethodInfo original1 = AccessTools.Method(typeof(Wind), "Awake");
            MethodInfo patch1 = AccessTools.Method(typeof(WindPatch), "AwakePatch");
            MethodInfo original2 = AccessTools.Method(typeof(Wind), "GetCurrentTradeWind");
            MethodInfo patch2 = AccessTools.Method(typeof(WindPatch), "GetTradeWindPatch");

/*            if (!Chainloader.PluginInfos.ContainsKey("com.raddude.climate"))
            {
                climateInstalled = false;
                harmony.Patch(original1, postfix: new HarmonyMethod(patch1));
                harmony.Patch(original2, prefix: new HarmonyMethod(patch2));
                Debug.Log("BE: patching trade winds");
            }
            else
            {
                climateInstalled = true;
                Debug.Log("BE: Climate mod installed, NOT patching trade winds");
            }*/
            if (Chainloader.PluginInfos.ContainsKey("com.nanbrew.scrambledseas"))
            {
                scrambledSeas = true;
            }

            freeSail = Config.Bind("Settings", "Free Sailing", false, new ConfigDescription("Allow sailing anywhere between 70°N and 70°S (allows east/west circumnavigation)"));
            sunPatch = Config.Bind("Settings", "Sun Patch", true, new ConfigDescription("Adjust the sun to account for latitude. will make sunrise a bit earlier at Aestrin and a bit later at FFL"));
            sunCompassPatch = Config.Bind("Settings", "Sun Compass Patch", false, new ConfigDescription("Sun compass requires manual alignment, so it works south of the equator (requires a restart to take effect)"));
            fishPickerPatch = Config.Bind("Settings", "Fish Picker Patch", false, new ConfigDescription("Duplicate fish latitudes in the southern hemisphere"));
            debugFreeSail = Config.Bind("zDebug", "Debug Free Sailing", false, new ConfigDescription("Bypass all limits", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            debugRegionHint = Config.Bind("zDebug", "Debug Region Hint", false, new ConfigDescription("Show notification when changing regions", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            perfBoost = Config.Bind("zDebug", "Performance boost", true, new ConfigDescription("Reduce the frequency of region blend updates and skip them altogether when already blended to one region"));
            
            freeSail.SettingChanged += (sender, args) => UpdateLimits();

            StartCoroutine(InitializeLimits());
        }

        public static void UpdateLimits()
        {
            Debug.Log("updating world limits");
            if (freeSail.Value)
            {
                Debug.Log("defaulting world limits");

                northLimit = 80f;
                southLimit = -80f;
                eastLimit = 185f;
                westLimit = -185f;
                return;
            }
            if (scrambledSeas) // should probably have check for scramble
            {
                northLimit = 37f;
                southLimit = 35f;
                eastLimit = 11f;
                westLimit = 9f;

            }
            else
            {
                northLimit = 46f;
                southLimit = 26f;
                eastLimit = 32f;
                westLimit = -12f;
            }
            for (int i = 0; i < Refs.islands.Length; i++)
            {
                var island = Refs.islands[i];
                if (island == null) continue;
                Vector3 loc = FloatingOriginManager.instance.GetGlobeCoords(Refs.islands[i]);
                if (loc.z + padding > northLimit) northLimit = Mathf.Round(loc.z + padding);
                if (loc.z - padding < southLimit) southLimit = Mathf.Round(loc.z - padding);
                if (loc.x + padding > eastLimit) eastLimit = Mathf.Round(loc.x + padding);
                if (loc.x - padding < westLimit) westLimit = Mathf.Round(loc.x - padding);
            }
/*            if (climateInstalled)
            {
                UpdateClimate();
            }*/
        }

/*        public static void UpdateClimate()
        {
            Climate_Plugin.MinWindLatitude = Mathf.FloorToInt(southLimit - 2);
            Climate_Plugin.MaxWindLatitude = Mathf.CeilToInt(northLimit + 2);
            
            Climate_Plugin.MinWindLongitude = Mathf.FloorToInt(westLimit - 2);
            Climate_Plugin.MaxWindLongitude = Mathf.CeilToInt(eastLimit + 2);

            Climate_Plugin.MinCellSpawnLatitude = Mathf.RoundToInt(southLimit + 4);
            Climate_Plugin.MaxCellSpawnLatitude = Mathf.RoundToInt(northLimit - 4);

            Climate_Plugin.MinCellSpawnLongitude = Mathf.RoundToInt(westLimit + 6);
            Climate_Plugin.MaxCellSpawnLongitude = Mathf.RoundToInt(eastLimit - 10);

            float currentArea = (northLimit - southLimit) * (eastLimit + Mathf.Abs(westLimit));

            Climate_Plugin.MaxPressureCells = Mathf.FloorToInt(6 * (currentArea / 880));
            //Climate.WindService.UpdateDailyWindField();
            Debug.Log("BE: max pressure cells = " + Climate_Plugin.MaxPressureCells);
        }*/

        private IEnumerator InitializeLimits()
        {
            yield return new WaitUntil(() => GameState.justStarted);
            if (initialized) yield break;
            UpdateLimits();
            initialized = true;
        }
    }

    [HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
    static class SaveLoadManagerPatch
    {
        [HarmonyPriority(Priority.VeryHigh)]
        private static void Prefix()
        {
            Plugin.scrambledSeas = GameState.modData.ContainsKey("ScrambledSeas");
            Plugin.UpdateLimits();
            Plugin.initialized = true;
        }
    }
}
