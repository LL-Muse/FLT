using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.Source.Objects.SettlementObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using SandBox.Source.Missions;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(VisualTrackerMissionBehavior), "RefreshCommonAreas")]
    class CommonAreaPatch
    {
        private static bool Prefix(VisualTrackerMissionBehavior __instance)
        {
            if (Test.followingHero != null)
            {
                Settlement settlement = Test.followingHero.CurrentSettlement;
                foreach (CommonAreaMarker commonAreaMarker in __instance.Mission.ActiveMissionObjects.FindAllWithType<CommonAreaMarker>().ToList<CommonAreaMarker>())
                {
                    if (settlement.CommonAreas.Count >= commonAreaMarker.AreaIndex && Campaign.Current.VisualTrackerManager.CheckTracked(settlement.CommonAreas[commonAreaMarker.AreaIndex - 1]))
                    {
                        __instance.RegisterLocalOnlyObject(commonAreaMarker);
                    }
                }
                return false;
            }
            return true;
        }
    }
}