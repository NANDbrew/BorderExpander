using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace BorderExpander
{
    [HarmonyPatch(typeof(Sun))]
    internal static class SunPatches
    {
        const float sunriseMult = 1f / 64f;
        const float dayMult = 1f / 40f;
        const float offset = 6.2f;
        static bool isReset = false;
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        internal static void SunPatch()
        {
            if (Plugin.sunPatch.Value)
            {
                float baseNum = Mathf.Abs(FloatingOriginManager.instance.GetGlobeCoords(Refs.observerMirror.transform).z);
                Sun.sun.sunriseStart = -Mathf.Exp(baseNum * sunriseMult) + offset;
                Sun.sun.dayBorder = Mathf.Exp(baseNum * dayMult) + offset;
                isReset = false;
            }
            else if (!isReset)
            {
                Sun.sun.dawnBorder = 6.6f;
                Sun.sun.sunriseStart = 4.44f;
                Sun.sun.dayBorder = 8;
                isReset = true;
            }
        }
    }
}
// -- vanilla values --
// sunrise start: 4.44
// dawn border: 6.6
// day border: 8
