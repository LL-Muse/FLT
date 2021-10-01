using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.Engine;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;

namespace FreelancerTemplate
{
    class BanditAmbushEvent : CampaignBehaviorBase
    {
        private MobileParty BanditParty;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, new Action(Tick));
            CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(MenuItems));
        }

        private void Tick()
        {
            if (Test.followingHero != null && Test.followingHero.PartyBelongedTo.MapEvent == null && Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount > 50 && !Hero.MainHero.IsWounded && !Test.OngoinEvent && Test.followingHero.PartyBelongedTo.Army == null && Test.followingHero.PartyBelongedTo.CurrentSettlement == null && MBRandom.RandomInt(1000) == 0)
            {
                Test.OngoinEvent = true;
                Test.conversation_type = "bandit_ambush";
                Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
                SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
                SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
                SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/ui/mission/horns/attack"));
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.PartyBelongedTo.MemberRoster.GetTroopRoster().GetRandomElement().Character, null, false, false, false, false));
            }
        }

        private void MenuItems(CampaignGameStarter campaignStarter)
        {
            campaignStarter.AddWaitGameMenu("bandit_ambush", "", new OnInitDelegate(wait_on_init), new OnConditionDelegate(wait_on_condition), null, new OnTickDelegate(wait_on_tick), type: GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.none, null);
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
            if (args.MenuContext.GameMenu.StringId == "bandit_ambush" && PlayerEncounter.Battle != null)
            {
                Test.OngoinEvent = false;
                PlayerEncounter.Current.FinalizeBattle();
                PlayerEncounter.LeaveEncounter = true;
                GameMenu.ActivateGameMenu("party_wait");
            }
        }

        private DialogFlow CreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000172}Sound the alarm!  We are under attack!  To arms men!  [rf:very_negative_hi, rb:very_negative]");
            TextObject textObject2 = new TextObject("{=FLT0000173}Where did they come from?");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                return Test.conversation_type == "bandit_ambush";
            }).Consequence(delegate
            {
                Test.conversation_type = null;
            }).PlayerLine(textObject2, null).Consequence(delegate {
                SubModule.ExecuteActionOnNextTick((Action)(() => startbattle()));
            }).CloseDialog();
        }

        private void startbattle()
        {
            BanditParty = CreateAmbushParty();
            MobileParty.MainParty.IsActive = true;
            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, BanditParty.Party, false);
            PlayerEncounter.StartBattle();
            Test.NoRetreat = true;
            MapEvent _mapEvent = (MapEvent)typeof(PlayerEncounter).GetField("_mapEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(PlayerEncounter.Current);
            typeof(MapEventSide).GetMethod("AddNearbyPartyToPlayerMapEvent", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(_mapEvent.DefenderSide, new object[] { Test.followingHero.PartyBelongedTo });
            CampaignMission.OpenBattleMission(PlayerEncounter.GetBattleSceneForMapPosition(MobileParty.MainParty.Position2D));
            GameMenu.ActivateGameMenu("bandit_ambush");
        }

        private MobileParty CreateAmbushParty()
        {
            Settlement settlement = SettlementHelper.FindNearestHideout(null, null);
            Clan clan = null;
            if (settlement != null)
            {
                CultureObject banditCulture = settlement.Culture;
                clan = Clan.BanditFactions.FirstOrDefault((Clan x) => x.Culture == banditCulture);
            }
            if (clan == null)
            {
                clan = Clan.All.GetRandomElementWithPredicate((Clan x) => x.IsBanditFaction);
            }
            MobileParty _ambushParty = BanditPartyComponent.CreateBanditParty("bandit_ambush_party", clan, settlement.Hideout, false); 
            TextObject customName = new TextObject("{=FLT0000174}Bandit Ambush Party", null);
            _ambushParty.InitializeMobileParty(new TroopRoster(_ambushParty.Party), new TroopRoster(_ambushParty.Party), Test.followingHero.PartyBelongedTo.Position2D, 1f, 0.5f);
            _ambushParty.SetCustomName(customName);
            CharacterObject character1 = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mounted_ransacker");
            CharacterObject character2 = CharacterObject.All.FirstOrDefault((CharacterObject t) => t.StringId == "mounted_pillager");
            _ambushParty.MemberRoster.AddToCounts(character1, 7 * Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount / 20);
            _ambushParty.MemberRoster.AddToCounts(character2, 7 * Test.followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount / 20);
            return _ambushParty;
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
