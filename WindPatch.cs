using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace AroundTheWorld
{
    /*[HarmonyPatch(typeof(FloatingOriginManager), "Start")]
    internal class WindPatch
    {
        public static void Postfix()
        {
            Vector3 globeOffset = (Vector3)Traverse.Create(FloatingOriginManager.instance).Field("globeOffset").GetValue();

            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.parent = Refs.shiftingWorld.transform;
            obj.name = "Polar cap";
            obj.tag = "Terrain";
            obj.transform.localScale = new Vector3(100000f, 100f, 100000f);
            Vector3 targetPos = new Vector3(0f, 0f, 80f) * 9000 + globeOffset + new Vector3(0f, -30f, 0f);
            obj.transform.position = FloatingOriginManager.instance.RealPosToShiftingPos(targetPos);

            //var comp = obj.AddComponent<IslandHorizon>();
        }
    }*/
}
