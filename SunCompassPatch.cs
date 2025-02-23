using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(ShipItem), "Awake")]
    internal static class SunCompassPatch
    {
        public static void Postfix(ShipItem __instance)
        {
            if (!Plugin.sunCompassPatch.Value) return;
            
            if (__instance is ShipItemCompass && __instance.gameObject.name.StartsWith("86 sun compass A"))
            {
                Compass oldComponent = __instance.GetComponentInChildren<Compass>();
                if (oldComponent == null) return;
                oldComponent.enabled = false;
                __instance.inventoryRotation = 180f;
                __instance.inventoryRotationX = 270;

                Transform rimContainer = new GameObject{ name = "compass" }.transform;
                rimContainer.SetParent(oldComponent.transform, false);
                oldComponent.transform.Find("rim").SetParent(rimContainer);
                oldComponent.transform.Find("letters").SetParent(rimContainer);
                Compass newComponent = rimContainer.gameObject.AddComponent<Compass>();
                Traverse trav = Traverse.Create(newComponent);
                trav.Field("magnetism").SetValue(1.5f);
                trav.Field("northAngles").SetValue(new Vector3(0f, 180f, 0f));
                trav.Field("lockAxis").SetValue(new Vector3(0f, 1f, 0f));
            }
        }
    }
}
