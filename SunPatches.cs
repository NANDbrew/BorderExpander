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

        //static float defaultDawnBorder;
        static float defaultSunriseStart;
        static float defaultDayBorder;

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
                //Sun.sun.dawnBorder = defaultDawnBorder;
                Sun.sun.sunriseStart = defaultSunriseStart;
                Sun.sun.dayBorder = defaultDayBorder;
                isReset = true;
            }
        }
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        internal static void StartPatch()
        {
            //defaultDawnBorder = Sun.sun.dawnBorder;
            defaultSunriseStart = Sun.sun.sunriseStart;
            defaultDayBorder = Sun.sun.dayBorder;
        }
    }
}
// -- vanilla values --
// sunrise start: 4.44
// dawn border: 6.6
// day border: 8
