using System;
using TaleWorlds.CampaignSystem;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using Helpers;


namespace FreelancerTemplate
{
    class ExtortionByDesertersEvent : CampaignBehaviorBase
    {
        private static Settlement EventSettlement;
        private static MobileParty deserterParty;
        private Hero notable;

        public override void RegisterEvents()
        {
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, new Action<PartyBase>(this.PartyRemoved));
        }

        private void PartyRemoved(PartyBase party)
        {
            if(Test.followingHero == null)
            {
                Test.OngoinEvent = false;
                if(deserterParty != null)
                {
                    DestroyPartyAction.Apply(PartyBase.MainParty, deserterParty);
                }
                deserterParty = null;
                return;
            }
            if(deserterParty != null && deserterParty.Party == party)
            {
                Test.OngoinEvent = false;
                Test.followingHero.PartyBelongedTo.Ai.SetDoNotMakeNewDecisions(false);
                Test.followingHero.PartyBelongedTo.IgnoreByOtherPartiesTill(CampaignTime.Now);
                Test.conversation_type = "extortion_by_deserters_end";
                notable = EventSettlement.Notables.GetRandomElement();
                Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog2());
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(notable.CharacterObject, null, false, false, false, false));
                deserterParty = null;
            }
            if (deserterParty != null && (Test.followingHero == null || Test.followingHero.PartyBelongedTo == null || Test.followingHero.PartyBelongedTo.Party == party))
            {
                Test.OngoinEvent = false;
                DestroyPartyAction.Apply(PartyBase.MainParty, deserterParty);
                deserterParty = null;
            }
        }

        private DialogFlow CreateDialog2()
        {
            TextObject textObject = new TextObject("{=FLT0000170}Thank you so much for protecting our village![rf:very_positive_hi, rb:very_positive]");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                return Test.conversation_type == "extortion_by_deserters_end";
            }).Consequence(delegate
            {
                Test.conversation_type = null;
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(notable, Test.followingHero, 20);
                ChangeRelationAction.ApplyPlayerRelation(notable, 5);
            }).CloseDialog();
        }

        private void OnSettlementEntered(MobileParty mobile, Settlement settlement, Hero hero)
        {
            if (Test.followingHero != null && hero == Test.followingHero && settlement.MapFaction == hero.MapFaction && settlement.IsVillage && settlement.Village.VillageState == Village.VillageStates.Normal && !Hero.MainHero.IsWounded && !Test.OngoinEvent && settlement.Notables.Count > 0 && Test.followingHero.PartyBelongedTo.Army == null && Test.print(MBRandom.RandomInt(100)) < 5)
            {
                Test.OngoinEvent = true;
                EventSettlement = settlement;
                Test.conversation_type = "extortion_by_deserters_start";
                Campaign.Current.ConversationManager.AddDialogFlow(CreateDialog());
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(Test.followingHero.CharacterObject, null, false, false, false, false));
            }
        }

        private DialogFlow CreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000171}Men!  We will be staying in this village for a while.  The locals have told me a band of army deserters have been extorting the villagers.  We will hide in the houses and ambush the brigands when they show up again");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                return Test.conversation_type == "extortion_by_deserters_start";
            }).Consequence(delegate
            {
                Test.conversation_type = null;
                Test.followingHero.PartyBelongedTo.SetMoveModeHold();
                Test.followingHero.PartyBelongedTo.Ai.SetDoNotMakeNewDecisions(true);
                deserterParty = CreateDeserterParty();
            }).CloseDialog();
        }

        private MobileParty CreateDeserterParty()
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

            MobileParty _deserterMobileParty = BanditPartyComponent.CreateBanditParty("deserters_party", clan, settlement.Hideout, false);
            TextObject customName = new TextObject("{=zT2b0v8y}Deserters Party", null);
            _deserterMobileParty.SetCustomName(customName);
            List<CharacterObject> trooplist = Test.GetTroopsList(EventSettlement.Culture);
            _deserterMobileParty.InitializeMobileParty(new TroopRoster(_deserterMobileParty.Party), new TroopRoster(_deserterMobileParty.Party), EventSettlement.Village.Bound.GatePosition, 1f);
            _deserterMobileParty.IsVisible = true;

            while (_deserterMobileParty.MemberRoster.TotalManCount < Test.followingHero.PartyBelongedTo.MemberRoster.TotalManCount/2)
            {
                _deserterMobileParty.MemberRoster.AddToCounts(trooplist.GetRandomElement(), 1);
            }
            _deserterMobileParty.ItemRoster.AddToCounts(DefaultItems.Grain, _deserterMobileParty.MemberRoster.Count);
            _deserterMobileParty.IgnoreByOtherPartiesTill(CampaignTime.Never);
            _deserterMobileParty.SetMoveEngageParty(Test.followingHero.PartyBelongedTo);
            _deserterMobileParty.Ai.SetDoNotMakeNewDecisions(true);
            return _deserterMobileParty;
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData<MobileParty>("_deserter_event_party", ref deserterParty);
            dataStore.SyncData<Settlement>("_deserter_event_settlement", ref EventSettlement);
        }
    }
}
