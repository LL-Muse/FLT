using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SandBox;
using System.Reflection;
namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(AlleyFightSpawnHandler), "AfterStart")]
    class AlleyArmourPatch
    {
        private static bool Prefix(AlleyFightSpawnHandler __instance)
        {
            if (Test.followingHero != null)
            {
                MapEvent _mapEvent = (MapEvent)typeof(AlleyFightSpawnHandler).GetField("_mapEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(__instance);
                int num = MBMath.Floor((float)_mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Defender));
                int num2 = MBMath.Floor((float)_mapEvent.GetNumberOfInvolvedMen(BattleSideEnum.Attacker));
                int defenderInitialSpawn = MBMath.Floor((float)num);
                int attackerInitialSpawn = MBMath.Floor((float)num2);
                __instance.Mission.DoesMissionRequireCivilianEquipment = false;
                MissionAgentSpawnLogic _missionAgentSpawnLogic = (MissionAgentSpawnLogic)typeof(AlleyFightSpawnHandler).GetField("_missionAgentSpawnLogic", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(__instance);
                _missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, false);
                _missionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, false);
                _missionAgentSpawnLogic.InitWithSinglePhase(num, num2, defenderInitialSpawn, attackerInitialSpawn, true, true, 1f);
                return false;
            }
            return true;
        }
    }
}