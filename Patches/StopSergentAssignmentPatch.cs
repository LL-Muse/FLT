using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(MapEvent), "IsPlayerSergeant")]
    class SergentAssignmentPatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (Test.followingHero != null && Test.EnlistTier < 6)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}