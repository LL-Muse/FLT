using HarmonyLib;
using SandBox;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(TournamentFightMissionController), "GetTeamWeaponEquipmentList")]
    class TournamentWeaponsPatch
    {
        private static bool Prefix(int teamSize, ref List<Equipment> __result)
        {
            if (Test.followingHero != null)
            {
				List<Equipment> list = new List<Equipment>();
				CultureObject culture = Test.followingHero.CurrentSettlement.Culture;
				IReadOnlyList<CharacterObject> readOnlyList = (teamSize == 4) ? culture.TournamentTeamTemplatesForFourParticipant : ((teamSize == 2) ? culture.TournamentTeamTemplatesForTwoParticipant : culture.TournamentTeamTemplatesForOneParticipant);
				CharacterObject characterObject;
				if (readOnlyList.Count > 0)
				{
					characterObject = readOnlyList[MBRandom.RandomInt(readOnlyList.Count)];
				}
				else
				{
					characterObject = ((teamSize == 4) ? MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_four_participant_set_v1") : ((teamSize == 2) ? MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_two_participant_set_v1") : MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_one_participant_set_v1")));
				}
				foreach (Equipment sourceEquipment in characterObject.BattleEquipments)
				{
					Equipment equipment = new Equipment();
					equipment.FillFrom(sourceEquipment, true);
					list.Add(equipment);
				}
				__result = list;
				return false;
            }
            return true;
        }
    }
}