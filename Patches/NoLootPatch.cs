using HarmonyLib;
using TaleWorlds.CampaignSystem;
using System.Reflection;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(PlayerEncounter), "DoLootParty")]
    class NoLootPatch
    {
        private static bool Prefix(PlayerEncounter __instance)
        {
            if (Test.followingHero != null)
            {
                typeof(PlayerEncounter).GetField("_mapEventState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).SetValue(__instance, PlayerEncounterState.End);
                return false;
            }
            return true;
        }
    }
}