using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(RegionBlender))]
    internal class RegionBlenderPatch
    {
        static readonly float[] lats = { 0, 20, 50 };
        static readonly string[] latNames = { "Region Equatorial", "Region Mid Latitude", "Region Northern" };

        public static bool regionByLat = false;
        public static bool switchingToLat = false;
        internal static float enterDist = 0;
        internal static float exitDist = 0;
        internal static RegionAddon lastRegion = null;
        internal static Region lastBlend = null;
        public static RegionAddon[] latitudeRegions;
        public static RegionAddon targetReg;
        private static float lerp;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void StartPatch()
        {
            lastBlend = new GameObject("Region blend cache").AddComponent<Region>();
            lastBlend.clearWeather = lastBlend.gameObject.AddComponent<WeatherSet>();
            lastBlend.cloudyWeather = lastBlend.gameObject.AddComponent<WeatherSet>();
            lastBlend.stormWeather = lastBlend.gameObject.AddComponent<WeatherSet>();
            lastBlend.rainWeather = lastBlend.gameObject.AddComponent<WeatherSet>();
            CopyRegionDetails(RegionBlender.instance.initialRegion, lastBlend);
            
            latitudeRegions = new RegionAddon[lats.Length];
            for (int i = 0; i < latitudeRegions.Length; i++)
            {
                var region = new GameObject(latNames[i]).AddComponent<Region>();
                region.portRegion = PortRegion.none;
                region.clearWeather = region.gameObject.AddComponent<WeatherSet>();
                region.cloudyWeather = region.gameObject.AddComponent<WeatherSet>();
                region.stormWeather = region.gameObject.AddComponent<WeatherSet>();
                region.rainWeather = region.gameObject.AddComponent<WeatherSet>();
                var addon = region.gameObject.AddComponent<RegionAddon>();
                addon.latitude = lats[i];
                addon.blendDist = 3000;
                latitudeRegions[i] = addon;
            }
            foreach (var region in GameObject.FindObjectsOfType<Region>())
            {
                var addOn = region.GetComponent<RegionAddon>() ?? region.gameObject.AddComponent<RegionAddon>();
                Debug.Log("did we add addon?");
                if (region.portRegion == PortRegion.alankh)
                {
                    // using al'ankh for both equatorial and mid. need a special one for equatorial someday
                    CopyRegionDetails(region, latitudeRegions[1].region);
                    CopyRegionDetails(region, latitudeRegions[0].region);
                }
                else if (region.portRegion == PortRegion.medi && region.gameObject.name.Contains("East"))
                {
                    addOn.blendDist = 2500;
                    // using chronos for higher latitudes
                    CopyRegionDetails(region, latitudeRegions[2].region);
                }
                else if (region.portRegion == PortRegion.emerald && region.gameObject.name.Contains("Lagoon"))
                {
                    addOn.blendDist = 1000;
                }
            }
        }

        public static void CopyRegionDetails(Region initialRegion, Region targetRegion)
        {
            WeatherSet.CopyFrom(ref targetRegion.clearWeather, initialRegion.clearWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.cloudyWeather, initialRegion.cloudyWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.rainWeather, initialRegion.rainWeather, instantiateMaterials: true);
            WeatherSet.CopyFrom(ref targetRegion.stormWeather, initialRegion.stormWeather, instantiateMaterials: true);
            targetRegion.stormRange = initialRegion.stormRange;
            targetRegion.windChaos = initialRegion.windChaos;
            targetRegion.windDirChaos = initialRegion.windDirChaos;
            targetRegion.stormCount = initialRegion.stormCount;
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
            if (GameState.justStarted || GameState.currentlyLoading || targetReg == null)
            {
                CopyRegionDetails(___blendedRegion, lastBlend);
                return true;
            }

            float value;
            if (regionByLat)
            {
                if (switchingToLat && lastRegion.collider != null)
                {

                    value = Vector3.Distance(___player.position, lastRegion.collider.ClosestPoint(___player.transform.position));
                    lerp = Mathf.InverseLerp(0, targetReg.blendDist, value);
                    if (lerp > 0.99f) switchingToLat = false;
                }
                else
                {
                    switchingToLat = false;
                    value = Mathf.Abs(Mathf.Abs(___currentTargetRegion.GetComponent<RegionAddon>().latitude - FloatingOriginManager.instance.GetGlobeCoords(___player).z));
                    lerp = Mathf.InverseLerp(10, 9.5f, value);
                }

            }
            else
            {
                if (targetReg.collider is SphereCollider sCol)
                {
                    value = -(Vector3.Distance(___player.position, targetReg.transform.position) - sCol.radius);
                }
                else
                {
                    // crazy bs to make collider distance work from inside. Why can't my distance function be signed?
                    Physics.ComputePenetration(___player.GetComponent<Collider>(), ___player.position, ___player.rotation, targetReg.collider, ___currentTargetRegion.transform.position, ___currentTargetRegion.transform.rotation, out Vector3 dir, out value);
                }
                switchingToLat = false;
                lerp = Mathf.InverseLerp(0, targetReg.blendDist, value);
            }
            if (lerp > 0.99f) 
            {
                CopyRegionDetails(___blendedRegion, lastBlend); 
            }
            WeatherSet.BlendSets(ref ___blendedRegion.clearWeather, lastBlend.clearWeather, ___currentTargetRegion.clearWeather, lerp);
            WeatherSet.BlendSets(ref ___blendedRegion.cloudyWeather, lastBlend.cloudyWeather, ___currentTargetRegion.cloudyWeather, lerp);
            WeatherSet.BlendSets(ref ___blendedRegion.rainWeather, lastBlend.rainWeather, ___currentTargetRegion.rainWeather, lerp);
            WeatherSet.BlendSets(ref ___blendedRegion.stormWeather, lastBlend.stormWeather, ___currentTargetRegion.stormWeather, lerp);
            ___blendedRegion.stormRange = Mathf.Lerp(lastBlend.stormRange, ___currentTargetRegion.stormRange, lerp);
            ___blendedRegion.windChaos = Mathf.Lerp(lastBlend.windChaos, ___currentTargetRegion.windChaos, lerp);
            ___blendedRegion.windDirChaos = Mathf.Lerp(lastBlend.windDirChaos, ___currentTargetRegion.windDirChaos, lerp);
            ___blendedRegion.stormCount = Mathf.RoundToInt(Mathf.Lerp(lastBlend.stormCount, ___currentTargetRegion.stormCount, lerp));

            return false;
        }

        [HarmonyPatch("SwitchRegion")]
        [HarmonyPrefix]
        public static void SwitchRegionPrefix(Region newRegion, Region ___currentTargetRegion, Region ___blendedRegion)
        {
            lastRegion = ___currentTargetRegion.GetComponent<RegionAddon>();
            targetReg = newRegion.GetComponent<RegionAddon>();

            if (Plugin.debugRegionHint.Value)
            {
                Hints.instance.ShowExternalHint($"Entering {newRegion.gameObject.name} from {lastRegion.gameObject.name}");
            }

            if (GameState.justStarted)
            {
                CopyRegionDetails(newRegion, lastBlend);
                lastBlend.portRegion = newRegion.portRegion;
            }
            else
            {
                CopyRegionDetails(___blendedRegion, lastBlend);
                lastBlend.portRegion = ___blendedRegion.portRegion;
            }
        }

        public static Region GetLatitudeRegion(Transform player)
        {
            float lat = FloatingOriginManager.instance.GetGlobeCoords(player).z;
            var closest = latitudeRegions.FirstOrDefault();
            float diff = 99;
            foreach (var regAdd in latitudeRegions)
            {
                float curDiff = Mathf.Abs(regAdd.latitude - lat);
                if (curDiff < diff)
                {
                    closest = regAdd;
                    diff = curDiff;
                }
            }
            return closest.region;
        }

        public static IEnumerator WaitForBlend()
        {
            yield return new WaitForSeconds(4);

        }
    }


    public class RegionAddon : MonoBehaviour
    {
        public Region region;
        public float latitude;
        public float blendDist = 2000f;
        public Collider collider;

        public void Awake()
        {
            region = GetComponent<Region>();
            collider = GetComponent<Collider>() ?? null;
        }
        
        public void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                if (collider == null)
                {
                    collider = GetComponent<Collider>();
                }
                RegionBlenderPatch.regionByLat = false;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && RegionBlender.instance != null && ReferenceEquals((Region)AccessTools.Field(typeof(RegionBlender), "currentTargetRegion").GetValue(RegionBlender.instance), region))
            {
                if (collider == null)
                {
                    collider = GetComponent<Collider>();
                }
                RegionBlenderPatch.switchingToLat = true;
                RegionBlenderPatch.regionByLat = true;
                RegionBlender.instance.SwitchRegion(RegionBlenderPatch.GetLatitudeRegion(other.transform));
            }
        }
    }
}
