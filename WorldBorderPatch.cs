using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace BorderExpander
{
    [HarmonyPatch(typeof(WorldBorder), "Update")]
    internal static class WorldBorderPatch
    {
        const string ob_message = "Approaching the world's edge. ";
        public static bool Prefix(WorldBorder __instance, ref Vector3 ___currentGlobePos, ref float ___outTimer, TextMesh ___text)
        {
            bool isOutOfBounds = false;
            if (!Plugin.initialized) return false;
            if (!GameState.playing || GameState.recovering) return false;
            ___currentGlobePos = FloatingOriginManager.instance.GetGlobeCoords(__instance.transform);

            #region soft limits
            if (___currentGlobePos.z < Plugin.southLimit)
            {
                Hints.instance.ShowExternalHint(ob_message + "Turn north");
                if (___currentGlobePos.z < Plugin.southLimit - 1)
                {
                    isOutOfBounds = true;
                    //return true;
                }

            }
            else if (___currentGlobePos.z > Plugin.northLimit)
            {
                Hints.instance.ShowExternalHint(ob_message + "Turn south");
                if (___currentGlobePos.z > Plugin.northLimit + 1)
                {
                    isOutOfBounds = true;
                    //return true;
                }
            }
            if (___currentGlobePos.x < Plugin.westLimit)
            {
                Hints.instance.ShowExternalHint(ob_message + "Turn east");
                //ApplyLimit(Vector3.right * (FloatingOriginManager.instance.GetGlobeCoords(GameState.lastBoat).x - Plugin.westLimit) * forceMult);
                if (___currentGlobePos.x < Plugin.westLimit - 1)
                {
                    isOutOfBounds = true;
                    //return true;
                }
            }
            else if (___currentGlobePos.x > Plugin.eastLimit)
            {
                Hints.instance.ShowExternalHint(ob_message + "Turn west");
                if (___currentGlobePos.x > Plugin.eastLimit + 1)
                {
                    isOutOfBounds = true;
                    //return true;
                }
            }
            #endregion
            #region hard limits
            if (isOutOfBounds)
            {
                ___outTimer -= Time.deltaTime;
                ___text.gameObject.SetActive(value: true);
                ___text.text = "out of game area\nrecovery in " + Mathf.RoundToInt(___outTimer) + " seconds...";
                if (GameState.sleeping) Sleep.instance.WakeUp();
            }
            else
            {
                ___outTimer = 120f;
                ___text.gameObject.SetActive(value: false);
            }
            if (___outTimer <= 0f)
            {
                ___outTimer = 120f;
                Recovery.RecoverPlayer(RecoveryReason.worldBorder);
            }
            #endregion
            // circumnavigation
            if (GameState.justWokeUp && (___currentGlobePos.x > 180 || ___currentGlobePos.x < -180))
            {
                Debug.Log("Transposing Boat");
                float sub = 180 * 9000;
                Vector3 currentPos = FloatingOriginManager.instance.ShiftingPosToRealPos(GameState.lastBoat.transform.position);
                float transpose = -sub + (currentPos.x - sub);
                if (___currentGlobePos.x < 0) transpose = sub + (currentPos.x + sub);

                Vector3 targetPos = new Vector3(transpose, currentPos.y, currentPos.z);
                __instance.StartCoroutine(MoveBoatToPos(GameState.lastBoat, targetPos));
            }

            return false;
        }

        public static void ApplyLimit(Vector3 dir)
        {
            Rigidbody body = GameState.lastBoat.GetComponent<Rigidbody>();
            body.AddForceAtPosition(dir, body.transform.forward * 10);
            //body.AddRelativeTorque(Vector3.right);
        }

        public static IEnumerator MoveBoatToPos(Transform boat, Vector3 targetPos)
        {
            GameState.recovering = true;
            BoatMooringRopes ropes = boat.GetComponent<BoatMooringRopes>();
            //ropes.UnmoorAllRopes();
            ropes.GetAnchorController().ResetAnchor();
            Vector3[] relVectors = new Vector3[0];
            PurchasableBoat[] nearbyBoats = GameObject.FindObjectsOfType<PurchasableBoat>().Where(o => (o.transform != boat && o.isPurchased() && (o.transform.position - boat.position).sqrMagnitude < 100000)).ToArray();
            relVectors = new Vector3[nearbyBoats.Length];
            for (int i = 0; i < nearbyBoats.Length; i++)
            {
                var nearBoat = nearbyBoats[i];
                var nearBoatRopes = nearBoat.GetComponent<BoatMooringRopes>();
                nearBoatRopes.GetAnchorController().ResetAnchor();

                relVectors[i] = nearBoat.transform.position - boat.position;
            }
            boat.position = FloatingOriginManager.instance.RealPosToShiftingPos(targetPos);

            for (int i = 0; i < nearbyBoats.Length; i++)
            {
                nearbyBoats[i].transform.position = relVectors[i] + boat.position;
            }
        
            yield return new WaitForSeconds(1f);

            GameState.recovering = false;

        }
    }
}

