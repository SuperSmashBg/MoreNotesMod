using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;


namespace MoreNotesMod.Patches
{
    [HarmonyPatch]
    public class StatPatches
    {
        static readonly ManualLogSource mls = Plugin.Instance.mls;

        public static float[] danceTotal;
        public static int conPlayerReq = 0;

        [HarmonyPatch(typeof(StartOfRound), "StartGame")]
        [HarmonyPostfix]
        public static void PatchStartGame(StartOfRound __instance)
        {
           //Resets and sets the danceTotal array
           danceTotal = new float[__instance.gameStats.allPlayerStats.Length];
           
        }

        //Patches before the WritePlayerNotes method
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPrefix]
        public static bool WrirtePlayerNotesPatch(StartOfRound __instance)
        {
            //Holds all of the data related to player notes
            List<Tuple<int, string>> playerNoteStorage = new List<Tuple<int, string>>();
            
            //Quick ref to an important value
            var q_allPlayerStats =  __instance.gameStats.allPlayerStats;
            var q_allPlayerScripts = __instance.allPlayerScripts;

            //copy of the code that sets isActivePlayer
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
                    .Where(stats => stats.isActivePlayer).ToArray() //Selects where players are active
                    .Select(stats => stats.stepsTaken).ToArray()
                    .Min();
                int minStepsId = Array.IndexOf(q_allPlayerStats, q_allPlayerStats.First(stats => stats.stepsTaken == minSteps));
                if (__instance.connectedPlayersAmount > conPlayerReq && minSteps < 10)
                    playerNoteStorage.Add(new Tuple<int, string>(minStepsId, "The laziest employee."));
                mls.LogInfo($"Marked player {minStepsId + 1} as laziest taking {minSteps} steps.");
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
                mls.LogInfo($"Marked player {maxTurnsId + 1} as most paranoid with {maxTurns} turns.");
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
                mls.LogInfo($"Marked player {maxDamageTakenId + 1} as the most injured taking {maxDamageTaken} damage.");
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
                mls.LogInfo($"Marked player {maxProfitId + 1} as the most profitable making {maxProfit} credits.");
            }

            //Adds best dancer
            {
                float maxDance = danceTotal
                    .Where((stats, index) => q_allPlayerStats[index].isActivePlayer).ToArray() //Where player is active
                    .Max(); //Get Max
                int maxDanceId = Array.IndexOf(danceTotal, maxDance);

                //Player must be dancing for so long
                if (__instance.connectedPlayersAmount > conPlayerReq && maxDance >= 20f)
                    playerNoteStorage.Add(new Tuple<int, string>(maxDanceId, $"Hit the dance floor."));
                mls.LogInfo($"Marked player {maxDanceId + 1} as the best dancer dancing {maxDance:0} seconds.");
            }

            //Picks Notes randomly and displays them
            {
                System.Random notePicker = new System.Random(__instance.randomMapSeed); //Choses what notes to display with the synced seed
                int notePickCount = 4; //Pick a number of things to pick
                int pickedID; //Id of picked element
                Tuple<int, string> pickedNote; //Data for picked note
                List<Tuple<int, string>> pickedNoteList = new List<Tuple<int, string>>(); //Make a new list for the notes

                mls.LogInfo($"Picking {notePickCount} notes from list.");

                for (int i = 0; i < notePickCount; i++)
                {
                    if (playerNoteStorage.Count() == 0) { break; }
                    pickedID = notePicker.Next(playerNoteStorage.Count());
                    pickedNote = playerNoteStorage[pickedID];
                    playerNoteStorage.RemoveAt(pickedID);
                    pickedNoteList.Add(pickedNote);
                }

                mls.LogInfo($"Picked notes! Preparing to add to playerStats.");

                //Adds all player notes
                foreach (Tuple<int, string> playerNote in pickedNoteList)
                {
                    q_allPlayerStats[playerNote.Item1].playerNotes.Add(playerNote.Item2);
                }

                mls.LogInfo("Notes added!");
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

        //Keeps track of a player getting jetpack controlls
        //Is called by activate items which is synced
        [HarmonyPatch(typeof(JetpackItem), "ActivateJetpack")]
        [HarmonyPostfix]
        public static void ActivateJetpackPatch() 
        {
            TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
            if (timeOfDay != null)
            {

            }
        }

        //Keeps track of a player getting jetpack controlls
        //Is called by synced RPC's
        [HarmonyPatch(typeof(JetpackItem), "DeativateJetpack")]
        [HarmonyPostfix]
        public static void DeactivateJetpackPatch()
        {
            TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
            if (timeOfDay != null)
            {

            }
        }
    }
   
}
