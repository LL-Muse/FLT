using HarmonyLib;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors.Towns;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.SandBox;
using System.Reflection;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(HeroAgentSpawnCampaignBehavior), "AddWandererLocationCharacter")]
    class TournamentWanderPatch
    {
        private static bool Prefix(Hero wanderer, Settlement settlement)
        {
            if (Test.followingHero != null)
            {
                string actionSetCode = (settlement.Culture.StringId.ToLower() == "aserai" || settlement.Culture.StringId.ToLower() == "khuzait") ? (wanderer.IsFemale ? "as_human_female_warrior_in_aserai_tavern" : "as_human_warrior_in_aserai_tavern") : (wanderer.IsFemale ? "as_human_female_warrior_in_tavern" : "as_human_warrior_in_tavern");
                LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new PartyAgentOrigin(null, wanderer.CharacterObject, -1, default(UniqueTroopDescriptor), false)).Monster(Campaign.Current.HumanMonsterSettlement).NoHorses(true), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, actionSetCode, true, false, null, false, false, true);
                if (settlement.IsTown)
                {
                    settlement.LocationComplex.GetLocationWithId("tavern").AddCharacter(locationCharacter);
                }
                return false;
            }
            return true;
        }
    }
}