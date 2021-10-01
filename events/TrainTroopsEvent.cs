using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using SandBox;
using SandBox.Source.Missions;
using SandBox.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using Helpers;

namespace FreelancerTemplate
{
    public class TrainTroopsEvent : CampaignBehaviorBase
    {
        private Settlement EventSettlement;
        public static bool trainingDone = false;
        public static bool success = false;

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.TickEvent.AddNonSerializedListener((object)this, new Action<float>(Tick));
        }

        private void Tick(float tick)
        {
            if(Test.followingHero != null && trainingDone)
            {
                if (success)
                {
                    SubModule.ExecuteActionOnNextTick(delegate
                    {
                        Test.conversation_type = "training_success";
                        Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog2());
                        CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, null, false, false, false, false));
                    });

                }
                else
                {
                    SubModule.ExecuteActionOnNextTick(delegate
                    {
                        Test.conversation_type = "training_fail";
                        Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog3());
                        CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, null, false, false, false, false));
                    });
                }
                Test.OngoinEvent = false;
                trainingDone = false;
                success = false;
            }
        }


        private DialogFlow CreateDialog2()
        {
            TextObject textObject = new TextObject("{=FLT0000148}{HERO}, You have done well, the recruits can actual hold their own now. [rf:very_positive_hi, rb:very_positive]");
            TextObject textObject2 = new TextObject("{=FLT0000149}Just doing my job!");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return Test.conversation_type == "training_success"; ;
            }).Consequence(delegate
            {
                Test.conversation_type = null;
                ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, 10);
                Test.ChangeFactionRelation(Test.followingHero.MapFaction, 300);
                Test.ChangeLordRelation(Test.followingHero, 300);
            }).BeginPlayerOptions().PlayerOption(textObject2, null).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private DialogFlow CreateDialog3()
        {
            TextObject textObject = new TextObject("{=FLT0000150}{HERO}I expected better from you! [rf:very_positive_ag, rb:very_positive]");
            TextObject textObject2 = new TextObject("{=FLT0000151}I am sorry my lord");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return Test.conversation_type == "training_fail"; ;
            }).Consequence(delegate
            {
                Test.conversation_type = null;
                ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, -5);
            }).BeginPlayerOptions().PlayerLine(textObject2, null).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (Test.followingHero != null && hero == Test.followingHero && settlement.MapFaction == hero.MapFaction && settlement.IsTown && !Hero.MainHero.IsWounded && !Test.OngoinEvent && HasRecruits() && !settlement.MapFaction.IsAtWarWith(Test.followingHero.MapFaction.MapFaction) && Test.print(MBRandom.RandomInt(100)) < 10)
            {
                Test.OngoinEvent = true;
                EventSettlement = settlement;
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, EventSettlement);
                Test.conversation_type = "train_troops";
                Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, null, false, false, false, false));
            }
        }

        private bool HasRecruits()
        {
            int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(Test.followingHero.Culture.BasicTroop);
            if(index != -1)
            {
                return Test.followingHero.PartyBelongedTo.MemberRoster.GetElementNumber(index) >= 5;
            }
            return false;
        }

        private DialogFlow CreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000152}{HERO} I have a task for you!  I have a bunch of new recruits that won't be of much use in a battle.  I need you to train them and get them caught up to the rest of the army.  You can make use of the sparring equipment at the arena to train [rf:very_positive_ag, rb:very_positive]");
            TextObject textObject2 = new TextObject("{=FLT0000153}Of course my lord!");
            TextObject textObject3 = new TextObject("{=FLT0000154}I can't turn a bunch of peasants into soldiers overnight, only the trials of many battles can do that");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return Test.conversation_type == "train_troops"; ;
            }).Consequence(delegate
            {
                Test.conversation_type = null;
            }).BeginPlayerOptions().PlayerOption(textObject2, null).CloseDialog().Consequence(delegate {
                SubModule.ExecuteActionOnNextTick((Action)(() => TrainingMission()));
            }).PlayerOption(textObject3, null).Consequence(delegate
            {
                Test.OngoinEvent = false;
                ChangeRelationAction.ApplyPlayerRelation(Test.followingHero, -1);
            }).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private void TrainingMission()
        {
            MobileParty.MainParty.IsActive = true;
            Test.disable_XP = true;
            string scene = Test.followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena").GetSceneName(Test.followingHero.CurrentSettlement.Town.GetWallLevel());
            Location location = Test.followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena");
            MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, ""), (InitializeMissionBehvaioursDelegate)(mission => (IEnumerable<MissionBehaviour>)new MissionBehaviour[13]
             {
                (MissionBehaviour) new MissionOptionsComponent(),
                (MissionBehaviour) new CustomArenaTrainingMissionController(Test.followingHero.PartyBelongedTo.MemberRoster, false, false),
                (MissionBehaviour) new HeroSkillHandler(),
                (MissionBehaviour) new MissionFacialAnimationHandler(),
                (MissionBehaviour) new MissionDebugHandler(),
                (MissionBehaviour) new MissionAgentPanicHandler(),
                (MissionBehaviour) new HighlightsController(),
                (MissionBehaviour) new BattleHighlightsController(),
                (MissionBehaviour) new AgentBattleAILogic(),
                (MissionBehaviour) new ArenaAgentStateDeciderLogic(),
                (MissionBehaviour) new VisualTrackerMissionBehavior(),
                (MissionBehaviour) new CampaignMissionComponent(),
                (MissionBehaviour) new MissionAgentHandler(location)
             }));
            GameMenu.ActivateGameMenu("party_wait");
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
