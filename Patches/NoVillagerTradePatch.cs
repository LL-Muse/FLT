using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.VillageBehaviors;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(VillagerCampaignBehavior), "village_farmer_buy_products_on_condition")]
    class NoVillagerTradePatch
    {
        private static bool Prefix(ref bool __result)
        {
            if (Test.followingHero != null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}