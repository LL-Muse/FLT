using HarmonyLib;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.Core;

namespace FreelancerTemplate
{

    [HarmonyPatch(typeof(BasicMissionHandler), "CreateWarningWidget")]
    class NoRetreatPatch
    {
        private static bool Prefix()
        {
            if(Test.NoRetreat && Test.followingHero != null)
            {
                InformationManager.DisplayMessage(new InformationMessage("Can not retreat from this mission"));
                return false;
            }
            return true;
        }
    }

}
