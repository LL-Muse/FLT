using HarmonyLib;
using SandBox.TournamentMissions.Missions;
using TaleWorlds.CampaignSystem;
using System.Reflection;
namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(TournamentBehavior), "OnPlayerWinTournament")]
    class AddTournamentPrizePatch
    {
        private static void Postfix(TournamentBehavior __instance)
        {
            if (Test.followingHero != null)
            {
                TournamentGame game = (TournamentGame)typeof(TournamentBehavior).GetField("_tournamentGame", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(__instance);
                if(Test.tournamentPrizes == null)
                {
                    Test.tournamentPrizes = new ItemRoster();
                }
                Test.tournamentPrizes.AddToCounts(game.Prize, 1);
                Test.ChangeFactionRelation(Test.followingHero.MapFaction, 200);
                Test.xp += 200;
            }
        }
    }
}