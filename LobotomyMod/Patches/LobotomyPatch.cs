using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using UnityEngine;

namespace LobotomyMod.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class LobotomyPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        static void patch(PlayerControllerB __instance)
        {
            if(__instance.gameObject.GetComponent<LobotomyControllerB>() == null)
            {
                if(!__instance.gameObject.activeInHierarchy)
                {
                    LobotomyMod.LogSource.LogDebug($"Player {__instance.gameObject.name} isn't active in hierarchy - skipping the lobotomy controller.");
                    return;
                }

                __instance.StartCoroutine(WaitForLocalPlayerControllerAndAddALobotomyControllerIfItIsTheLocalPlayer(__instance));
            }
        }

        //long coroutine name go brrrr
        private static IEnumerator WaitForLocalPlayerControllerAndAddALobotomyControllerIfItIsTheLocalPlayer(PlayerControllerB other)
        {
            while(GameNetworkManager.Instance.localPlayerController == null)
            {
                //wait until the next frame for localPlayerController
                yield return null;
            }

            if(other == GameNetworkManager.Instance.localPlayerController)
            {
                LobotomyMod.LogSource.LogDebug($"Ownership of player controller for {other.name} confirmed!");
                other.gameObject.AddComponent<LobotomyControllerB>();
            }
            else
            {
                LobotomyMod.LogSource.LogDebug($"Ownership of player controller for {other.name} failed.");
            }

        }
    }

    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("EndOfGame")]
        [HarmonyPrefix]
        private static void EndOfGamePatch()
        {
            if(LobotomyControllerB.instance != null)
            {
                LobotomyControllerB.instance.isActive = false;
            }
        }

        [HarmonyPatch("StartGame")]
        [HarmonyPrefix]
        private static void StartOfGamePatch()
        {
            if(LobotomyControllerB.instance != null)
            {
                LobotomyControllerB.instance.isActive = true;
            }
        }
    }
}
