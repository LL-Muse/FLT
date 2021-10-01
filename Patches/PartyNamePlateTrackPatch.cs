using System.Reflection;
using SandBox.ViewModelCollection.Nameplate;
using HarmonyLib;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{

    [HarmonyPatch(typeof(SettlementNameplateVM), "RefreshBindValues")]
    class PartyNamePlateTrackPatch
    {
        private static void Postfix(SettlementNameplateVM __instance)
        {
            if (__instance.Settlement == Test.Tracked)
            {
                typeof(SettlementNameplateVM).GetMethod("Track", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
            }
            if (__instance.Settlement == Test.Untracked)
            {
                typeof(SettlementNameplateVM).GetMethod("Untrack", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
            }
        }
    }

}
