using HarmonyLib;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(PlayerArmyWaitBehavior), "OnArmyDispersed")]
    class NoDisperseMessagePatch
    {
        private static bool Prefix(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
        {
            if(Test.followingHero != null)
            {
                return false;
            }
            return true;
        }
    }
}