using HarmonyLib;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(RegionBlender))]
    internal class RegionBlenderPatch
    {
        const float northLat = 40;
        const float midLat = 30;
        const float eqLat = 0;

        public static bool regionByLat = false;
        public static bool switchingToLat = false;
        internal static float enterDist = 0;
        internal static float exitDist = 0;
        internal static Region lastRegion = null;
        public static Dictionary<float, Region> latitudeRegions;
        public static float blendDist;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPatch()
        {
            latitudeRegions = new Dictionary<float, Region>
            {
                { eqLat, new GameObject("Region Equatorial").AddComponent<Region>() },
                { midLat, new GameObject("Region Mid lat").AddComponent<Region>() },
                { northLat, new GameObject("Region Northern").AddComponent<Region>() }
            };
            latitudeRegions[eqLat].gameObject.AddComponent<RegionAddon>().latitude = eqLat;
            latitudeRegions[midLat].gameObject.AddComponent<RegionAddon>().latitude = midLat;
            latitudeRegions[northLat].gameObject.AddComponent<RegionAddon>().latitude = northLat;
            foreach (var region in latitudeRegions.Values)
            {
                region.portRegion = PortRegion.none;
                //CopyWeatherSets(RegionBlender.instance.initialRegion, region);
            }
            foreach (var region in GameObject.FindObjectsOfType<Region>())
            {
                if (region.GetComponent<RegionAddon>() == null) region.gameObject.AddComponent<RegionAddon>();
                Debug.Log("did we add addon?");
                if (region.portRegion == PortRegion.alankh)
                {
                    CopyWeatherSets(region, latitudeRegions[midLat]);
                }
                else if (region.portRegion == PortRegion.medi && region.gameObject.name.Contains("East"))
                {
                    region.GetComponent<RegionAddon>().blendDist = 4000;
                    CopyWeatherSets(region, latitudeRegions[northLat]);
                }
                else if (region.portRegion == PortRegion.emerald && region.gameObject.name.Contains("Lagoon"))
                {
                    region.GetComponent<RegionAddon>().blendDist = 1000;
                    CopyWeatherSets(region, latitudeRegions[eqLat]);
                }
            }
        }

        public static void CopyWeatherSets(Region initialRegion, Region targetRegion)
        {
            targetRegion.clearWeather = new WeatherSet();
            targetRegion.cloudyWeather = new WeatherSet();
            targetRegion.rainWeather = new WeatherSet();
            targetRegion.stormWeather = new WeatherSet();
            WeatherSet.CopyFrom(ref targetRegion.clearWeather, initialRegion.clearWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.cloudyWeather, initialRegion.cloudyWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.rainWeather, initialRegion.rainWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.stormWeather, initialRegion.stormWeather, instantiateMaterials: true);
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void UpdatePatch(Transform ___player, Region ___currentTargetRegion)
        {
            if (___currentTargetRegion.portRegion == PortRegion.none)
            {
                var latitudeRegion = GetLatitudeRegion(___player);
                if (latitudeRegion != ___currentTargetRegion)
                {
                    RegionBlender.instance.SwitchRegion(latitudeRegion);
                }
            }

        }

        [HarmonyPatch("UpdateBlend")]
        [HarmonyPrefix]
        public static bool BlendPatch(Transform ___player, Region ___currentTargetRegion, ref Region ___blendedRegion)
        {
            float num;
            float value;
            if (regionByLat)
            {
                //float num;
                if (switchingToLat)
                {
                    value = Vector3.Distance(___player.position, lastRegion.transform.position);
                    num = Mathf.InverseLerp(exitDist, exitDist + blendDist, value);
                    if (num > 0.999f) switchingToLat = false;
                }
                else
                {
                    value = Mathf.Abs(Mathf.Abs(___currentTargetRegion.GetComponent<RegionAddon>().latitude - FloatingOriginManager.instance.GetGlobeCoords(___player).z));
                    num = Mathf.InverseLerp(10, 9.5f, value);
                }
                WeatherSet.BlendSets(ref ___blendedRegion.clearWeather, lastRegion.clearWeather, ___currentTargetRegion.clearWeather, num);
                WeatherSet.BlendSets(ref ___blendedRegion.cloudyWeather, lastRegion.cloudyWeather, ___currentTargetRegion.cloudyWeather, num);
                WeatherSet.BlendSets(ref ___blendedRegion.rainWeather, lastRegion.rainWeather, ___currentTargetRegion.rainWeather, num);
                WeatherSet.BlendSets(ref ___blendedRegion.stormWeather, lastRegion.stormWeather, ___currentTargetRegion.stormWeather, num);
                ___blendedRegion.stormRange = Mathf.Lerp(___blendedRegion.stormRange, ___currentTargetRegion.stormRange, num);
                ___blendedRegion.windChaos = Mathf.Lerp(___blendedRegion.windChaos, ___currentTargetRegion.windChaos, num);
                ___blendedRegion.windDirChaos = Mathf.Lerp(___blendedRegion.windDirChaos, ___currentTargetRegion.windDirChaos, num);
                ___blendedRegion.stormCount = Mathf.RoundToInt(Mathf.Lerp(___blendedRegion.stormCount, ___currentTargetRegion.stormCount, num));
                //Debug.Log(num);
                return false;
            }
            switchingToLat = false;
            value = Vector3.Distance(___player.position, ___currentTargetRegion.transform.position);
            num = Mathf.InverseLerp(enterDist, enterDist - blendDist, value);
            WeatherSet.BlendSets(ref ___blendedRegion.clearWeather, ___blendedRegion.clearWeather, ___currentTargetRegion.clearWeather, num);
            WeatherSet.BlendSets(ref ___blendedRegion.cloudyWeather, ___blendedRegion.cloudyWeather, ___currentTargetRegion.cloudyWeather, num);
            WeatherSet.BlendSets(ref ___blendedRegion.rainWeather, ___blendedRegion.rainWeather, ___currentTargetRegion.rainWeather, num);
            WeatherSet.BlendSets(ref ___blendedRegion.stormWeather, ___blendedRegion.stormWeather, ___currentTargetRegion.stormWeather, num);
            ___blendedRegion.stormRange = Mathf.Lerp(___blendedRegion.stormRange, ___currentTargetRegion.stormRange, num);
            ___blendedRegion.windChaos = Mathf.Lerp(___blendedRegion.windChaos, ___currentTargetRegion.windChaos, num);
            ___blendedRegion.windDirChaos = Mathf.Lerp(___blendedRegion.windDirChaos, ___currentTargetRegion.windDirChaos, num);
            ___blendedRegion.stormCount = Mathf.RoundToInt(Mathf.Lerp(___blendedRegion.stormCount, ___currentTargetRegion.stormCount, num));

            //Debug.Log(num);

            return false;
        }
        [HarmonyPatch("SwitchRegion")]
        [HarmonyPrefix]
        public static void SwitchRegionPrefix(Region newRegion, Transform ___player, Region ___currentTargetRegion)
        {
            lastRegion = ___currentTargetRegion;
            enterDist = Vector3.Distance(___player.position, newRegion.transform.position);
            blendDist = newRegion.GetComponent<RegionAddon>().blendDist;
            Hints.instance.ShowExternalHint($"Entering {newRegion.gameObject.name} from {lastRegion.gameObject.name}");

        }

        public static Region GetLatitudeRegion(Transform player)
        {
            float lat = FloatingOriginManager.instance.GetGlobeCoords(player).z;
            Region closest = latitudeRegions.FirstOrDefault().Value;
            float diff = 99;
            foreach (float key in latitudeRegions.Keys)
            {
                float curDiff = Mathf.Abs(key - lat);
                if (curDiff < diff)
                {
                    closest = latitudeRegions[key];
                    diff = curDiff;
                }
            }
            return closest;
        }
    }


    public class RegionAddon : MonoBehaviour
    {
        public Region region;
        public float latitude;
        public float blendDist = 2000f;

        public void Awake()
        {
            region = GetComponent<Region>();
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                RegionBlenderPatch.regionByLat = false;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && RegionBlender.instance != null && ReferenceEquals((Region)AccessTools.Field(typeof(RegionBlender), "currentTargetRegion").GetValue(RegionBlender.instance), region))
            {
                RegionBlenderPatch.switchingToLat = true;
                RegionBlenderPatch.regionByLat = true;
                RegionBlenderPatch.exitDist = Vector3.Distance(other.transform.position, region.transform.position);
                RegionBlenderPatch.lastRegion = region;
                RegionBlender.instance.SwitchRegion(RegionBlenderPatch.GetLatitudeRegion(other.transform));
            }
        }
    }
}
