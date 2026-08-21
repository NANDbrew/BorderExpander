using Crest;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

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
            if (Plugin.debugFreeSail.Value) return false;

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
            #region circumnavigation
            if (GameState.justWokeUp && (___currentGlobePos.x > 180 || ___currentGlobePos.x < -180))
            {
                //Debug.Log("Transposing Boat");
                float sub = 180 * 9000;
                Vector3 currentPos = FloatingOriginManager.instance.ShiftingPosToRealPos(GameState.lastBoat.transform.position);
                float transpose = -sub + (currentPos.x - sub);
                if (___currentGlobePos.x < 0) transpose = sub + (currentPos.x + sub);

                Vector3 targetPos = new Vector3(transpose, currentPos.y, currentPos.z);
                //__instance.StartCoroutine(MoveBoatToPos(GameState.lastBoat, targetPos));
                var boat = GameState.lastBoat;
                PurchasableBoat[] nearbyBoats = new PurchasableBoat[0];
                Vector3[] relVectors = new Vector3[0];

                nearbyBoats = GameObject.FindObjectsOfType<PurchasableBoat>().Where(o => (o.transform != boat && o.isPurchased() && (o.transform.position - boat.position).sqrMagnitude < 10000)).ToArray();
                relVectors = new Vector3[nearbyBoats.Length];
                for (int i = 0; i < nearbyBoats.Length; i++)
                {
                    relVectors[i] = nearbyBoats[i].transform.position - GameState.lastBoat.position;
                }

                FloatingOriginManager.instance.StartCoroutine(BoatMover.IMoveBoat(targetPos, boat.rotation, boat));

                if (nearbyBoats.Length > 0 && relVectors.Length > 0)
                {
                    for (int j = 0; j < nearbyBoats.Length; j++)
                    {
                        Transform nb = nearbyBoats[j].transform;
                        FloatingOriginManager.instance.StartCoroutine(BoatMover.IMoveBoat(targetPos + relVectors[j], nb.transform.rotation, nb));

                    }
                }
            }
            #endregion
            return false;
        }
        private static Vector3[] PrepareBoat(PurchasableBoat boat, Vector3 referencePos)
        {
            BoatMooringRopes ropes = boat.GetComponent<BoatMooringRopes>();
            Vector3 anchorPos = Vector3.zero;
            if (ropes.GetAnchorController()?.joint.gameObject is GameObject anchor)
            {
                anchorPos = anchor.transform.position - boat.transform.position;
            }
            boat.GetComponent<BoatProbes>().dontUpdateVelocity = true;
            Debug.Log("BE: prepared boat: " + boat.name);
            return new Vector3[] { boat.transform.position - referencePos, anchorPos };
        }

        private static void MoveBoat(PurchasableBoat boat, Vector3 targetPos, Vector3 anchorPos, Vector3 referencePos)
        {
            boat.transform.position = targetPos + referencePos;
            BoatMooringRopes ropes = boat.GetComponent<BoatMooringRopes>();
            if (ropes.GetAnchorController()?.joint.gameObject is GameObject anchor)
            {
                anchor.transform.position = anchorPos + boat.transform.position;
                anchor.GetComponent<Rigidbody>().velocity = boat.GetComponent<Rigidbody>().velocity;
            }
            boat.GetComponent<BoatProbes>().dontUpdateVelocity = false;
            Debug.Log("BE: moved boat: " + boat.name);
        }

        public static IEnumerator MoveBoatToPos(Transform boat, Vector3 targetPos)
        {
            var pos = PrepareBoat(boat.GetComponent<PurchasableBoat>(), Vector3.zero);

            PurchasableBoat[] nearbyBoats = GameObject.FindObjectsOfType<PurchasableBoat>().Where(o => (o.transform != boat && o.isPurchased() && (o.transform.position - boat.position).sqrMagnitude < 100000)).ToArray();
            var relVectors = new Vector3[nearbyBoats.Length][];
            for (int i = 0; i < nearbyBoats.Length; i++)
            {
                relVectors[i] = PrepareBoat(nearbyBoats[i], pos[0]); 
            }
            targetPos = FloatingOriginManager.instance.RealPosToShiftingPos(targetPos);
            
            GameState.recovering = true;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            MoveBoat(boat.GetComponent<PurchasableBoat>(), targetPos, pos[1], Vector3.zero);

            for (int i = 0; i < nearbyBoats.Length; i++)
            {
                MoveBoat(nearbyBoats[i], relVectors[i][0], relVectors[i][1], targetPos);
            }

            yield return new WaitForSeconds(1f);

            GameState.recovering = false;

        }

        /*
                public static IEnumerator MoveBoatToPosa(Transform boat, Vector3 targetPos)
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

                }*/
    }
}

