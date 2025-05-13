using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace BorderExpander
{
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.nandbrew.borderexpander";
        public const string PLUGIN_NAME = "Border Expander";
        public const string PLUGIN_VERSION = "0.3.0";

        public static float northLimit = 46f;
        public static float southLimit = 26f;
        public static float eastLimit = 32f;
        public static float westLimit = -12f;
        public static float padding = 6f;
        internal static bool initialized = false;

        //--settings--
        internal static ConfigEntry<bool> freeSail;
        internal static ConfigEntry<bool> sunPatch;
        internal static ConfigEntry<bool> sunCompassPatch;
        internal static ConfigEntry<bool> debugFreeSail;

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_ID);

            freeSail = Config.Bind("Settings", "Free Sailing", false, new ConfigDescription("Allow sailing anywhere between 70°N and 70°S (allows east/west circumnavigation)"));
            sunPatch = Config.Bind("Settings", "Sun Patch", true, new ConfigDescription("Adjust the sun's behavior around sunrise and sunset. will make sunrise a bit earlier at Aestrin and a bit later at FFL"));
            sunCompassPatch = Config.Bind("Settings", "Sun Compass Patch", false, new ConfigDescription("sun compass requires manual alignment, so it works south of the equator (requires a restart to take effect)"));

            debugFreeSail = Config.Bind("zDebug", "Debug Free Sailing", false, new ConfigDescription("bypass all limits", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            freeSail.SettingChanged += (sender, args) => UpdateLimits();

            StartCoroutine(InitializeLimits());
        }

        public static void UpdateLimits()
        {
            Debug.Log("updating world limits");
            if (freeSail.Value)
            {
                Debug.Log("defaulting world limits");

                northLimit = 70f;
                southLimit = -70;
                eastLimit = float.PositiveInfinity;
                westLimit = float.NegativeInfinity;
                return;
            }

            northLimit = 46f;
            southLimit = 26f;
            eastLimit = 32f;
            westLimit = -12f;

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

        }

        private IEnumerator InitializeLimits()
        {
            yield return new WaitUntil(() => GameState.justStarted);
            UpdateLimits();
            initialized = true;
        }
    }
}
