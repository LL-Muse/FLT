using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(Clan), "Banner", MethodType.Getter)]
    class ReplaceBannerPatch
    {
        private static bool Prefix(ref Banner __result, ref Clan __instance)
        {
            if (Test.followingHero != null && __instance == Hero.MainHero.Clan)
            {
                __result = Test.followingHero.ClanBanner;
                return false;
            }
            return true;
        }
    }
}