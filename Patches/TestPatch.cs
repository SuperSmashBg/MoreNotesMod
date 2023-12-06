using HarmonyLib;

namespace MoreNotesMod.Patches
{
    [HarmonyPatch]
    public class WirtePatch
    {
        [HarmonyPatch(typeof(StartOfRound), "WritePlayerNotes")]
        [HarmonyPostfix]
        public static void MyPatch(StartOfRound __instance)
        {
            for (int i = 0; i < __instance.gameStats.allPlayerStats.Length; i++)
            {
                if (__instance.gameStats.allPlayerStats[i].isActivePlayer && !__instance.allPlayerScripts[i].isPlayerDead)
                {
                    __instance.gameStats.allPlayerStats[i].playerNotes.Add($"Took {__instance.gameStats.allPlayerStats[i].stepsTaken} steps");
                } 
            } 
            
        }
        
    }
}
