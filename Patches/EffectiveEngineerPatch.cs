﻿using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(MobileParty), "EffectiveEngineer", MethodType.Getter)]
    class EffectiveEngineerPatch
    {
        private static bool Prefix(MobileParty __instance, ref Hero __result)
        {
            if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.currentAssignment == Test.Assignment.Engineer && Test.followingHero.PartyBelongedTo == __instance)
            {
                __result = Hero.MainHero;
                return false;
            }
            return true;
        }
    }
}