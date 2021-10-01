using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(SiegeAftermathCampaignBehavior), "menu_settlement_taken_continue_on_consequence")]
    class SiegeAftermathPatch
    {
        private static bool Prefix(MenuCallbackArgs args)
        {
            if (Test.followingHero != null)
            {
                GameMenu.ActivateGameMenu("party_wait");
                PlayerEncounter.LeaveSettlement();
                PlayerEncounter.Finish(true);
                Campaign.Current.SaveHandler.SignalAutoSave();
                return false;
            }
            return true;
        }
    }
}