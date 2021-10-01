using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FreelancerTemplate
{
    class IllegalPoachersEvents : CampaignBehaviorBase
    {
        private Settlement EventSettlement;
        private MobileParty PoacherParty;

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(MenuItems));
        }

        private void MenuItems(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddWaitGameMenu("illegal_poachers", "", new OnInitDelegate(wait_on_init), new OnConditionDelegate(wait_on_condition), null, new OnTickDelegate(wait_on_tick), type: GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.none, null);
        }

        private void wait_on_init(MenuCallbackArgs args)
        {
        }

        private void wait_on_tick(MenuCallbackArgs args, CampaignTime dt)
        {
        }

        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        private void OnGameMenuOpened(MenuCallbackArgs args)
        {
            if (Campaign.Current.GameMenuManager.NextLocation != null || !(GameStateManager.Current.ActiveState is MapState))
            {
                return;
            }
            if (args.MenuContext.GameMenu.StringId == "illegal_poachers" && PlayerEncounter.Battle != null)
            {
                if (PoacherParty != null)
                {
                    DestroyPartyAction.Apply(PartyBase.MainParty, PoacherParty);
                }
                Test.OngoinEvent = false;
                PlayerEncounter.Current.FinalizeBattle();
                PlayerEncounter.LeaveEncounter = true;
                GameMenu.ActivateGameMenu("party_wait");
            }
        }

        private void OnSettlementEntered(MobileParty mobile, Settlement settlement, Hero hero)
        {
            if (Test.followingHero != null && hero == Test.followingHero && settlement.MapFaction == hero.MapFaction && settlement.IsVillage && settlement.Village.VillageState == Village.VillageStates.Normal && !Hero.MainHero.IsWounded && !Test.OngoinEvent && Test.followingHero.PartyBelongedTo.Army == null && Test.print(MBRandom.RandomInt(100)) < 5)
            {
                Test.OngoinEvent = true;
                EventSettlement = settlement;
                Test.conversation_type = "illegal_poachers";
                Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, null, false, false, false, false));
            }
        }

        private DialogFlow CreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000168}Men!  Ready you weapons!  I warned these poacher scum never to come back here again! [rf:very_negative_hi, rb:very_negative]");
            TextObject textObject2 = new TextObject("{=FLT0000169}Time to teach those bastards a lesson!");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                return Test.conversation_type == "illegal_poachers";
            }).Consequence(delegate
            {
                Test.conversation_type = null;
            }).PlayerLine(textObject2, null).Consequence(delegate {
                SubModule.ExecuteActionOnNextTick((Action)(() => startbattle()));
            }).CloseDialog();
        }

        private void startbattle()
        {
            PoacherParty = CreatePoacherParty();
            MobileParty.MainParty.IsActive = true;
            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, PoacherParty.Party, false);
            PlayerEncounter.StartBattle();
            Test.NoRetreat = true;
            MapEvent _mapEvent = (MapEvent)typeof(PlayerEncounter).GetField("_mapEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(PlayerEncounter.Current);
            typeof(MapEventSide).GetMethod("AddNearbyPartyToPlayerMapEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(_mapEvent.DefenderSide, new object[] { Test.followingHero.PartyBelongedTo });
            CampaignMission.OpenBattleMission(EventSettlement.LocationComplex.GetLocationWithId("village_center").GetSceneName(1));
            GameMenu.ActivateGameMenu("illegal_poachers");
        }

        private MobileParty CreatePoacherParty()
        {
            MobileParty _poachersParty = MobileParty.CreateParty("poachers_party", null, null);
            TextObject customName = new TextObject("{=WQa1R55u}Poachers Party", null);
            _poachersParty.InitializeMobileParty(new TroopRoster(_poachersParty.Party), new TroopRoster(_poachersParty.Party), EventSettlement.GetPosition2D, 1f, 0.5f);
            _poachersParty.SetCustomName(customName);
            EnterSettlementAction.ApplyForParty(_poachersParty, EventSettlement);
            CharacterObject character = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "poacher");
            _poachersParty.MemberRoster.AddToCounts(character, Math.Min(Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount/2, 50));
            return _poachersParty;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
