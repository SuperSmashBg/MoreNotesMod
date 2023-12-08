using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MoreNotesMod.Patches
{
    [HarmonyPatch]
    public class StatPatches
    {

        public static float[] danceTotal;
        public static int conPlayerReq = 0;

        [HarmonyPatch(typeof(StartOfRound), "StartGame")]
        [HarmonyPostfix]
        public static void PatchStartGame(StartOfRound __instance)
        {
           //Resets and sets the danceTotal array
           danceTotal = new float[__instance.gameStats.allPlayerStats.Length];
           
        }

        //Patches after the WritePlayerNotes method
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPrefix]
        public static bool WrirtePlayerNotesPatch(StartOfRound __instance)
        {
            //Holds all of the data related to player notes
            List<Tuple<int, string>> playerNoteStorage = new List<Tuple<int, string>>();
            
            //Quick ref to an important value
            var q_allPlayerStats =  __instance.gameStats.allPlayerStats;
            var q_allPlayerScripts = __instance.allPlayerScripts;

            //Refactor of the code that sets isActivePlayer
            for (int i = 0; i < q_allPlayerStats.Length; i++)
            {
                q_allPlayerStats[i].isActivePlayer =
                    (q_allPlayerScripts[i].disconnectedMidGame
                    || q_allPlayerScripts[i].isPlayerDead
                    || q_allPlayerScripts[i].isPlayerControlled);
            }

            //Refactor of code that gets the min step player
            {
                int minSteps = q_allPlayerStats
                    .Where(stats => stats.isActivePlayer).ToArray()
                    .Select(stats => stats.stepsTaken).ToArray()
                    .Min();
                int minStepsId = Array.IndexOf(q_allPlayerStats, q_allPlayerStats.First(stats => stats.stepsTaken == minSteps));
                if (__instance.connectedPlayersAmount > conPlayerReq && minSteps < 10)
                    playerNoteStorage.Add(new Tuple<int, string>(minStepsId, "The laziest employee."));
            }

            //Refactor of code that gets the max turns player
            {
                int maxTurns = q_allPlayerStats
                    .Where((stats) => stats.isActivePlayer).ToArray()
                    .Select((stats) => stats.turnAmount).ToArray()
                    .Max();
                int maxTurnsId = Array.IndexOf(q_allPlayerStats, q_allPlayerStats.First(stats => stats.turnAmount == maxTurns));

                if (__instance.connectedPlayersAmount > conPlayerReq)
                    playerNoteStorage.Add(new Tuple<int, string>(maxTurnsId, "The most paranoid employee."));
            }

            //Refactor of code that gets the most injured
            {
                int maxDamageTaken = q_allPlayerStats
                    .Where((stats) => stats.isActivePlayer).ToArray()
                    .Select((stats) => stats.damageTaken).ToArray()
                    .Max();
                int maxDamageTakenId = Array.IndexOf(q_allPlayerStats, q_allPlayerStats.First(stats => stats.damageTaken == maxDamageTaken));

                if (__instance.connectedPlayersAmount > conPlayerReq)
                    playerNoteStorage.Add(new Tuple<int, string>(maxDamageTakenId, "Sustained the most injuries."));
            }

            //Refactor of code that gets the most injured
            {
                int maxProfit = q_allPlayerStats
                    .Where((stats) => stats.isActivePlayer).ToArray()
                    .Select((stats) => stats.profitable).ToArray()
                    .Max();
                int maxProfitId = Array.IndexOf(q_allPlayerStats, q_allPlayerStats.First(stats => stats.profitable == maxProfit));

                if (__instance.connectedPlayersAmount > conPlayerReq && maxProfit > 50)
                    playerNoteStorage.Add(new Tuple<int, string>(maxProfitId, "Most profitable."));
            }

            //Adds best dancer
            {
                float maxDance = danceTotal
                    .Where((stats, index) => q_allPlayerStats[index].isActivePlayer).ToArray() //Where player is active
                    .Max(); //Get Max
                int maxDanceId = Array.IndexOf(danceTotal, maxDance);

                //Player must be dancing for so long
                if (__instance.connectedPlayersAmount > conPlayerReq && maxDance >= 20f)
                    playerNoteStorage.Add(new Tuple<int, string>(maxDanceId, $"Hit the dance floor for {maxDance:0} seconds"));
            }

           

            //Adds all player notes
            foreach (Tuple<int, string> playerNote in playerNoteStorage)
            {
                __instance.gameStats.allPlayerStats[playerNote.Item1].playerNotes.Add(playerNote.Item2);
            }

            return true; //Tells patcher to skip orignal code

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
