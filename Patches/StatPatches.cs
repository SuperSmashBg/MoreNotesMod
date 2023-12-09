﻿using BepInEx.Logging;
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
        public static float[] activationJetpackPower;
        public static float[] deltaJetpackUsage;

        [HarmonyPatch(typeof(StartOfRound), "StartGame")]
        [HarmonyPostfix]
        public static void PatchStartGame(StartOfRound __instance)
        {
            int playerCount = __instance.gameStats.allPlayerStats.Length;
            //Resets and sets the danceTotal array
            danceTotal = new float[playerCount];
            deltaJetpackUsage = new float[playerCount];
            activationJetpackPower = new float[playerCount];
            mls.LogInfo("PatchStartOfGame Ran.");

        }

        //Patches before the WritePlayerNotes method
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPrefix]
        public static bool WrirtePlayerNotesPatch(StartOfRound __instance)
        {
            //Holds all of the data related to player notes
            List<Tuple<int, string>> playerNoteStorage = new List<Tuple<int, string>>();

            //Quick ref to an important value
            var q_allPlayerStats = __instance.gameStats.allPlayerStats;
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

            //Adds best pilot 
            /*
            { 
                float maxJetpackDelta = deltaJetpackUsage
                    .Where((stats, index) => q_allPlayerStats[index].isActivePlayer).ToArray()
                    .Max();
                int maxJetpackDeltaId = Array.IndexOf(deltaJetpackUsage, maxJetpackDelta);

                //Player must have spent 1/3 jetpack
                if (__instance.connectedPlayersAmount > conPlayerReq && maxJetpackDelta >= 0.33f)
                    playerNoteStorage.Add(new Tuple<int, string>(maxJetpackDeltaId, $"Flys everywhere instead of walking."));
                mls.LogInfo($"Marked Player {maxJetpackDeltaId + 1} as best pilot spending {maxJetpackDelta:0.#} charge.");
            } */

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
            if (__instance.playerBodyAnimator.GetInteger("emoteNumber") == 1)
            {
                //Checked the code, timeSinceStartingEmote is set after the PRC call
                //The (int) conversion gets the id for the player in the array (somehow)
                //danceTotal will be called in my WritePlayerNotes Patch
                danceTotal[(int)(checked((IntPtr)__instance.playerClientId))] += __instance.timeSinceStartingEmote;
            }
        }
        /*
        //Keeps track of a player getting jetpack controlls
        //Is called by activate items which is synced
        [HarmonyPatch(typeof(JetpackItem), "ActivateJetpack")]
        [HarmonyPostfix]
        public static void ActivateJetpackPatch(JetpackItem __instance, ref bool ___jetpackActivatedPreviousFrame)
        {
            //Find the startofround object. This is done by the game and is OK
            //Done to get player count for setting the array correctly
            StartOfRound startOfRound = UnityEngine.Object.FindObjectOfType<StartOfRound>();
            if (startOfRound != null && ___jetpackActivatedPreviousFrame)
            {
                int playerCount = startOfRound.gameStats.allPlayerStats.Length;
                //This part sets the array if it is not enough/not made
                if (activationJetpackPower.Length != playerCount)
                {
                    //Makes correctly sized array
                    activationJetpackPower = new float[playerCount];
                    mls.LogInfo("activationJetpackPower Made.");
                }
                //Gets the holding player and sets the inital charge 
                PlayerControllerB holdingPlayer = __instance.playerHeldBy;
                if (holdingPlayer != null)
                {
                    //Sets the array to the Jetpack charge 
                    activationJetpackPower[(int)(checked((IntPtr)holdingPlayer.playerClientId))] = __instance.insertedBattery.charge;
                    mls.LogInfo($"Set activationPower index:{(int)(checked((IntPtr)holdingPlayer.playerClientId))} to {__instance.insertedBattery.charge:0.##}.");
                }
            }
        }

        //Keeps track of a player getting jetpack controlls
        //Is called by synced RPC's
        
        [HarmonyPatch(typeof(JetpackItem), "DeactivateJetpack")]
        [HarmonyPostfix]
        public static void DeactivateJetpackPatch(JetpackItem __instance, ref PlayerControllerB ___previousPlayerHeldBy)
        {
            //Uses startOfRound to protect our array from bad playercount calls
            StartOfRound startOfRound = UnityEngine.Object.FindObjectOfType<StartOfRound>();
            if (startOfRound != null)
            {
                int playerCount = startOfRound.gameStats.allPlayerStats.Length;
                if (deltaJetpackUsage.Length != playerCount)
                {
                    //Makes correctly sized array
                    deltaJetpackUsage = new float[playerCount];
                    mls.LogInfo("deltaJetpackUsage Made.");
                }

                //Gets holding or ex/holding player
                PlayerControllerB holdingPlayer = __instance.playerHeldBy;
                if (holdingPlayer == null)
                    holdingPlayer = ___previousPlayerHeldBy;

                //Sets deltaJetpackUsage if the player has an innital value for charge
                if (holdingPlayer != null
                    && activationJetpackPower[(int)(checked((IntPtr)holdingPlayer.playerClientId))] != 0f)
                {
                    deltaJetpackUsage[(int)(checked((IntPtr)holdingPlayer.playerClientId))] =
                        activationJetpackPower[(int)(checked((IntPtr)holdingPlayer.playerClientId))] -
                         __instance.insertedBattery.charge;
                    mls.LogInfo($"Set player deltaCharge index:{(int)(checked((IntPtr)holdingPlayer.playerClientId))} to {deltaJetpackUsage[(int)(checked((IntPtr)holdingPlayer.playerClientId))]:0.##}.");
                }

            }
        }
        */
    }
}
