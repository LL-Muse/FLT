using HarmonyLib;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(PartyNameplateVM), "RefreshBinding")]
    class HidePartyNamePlatePatch
    {
        private static void Postfix(PartyNameplateVM __instance)
        {
            if (__instance.Party == MobileParty.MainParty)
            {
                if (Test.followingHero != null)
                {
                    __instance.IsMainParty = false;
                    __instance.IsVisibleOnMap = false;
                }
                else
                {
                    __instance.IsMainParty = true;
                    __instance.IsVisibleOnMap = true;
                }
            }
        }
    }
}