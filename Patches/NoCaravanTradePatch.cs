using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(CaravansCampaignBehavior), "caravan_buy_products_on_condition")]
    class NoCaravanTradePatch
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