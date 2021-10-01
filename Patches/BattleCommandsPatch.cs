using HarmonyLib;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;


namespace FreelancerTemplate
{
    [HarmonyPatch(typeof(BehaviorComponent), "InformSergeantPlayer")]
    class BattleCommandsPatch
    {
        private static void Postfix(BehaviorComponent __instance)
        {
            if (Test.followingHero != null && Test.EnlistTier < 6 && (__instance.Formation.Team.IsPlayerTeam || __instance.Formation.Team.IsPlayerAlly) && (Test.AllBattleCommands || CommandForPlayerFormation(__instance.Formation.PrimaryClass)))
            {
                TextObject behaviorString = __instance.GetBehaviorString();
                if (behaviorString != null && __instance.GetType() != typeof(BehaviorGeneral) && __instance.GetType() != typeof(BehaviorProtectGeneral))
                {
                    InformationManager.AddQuickInformation(new TextObject((__instance.GetType() != typeof(BehaviorHorseArcherSkirmish) ? formatclass(__instance.Formation.PrimaryClass) + " " : "") + behaviorString.ToString().ToLower()), 4000, Test.followingHero.CharacterObject);
                    if(__instance.GetType() == typeof(BehaviorHorseArcherSkirmish) || __instance.GetType() == typeof(BehaviorAssaultWalls) || __instance.GetType() == typeof(BehaviorCharge) || __instance.GetType() == typeof(BehaviorSkirmish) || __instance.GetType() == typeof(BehaviorTacticalCharge))
                    {
                        SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
                    }
                    else
                    {
                        SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/move"));
                    }
                }
            }
        }

        private static bool CommandForPlayerFormation(FormationClass formation)
        {
            if (formation == FormationClass.Infantry && !Hero.MainHero.CharacterObject.IsArcher && !Hero.MainHero.CharacterObject.IsMounted)
            {
                return true;
            }
            if (formation == FormationClass.Ranged && Hero.MainHero.CharacterObject.IsArcher && !Hero.MainHero.CharacterObject.IsMounted)
            {
                return true;
            }
            if (formation == FormationClass.Cavalry && !Hero.MainHero.CharacterObject.IsArcher && Hero.MainHero.CharacterObject.IsMounted)
            {
                return true;
            }
            if (formation == FormationClass.HorseArcher && Hero.MainHero.CharacterObject.IsArcher && Hero.MainHero.CharacterObject.IsMounted)
            {
                return true;
            }

            return false;
        }

        private static string formatclass(FormationClass primaryClass)
        {
            switch (primaryClass)
            {
                case FormationClass.Infantry:
                    return new TextObject("{=FLT0000140}Infantry").ToString();
                case FormationClass.Ranged:
                    return new TextObject("{=FLT0000141}Archers").ToString();
                case FormationClass.Cavalry:
                    return new TextObject("{=FLT0000142}Cavalry").ToString();
                case FormationClass.HorseArcher:
                    return new TextObject("{=FLT0000143}Horse archers").ToString();
                case FormationClass.Skirmisher:
                    return new TextObject("{=FLT0000144}Skirmishers").ToString();
                case FormationClass.HeavyInfantry:
                    return new TextObject("{=FLT0000145}Heavy infantry").ToString();
                case FormationClass.LightCavalry:
                    return new TextObject("{=FLT0000146}Light cavalry").ToString();
                case FormationClass.HeavyCavalry:
                    return new TextObject("{=FLT0000147}Heavy calalry").ToString();
                default :
                    return "";
            }
        }
    }
}