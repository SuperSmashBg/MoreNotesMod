using GameNetcodeStuff;
using HarmonyLib;
using System;

namespace MoreNotesMod.Patches
{
    [HarmonyPatch]
    public class StatPatches
    {

        public static float[] danceTotal;

        //Patches after the WritePlayerNotes method
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPostfix]
        public static void WrirtePlayerNotesPatch(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++) //All players
            {
                if (__instance.gameStats.allPlayerStats[i].isActivePlayer && !__instance.allPlayerScripts[i].isPlayerDead)
                {
                    __instance.gameStats.allPlayerStats[i].playerNotes.Add($"Took {__instance.gameStats.allPlayerStats[i].stepsTaken} steps"); //Adds step count
                }
            }

            

        }

        [HarmonyPatch(typeof(PlayerControllerB), "StopPerformingEmoteClientRpc")]
        [HarmonyPostfix]
        public static void StopPerformingEmoteClientRpcPatch(PlayerControllerB __instance)
        {
            //Gets the animation number from the player model
            //Apparently this value is never over-writen
            //However I think that should not be an issue?
            if (__instance.playerBodyAnimator.GetInteger("emoteNumber") == 1) { 
                //Checked the code, timeSinceStartingEmote is set after the PRC call
                //The (int) conversion gets the id for the player in the array (somehow)
                //danceTotal will be called in my WritePlayerNotes Patch
                danceTotal[(int)(checked((IntPtr)__instance.playerClientId))] += __instance.timeSinceStartingEmote;
            }
        }

    
    }
   
}
