using System;
using TaleWorlds.CampaignSystem;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.SandBox;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using System.Reflection;
using System.Linq;
using SandBox;
using System.Collections.Generic;
using SandBox.Source.Missions;
using SandBox.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using Helpers;

namespace FreelancerTemplate
{
    class Test : CampaignBehaviorBase
    {
        public static Hero followingHero;
        public static CampaignTime enlistTime;
        public static bool disbandArmy;
        public static int EnlistTier;
        public static int xp;
        private static Dictionary<IFaction, int> FactionReputation = new Dictionary<IFaction, int>();
        private static Dictionary<IFaction, int> retirementXP = new Dictionary<IFaction, int>();
        private static Dictionary<Hero, int> LordReputation = new Dictionary<Hero, int>();
        private static List<IFaction> kingVassalOffered = new List<IFaction>();
        public static ItemRoster oldItems = new ItemRoster();
        public static ItemRoster oldGear = new ItemRoster();
        public static ItemRoster tournamentPrizes = new ItemRoster();
        public static bool AllBattleCommands = false;
        public static bool disable_XP = false;
        public static string conversation_type = "";
        public static Assignment currentAssignment;
        private Settlement selected_selement;
        public static Settlement Tracked;
        public static Settlement Untracked;
        public static bool OngoinEvent = false;
        public static bool NoRetreat = false;
        private MobileParty CavalryDetachment;

        private int[] NextlevelXP = new int[] {0, 600, 1700, 3400, 6000, 9400, 14000, 20000};

        public override void RegisterEvents()
        {
            if (SubModule.settings != null)
            {
                NextlevelXP[1] = SubModule.settings.Level1XP;
                NextlevelXP[2] = SubModule.settings.Level2XP;
                NextlevelXP[3] = SubModule.settings.Level3XP;
                NextlevelXP[4] = SubModule.settings.Level4XP;
                NextlevelXP[5] = SubModule.settings.Level5XP;
                NextlevelXP[6] = SubModule.settings.Level6XP;
                NextlevelXP[7] = SubModule.settings.Level7XP;
            }

            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(OnSessionLaunched));

            CampaignEvents.TickEvent.AddNonSerializedListener((object)this, new Action<float>(Tick));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, new Action(Tick2));
            CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, new Action(TickDaily));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, new Action<MobileParty, Settlement>(this.OnSettlementLeftEvent));
        }

        private void OnSettlementLeftEvent(MobileParty party, Settlement settlement)
        {
            if(party.LeaderHero != null && party.LeaderHero == followingHero)
            {
                GameMenu.ActivateGameMenu("party_wait");
            }
        }

        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if(followingHero != null && hero == followingHero)
            {
                GameMenu.ActivateGameMenu("party_wait");
                if (settlement.IsTown)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
            }
        }

        private void TickDaily()
        {
            if (followingHero != null)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, wage());
                ChangeFactionRelation(followingHero.MapFaction, 10);
                int XPAmount = SubModule.settings == null ? 10 : SubModule.settings.DailyXP;
                ChangeLordRelation(followingHero, XPAmount);
                xp += XPAmount;
                GetXPForRole();
            }
        }

        private void GetXPForRole()
        {
            switch (currentAssignment)
            {
                case Assignment.Grunt_Work:
                    Hero.MainHero.AddSkillXp(DefaultSkills.Athletics, 100);
                    return;
                case Assignment.Guard_Duty:
                    Hero.MainHero.AddSkillXp(DefaultSkills.Scouting, 100);
                    return;
                case Assignment.Foraging:
                    Hero.MainHero.AddSkillXp(DefaultSkills.Riding, 100);
                    if(followingHero != null && followingHero.PartyBelongedTo != null)
                    {
                        followingHero.PartyBelongedTo.ItemRoster.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>("grain"), MBRandom.RandomInt(5));
                    }
                    return;
                case Assignment.Cook:
                    Hero.MainHero.AddSkillXp(DefaultSkills.Steward, 100);
                    return;
                case Assignment.Sergeant:
                    Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 100);
                    if (followingHero != null && followingHero.PartyBelongedTo != null)
                    {
                        AddXPToRandomTroop();
                    } 
                    return;
                default :
                    return;
            }
        }

        private void AddXPToRandomTroop()
        {
            List<CharacterObject> list = new List<CharacterObject>();
            foreach(TroopRosterElement troop in followingHero.PartyBelongedTo.MemberRoster.GetTroopRoster())
            {
                if(!troop.Character.IsHero && (troop.Character.UpgradeTargets == null || troop.Character.UpgradeTargets.Length == 0))
                {
                    list.Add(troop.Character);
                }
            }
            if(list.Count > 0)
            {
                followingHero.PartyBelongedTo.MemberRoster.AddXpToTroop(500, list.GetRandomElement());
            }
        }

        private int wage()
        {
            return Math.Max(0, Math.Min(Hero.MainHero.Level * 2 + (xp/10), 1000));
        }

        private void Tick2()
        {
            if (followingHero != null)
            {
                if (followingHero.PartyBelongedTo.MapEvent == null)
                {
                    GameMenu.ActivateGameMenu("party_wait");
                }
                
                if (followingHero.CurrentSettlement != null)
                {
                    Hero.MainHero.Heal(PartyBase.MainParty, 3 * healAmount(), true);
                }
                else
                {
                    Hero.MainHero.Heal(PartyBase.MainParty, healAmount(), true);
                }
                
                bool leveledUp = false;
                while(EnlistTier < 7 && xp > NextlevelXP[EnlistTier])
                {
                    EnlistTier++;
                    leveledUp = true;
                }                
                
                if(kingVassalOffered == null)
                {
                    kingVassalOffered = new List<IFaction>();
                }

                if(retirementXP == null)
                {
                    retirementXP = new Dictionary<IFaction, int>();
                }
                if (!retirementXP.ContainsKey(followingHero.MapFaction))
                {
                    retirementXP.Add(followingHero.MapFaction, 10000);
                }

                int retirementXPNeeded;
                retirementXP.TryGetValue(followingHero.MapFaction, out retirementXPNeeded);
                if (leveledUp)
                {
                    conversation_type = "promotion";
                    TextObject text = new TextObject("{=FLT0000000}{HERO} has been promoted to tier {TIER}");
                    text.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
                    text.SetTextVariable("TIER", EnlistTier.ToString());
                    InformationManager.AddQuickInformation(text, announcerCharacter: Hero.MainHero.CharacterObject);
                    Campaign.Current.ConversationManager.AddDialogFlow(CreatePromotionDialog());
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
                }
                else if (!kingVassalOffered.Contains(followingHero.MapFaction) && GetFactionRelations(followingHero.MapFaction) >= 20000 && !leveledUp)
                {
                    if (followingHero.IsFactionLeader)
                    {
                        conversation_type = "vassalage2";
                        Campaign.Current.ConversationManager.AddDialogFlow(KingdomJoinCreateDialog2());
                        CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
                    }
                    else
                    {
                        conversation_type = "vassalage";
                        Campaign.Current.ConversationManager.AddDialogFlow(KingdomJoinCreateDialog());
                        CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
                    }
                    kingVassalOffered.Add(followingHero.MapFaction);
                }
                else if(xp > retirementXPNeeded)
                {
                    conversation_type = "retirement";
                    Campaign.Current.ConversationManager.AddDialogFlow(RetirementCreateDialog());
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
                }
            }
            else
            {
                Tracked = null;
                Untracked = null;
            }
        }

        private DialogFlow RetirementCreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000001}{HERO}, you have served long enough to fulfill your enlistment.  You can honorably retire and keep your gear, but I have need for talented soldiers.  I will offer you a bonus of 25000 {COIN} if you are willing to reenlist");
            TextObject textObject2 = new TextObject("{=FLT0000002}I will reenlist");
            TextObject textObject3 = new TextObject("{=FLT0000003}I will retire");
            textObject.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                if (followingHero != null)
                {
                    textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                }
                return Hero.OneToOneConversationHero == followingHero && conversation_type == "retirement"; ;
            }).Consequence(delegate
            {
                conversation_type = null;
            }).BeginPlayerOptions().PlayerOption(textObject2, null).Consequence(delegate {
                ChangeRelationAction.ApplyPlayerRelation(followingHero, 20);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 25000);
                retirementXP.Remove(followingHero.MapFaction);
                retirementXP.Add(followingHero.MapFaction, xp + 5000);
            }).CloseDialog().PlayerOption(textObject3, null).Consequence(delegate () {
                ChangeFactionRelation(followingHero.MapFaction, -100000);
                foreach (Clan clan in followingHero.Clan.Kingdom.Clans)
                {
                    if (!clan.IsUnderMercenaryService)
                    {
                        foreach (Hero lord in clan.Heroes)
                        {
                            if (lord.IsNoble)
                            {
                                ChangeLordRelation(lord, -100000);
                            }
                        }
                    }
                }
                ChangeRelationAction.ApplyPlayerRelation(followingHero, 20);
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                LeaveLordPartyAction(true);
            }).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private int healAmount()
        {
            return 1 + (Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) / 100) + (MBRandom.RandomInt(100) < (Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) % 100) ? 1 : 0);
        }
        

        private DialogFlow KingdomJoinCreateDialog2()
        {
            TextObject textObject = new TextObject("{=FLT0000004}{HERO}, you have shown yourself to be a warrior with no equal.  Hundreds of my enemies have died by your hands.  I need people like you in my kingdom.  I am willing to make you a vassal of my realm.  I will give you a generous bonus if you agree");
            TextObject textObject2 = new TextObject("{=FLT0000005}I will grant you the settlement of {FIEF} as your personal fief as a reward for your service");
            TextObject textObject3 = new TextObject("{=FLT0000006}My place is as a soldier on the battlefield");
            TextObject textObject4 = new TextObject("{=FLT0000007}Talk to me again if you change your mind");
            TextObject textObject5 = new TextObject("{=FLT0000008}It would be a honor to serve you my liege");
            TextObject textObject6 = new TextObject("{=FLT0000009}I will grant you a sum of 500000 {COIN} as a reward for your service");
            textObject6.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return conversation_type == "vassalage2"; ;
            }).Consequence(delegate
            {
                conversation_type = null;
            }).BeginPlayerOptions().PlayerOption(textObject3, null).NpcLine(textObject4).CloseDialog().PlayerOption(textObject5, null).Consequence(delegate () { 
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                LeaveLordPartyAction(true);
            }).BeginNpcOptions().NpcOption(textObject2, delegate {
                selected_selement = null;
                List<Settlement> list = new List<Settlement>();
                foreach(Settlement settlement in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
                {
                    if (settlement.IsTown)
                    {
                        list.Add(settlement);
                    }
                }
                if(list.Count < 1)
                {
                    foreach (Settlement settlement in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
                    {
                        if (settlement.IsCastle)
                        {
                            list.Add(settlement);
                        }
                    }
                }
                if(list.Count > 0)
                {
                    selected_selement = list.GetRandomElement();
                    textObject2.SetTextVariable("FIEF", selected_selement.EncyclopediaLinkWithName);
                }
                return selected_selement != null;
            }).Consequence(delegate{
                ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
                ChangeOwnerOfSettlementAction.ApplyByGift(selected_selement, Hero.MainHero);
                kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
            }).CloseDialog().NpcOption(textObject6, delegate { 
                return true; 
            }).Consequence(delegate {
                ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 500000);
                kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
            }).CloseDialog().EndNpcOptions().CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private DialogFlow KingdomJoinCreateDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000010}{HERO}, I recieved a message from {KING}, the leader of our kingdom.  {KING_GENDER_PRONOUN} would like to speak with you personally about offering you a lordship in our kingdom.  You have permission to leave my warband");
            TextObject textObject2 = new TextObject("{=FLT0000011}I rather stay here");
            TextObject textObject3 = new TextObject("{=FLT0000012}I will take my leave then");

            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                if(followingHero != null)
                {
                    textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("KING", followingHero.Clan.Kingdom.Leader.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("KING_GENDER_PRONOUN", followingHero.Clan.Kingdom.Leader.IsFemale ? "She" : "He");
                }
                return conversation_type == "vassalage"; ;
            }).Consequence(delegate
            {
                conversation_type = null;
            }).BeginPlayerOptions().PlayerOption(textObject2, null).CloseDialog().PlayerOption(textObject3, null).Consequence(delegate () {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                LeaveLordPartyAction(true); 
            }).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private DialogFlow CreatePromotionDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000013}{HERO}, you have proven yourself to be a fine warrior.  For your bravery and loyalty, I have decided to give you a promotion.  Visit my blade smith and armourer in the camp and they will provide you with the gear befitting your new rank");
            TextObject textObject2 = new TextObject("{=FLT0000014}It is a honor my lord");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return conversation_type == "promotion"; ;
            }).Consequence(delegate
            {
                conversation_type = null;
                if (followingHero != null)
                {
                    ChangeRelationAction.ApplyPlayerRelation(followingHero, 2 * EnlistTier);
                }
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }).BeginPlayerOptions().PlayerOption(textObject2, null).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private void Tick(float f)
        {
            if (Hero.MainHero.IsPrisoner && MobileParty.MainParty != null)
            {
                MobileParty.MainParty.IgnoreByOtherPartiesTill(CampaignTime.DaysFromNow(1f));
            }
            if (followingHero != null)
            {
                if (followingHero.PartyBelongedTo != null && !followingHero.IsPrisoner && !Hero.MainHero.IsPrisoner && followingHero.IsAlive)
                {   
                    if(CavalryDetachment != null && CavalryDetachment.MapEvent == null)
                    {
                        foreach (TroopRosterElement troop in CavalryDetachment.MemberRoster.GetTroopRoster())
                        {
                            followingHero.PartyBelongedTo.MemberRoster.AddToCounts(troop.Character, troop.Number);
                        }
                        DestroyPartyAction.Apply(followingHero.PartyBelongedTo.Party, CavalryDetachment);
                        CavalryDetachment = null;
                    }

                    if (disbandArmy && followingHero.PartyBelongedTo.Army != null && followingHero.PartyBelongedTo.MapEvent == null)
                    {
                        followingHero.PartyBelongedTo.Army.DisperseArmy();
                        disbandArmy = false;
                    }
                    if (MobileParty.MainParty.Army != null && followingHero.PartyBelongedTo.MapEvent == null)
                    {
                        MobileParty.MainParty.Army = (Army)null;
                    }
                    if (Campaign.Current.CurrentMenuContext == null)
                    {
                        GameMenu.ActivateGameMenu("party_wait");
                    }
                    if(MobileParty.MainParty.CurrentSettlement != null)
                    {
                        LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
                    }
                    UpdateDiplomacy();
                    MobileParty.MainParty.Position2D = followingHero.PartyBelongedTo.Position2D;
                    followingHero.PartyBelongedTo.Party.SetAsCameraFollowParty();
                    hidePlayerParty();
                    MobileParty.MainParty.IsActive = false;
                    disable_XP = false;
                    NoRetreat = false;
                    if (followingHero.PartyBelongedTo.MapEvent != null && MobileParty.MainParty.MapEvent == null)
                    {
                        while (Campaign.Current.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }
                        if (followingHero.PartyBelongedTo.Army == null)
                        {
                            followingHero.PartyBelongedTo.ActualClan.Kingdom.CreateArmy(followingHero.PartyBelongedTo.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
                            disbandArmy = true;
                        }
                        else if(followingHero.PartyBelongedTo.AttachedTo == null && followingHero.PartyBelongedTo.Army != null && followingHero.PartyBelongedTo != followingHero.PartyBelongedTo.Army.LeaderParty)
                        {
                            followingHero.PartyBelongedTo.Army = (Army)null;
                            followingHero.PartyBelongedTo.ActualClan.Kingdom.CreateArmy(followingHero.PartyBelongedTo.LeaderHero, Hero.MainHero.HomeSettlement, Army.ArmyTypes.Patrolling);
                            disbandArmy = true;
                        }
                        followingHero.PartyBelongedTo.Army.AddPartyToMergedParties(MobileParty.MainParty);
                        MobileParty.MainParty.Army = followingHero.PartyBelongedTo.Army;
                        MobileParty.MainParty.IsActive = true;
                        MobileParty.MainParty.SetMoveEngageParty(followingHero.PartyBelongedTo);
                        
                        if (followingHero != null &&  followingHero.PartyBelongedTo != null && followingHero.PartyBelongedTo.MapEvent != null && !followingHero.PartyBelongedTo.MapEvent.IsSiegeAssault)
                        {
                            typeof(MapEvent).GetMethod("CheckNearbyPartiesToJoinMainPlayerMapEventBattle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(followingHero.PartyBelongedTo.MapEvent, new object[] {});
                        }
                    }
                }
                else //If party gets destoryed
                {
                    LeaveLordPartyAction(false);
                }
            }
        }

        public static void LeaveLordPartyAction(bool keepgear)
        {
            MobileParty.MainParty.IsActive = true;
            UndoDiplomacy();
            showPlayerParty();
            followingHero = null;
            if (!keepgear)
            {
                GetOldGear();
                equipGear();
                GetTournamentPrizes();
            }
            if(PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(true);
            }
            TransferAllItems(oldItems, MobileParty.MainParty.ItemRoster);
        }

        private static void GetTournamentPrizes()
        {
            if (tournamentPrizes == null)
            {
                tournamentPrizes = new ItemRoster();
            }
            foreach(ItemRosterElement item in tournamentPrizes)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
            }
        }


        private void OnSessionLaunched(CampaignGameStarter campaignStarter)
        {
            if (kingVassalOffered == null)
            {
                kingVassalOffered = new List<IFaction>();
            }
            TextObject textObject1 = new TextObject("{=FLT0000015}Let me join your warband");
            campaignStarter.AddPlayerLine("join_legions_start", "lord_talk_speak_diplomacy_2", "join_legions_response", textObject1.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return CharacterObject.OneToOneConversationCharacter.HeroObject != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo != null && CharacterObject.OneToOneConversationCharacter.HeroObject.PartyBelongedTo.LeaderHero == CharacterObject.OneToOneConversationCharacter.HeroObject && followingHero == null && !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction &&  Hero.MainHero.Clan.Kingdom == null && CharacterObject.OneToOneConversationCharacter.HeroObject.CurrentSettlement == null && CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.Kingdom != null;
            }),(ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject2 = new TextObject("{=FLT0000016}There is no way I would let a wanted criminal like you join my ranks");
            campaignStarter.AddDialogLine("join_legions_response_a", "join_legions_response", "lord_pretalk", textObject2.ToString(), new ConversationSentence.OnConditionDelegate (()=>{
                return CharacterObject.OneToOneConversationCharacter.HeroObject.MapFaction.MainHeroCrimeRating > 30;
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject3 = new TextObject("{=FLT0000017}I don't want you in my warband. You and I don't get along");
            campaignStarter.AddDialogLine("join_legions_response_b", "join_legions_response", "lord_pretalk", textObject3.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return CharacterObject.OneToOneConversationCharacter.HeroObject.GetRelationWithPlayer() <= -10;
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject4 = new TextObject("{=FLT0000018}About that lordship you offered me");
            campaignStarter.AddPlayerLine("join_faction_start", "lord_talk_speak_diplomacy_2", "join_faction_response", textObject4.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return CharacterObject.OneToOneConversationCharacter.HeroObject != null && !CharacterObject.OneToOneConversationCharacter.HeroObject.Clan.IsMinorFaction && Hero.MainHero.Clan.Kingdom == null && CharacterObject.OneToOneConversationCharacter.HeroObject.IsFactionLeader && kingVassalOffered.Contains(CharacterObject.OneToOneConversationCharacter.HeroObject.MapFaction);
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject5 = new TextObject("{=FLT0000019}{HERO}, you have shown yourself to be a warrior with no equal.  Hundreds of my enemies have died by your hands.  I need people like you in my kingdom");
            textObject5.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
            campaignStarter.AddDialogLine("join_faction_lord_response", "join_faction_response", "join_faction_response_2", textObject5.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return true; ;
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject6 = new TextObject("{=FLT0000020}It would be a honor to serve you my liege");
            campaignStarter.AddPlayerLine("join_faction_player_response_a", "join_faction_response_2", "join_faction_response_3", textObject6.ToString() , new ConversationSentence.OnConditionDelegate(() => {
                return true;
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject7 = new TextObject("{=FLT0000021}I change my mind");
            campaignStarter.AddPlayerLine("join_faction_player_response_b", "join_faction_response_2", "lord_pretalk", textObject7.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return true;
            }), (ConversationSentence.OnConsequenceDelegate)null);

            TextObject textObject8 = new TextObject("{=FLT0000022}I will grant you the settlement of ");
            TextObject textObject9 = new TextObject("{=FLT0000023} as your personal fief as a reward for your service");
            campaignStarter.AddDialogLine("join_faction_lord_response_2a", "join_faction_response_3", "lord_pretalk", textObject8.ToString() + "{FIEF}" + textObject9.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                
                selected_selement = null;
                List<Settlement> list = new List<Settlement>();
                foreach (Settlement settlement in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
                {
                    if (settlement.IsTown)
                    {
                        list.Add(settlement);
                    }
                }
                if (list.Count < 1)
                {
                    foreach (Settlement settlement in Hero.OneToOneConversationHero.Clan.Kingdom.Settlements)
                    {
                        if (settlement.IsCastle)
                        {
                            list.Add(settlement);
                        }
                    }
                }
                if (list.Count > 0)
                {
                    selected_selement = list.GetRandomElement();
                    MBTextManager.SetTextVariable("FIEF", selected_selement.EncyclopediaLinkWithName, false);
                }
                return selected_selement != null;
            }), new ConversationSentence.OnConsequenceDelegate(() =>{
                ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
                ChangeOwnerOfSettlementAction.ApplyByGift(selected_selement, Hero.MainHero);
                kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
            }));

            TextObject textObject10 = new TextObject("{=FLT0000024}I will grant you a sum of 500000 {COIN} as a reward for your service");
            textObject10.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            campaignStarter.AddDialogLine("join_faction_lord_response_2b", "join_faction_response_3", "lord_pretalk", textObject10.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return true;
            }), new ConversationSentence.OnConsequenceDelegate(() => {
                ChangeKingdomAction.ApplyByJoinToKingdom(Hero.MainHero.Clan, Hero.OneToOneConversationHero.Clan.Kingdom);
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 500000);
                kingVassalOffered.Remove(Hero.OneToOneConversationHero.MapFaction);
            }));

            TextObject textObject11 = new TextObject("{=FLT0000025}Sure you may join");
            campaignStarter.AddDialogLine("join_legions_response_c", "join_legions_response", "lord_pretalk", textObject11.ToString(), new ConversationSentence.OnConditionDelegate(() => {
                return true;
            }), new ConversationSentence.OnConsequenceDelegate (()=> {
                followingHero = Hero.OneToOneConversationHero;
                enlistTime = CampaignTime.Now;
                EnlistTier = 1;
                xp = (GetFactionRelations(followingHero.MapFaction) / 2) + (GetLordRelations(followingHero) / 2);
                bool leveledUp = false;
                UpdateDiplomacy();
                hidePlayerParty();
                if (oldItems == null)
                {
                    oldItems = new ItemRoster();
                }
                else
                {
                    oldItems.Clear();
                }
                if (oldGear == null)
                {
                    oldGear = new ItemRoster();
                }
                else
                {
                    oldGear.Clear();
                }
                if (tournamentPrizes == null)
                {
                    tournamentPrizes = new ItemRoster();
                }
                else
                {
                    tournamentPrizes.Clear();
                }
                currentAssignment = Assignment.Grunt_Work;
                DisbandParty();
                disbandArmy = false;
                TransferAllItems(MobileParty.MainParty.ItemRoster, oldItems);
                SetOldGear();
                Hero.MainHero.CharacterObject.Equipment.FillFrom(followingHero.Culture.BasicTroop.Equipment);
                GetOldGear();
                while (EnlistTier < 7 && xp > NextlevelXP[EnlistTier])
                {
                    EnlistTier++;
                    leveledUp = true;
                }
                if (leveledUp)
                {
                    TextObject infotext = new TextObject("{=FLT0000026}{HERO} has enlisted at tier {TIER} due to high reputation with the {FACTION}");
                    infotext.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
                    infotext.SetTextVariable("TIER", EnlistTier.ToString());
                    infotext.SetTextVariable("FACTION", followingHero.MapFaction.Name.ToString());
                    InformationManager.AddQuickInformation(infotext, announcerCharacter: Hero.MainHero.CharacterObject);
                    getRandomEquipmentSet();
                }
            }));

            campaignStarter.AddWaitGameMenu("party_wait", "Party Leader: {PARTY_LEADER}\n{PARTY_TEXT}", new OnInitDelegate(wait_on_init), new OnConditionDelegate(wait_on_condition), null, new OnTickDelegate(wait_on_tick), GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.none, null);

            TextObject textObject12 = new TextObject("{=FLT0000027}Change Equipment");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_change_equipment", textObject12.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                SwitchGear();
            }), true);

            TextObject textObject13 = new TextObject("{=FLT0000028}Train with the troops");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_train", textObject13.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                if(followingHero.PartyBelongedTo.MemberRoster.TotalHealthyCount < 11)
                {
                    args.Tooltip = new TextObject("{=FLT0000029}Not enough men or uninjured men in lord's party");
                    args.IsEnabled = false;
                }
                return followingHero.CurrentSettlement != null && followingHero.CurrentSettlement.IsTown && !followingHero.CurrentSettlement.Town.HasTournament;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, followingHero.CurrentSettlement);
                MobileParty.MainParty.IsActive = true;
                disable_XP = true;
                string scene = followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena").GetSceneName(followingHero.CurrentSettlement.Town.GetWallLevel());
                Location location = followingHero.CurrentSettlement.LocationComplex.GetLocationWithId("arena");
                MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, ""), (InitializeMissionBehvaioursDelegate)(mission => (IEnumerable<MissionBehaviour>)new MissionBehaviour[13]
                 {
                (MissionBehaviour) new MissionOptionsComponent(),
                (MissionBehaviour) new CustomArenaBattleMissionController(followingHero.PartyBelongedTo.MemberRoster),
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
            }), true);

            TextObject textObject14 = new TextObject("{=FLT0000030}Battle Commands : All");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_battle_commands_on", textObject14.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
               args.Tooltip = new TextObject("{=FLT0000031}Commands for all formations will be shouted durring battle\nClick to toggle");
                args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                return AllBattleCommands;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                AllBattleCommands = false;
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject15 = new TextObject("{=FLT0000032}Battle Commands : Player Formation Only");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_battle_commands_on", textObject15.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.Tooltip = new TextObject("{=FLT0000033}Commands for only the player's formation will be shouted durring battle\nClick to toggle");
                args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
                return !AllBattleCommands;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                AllBattleCommands = true;
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject16 = new TextObject("{=FLT0000034}Participate in tournament");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_tournament", textObject16.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                return followingHero.CurrentSettlement != null && followingHero.CurrentSettlement.IsTown && followingHero.CurrentSettlement.Town.HasTournament;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                disable_XP = true;
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, followingHero.CurrentSettlement);
                MobileParty.MainParty.IsActive = true;
                 TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(followingHero.CurrentSettlement.Town);
                int upgradeLevel = followingHero.CurrentSettlement.IsTown ? followingHero.CurrentSettlement.GetComponent<Town>().GetWallLevel() : 1;
                string scene = followingHero.CurrentSettlement.LocationComplex.GetScene("arena", upgradeLevel);
                SandBoxMission.OpenTournamentFightMission(scene, tournamentGame, followingHero.CurrentSettlement, followingHero.CurrentSettlement.Culture, true);
                Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), followingHero.CurrentSettlement);
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject17 = new TextObject("{=FLT0000035}Show reputation with factions");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_reputation", textObject17.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                GameMenu.SwitchToMenu("faction_reputation");
            }), true);

            TextObject textObject18 = new TextObject("{=FLT0000036}Ask commander for leave");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_ask_leave", textObject18.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                if (kingVassalOffered.Contains(followingHero.MapFaction))
                {
                    LeaveLordPartyAction(false);
                    GameMenu.ExitToLast();
                }
                else
                {
                    conversation_type = "ask_to_leave";
                    Campaign.Current.ConversationManager.AddDialogFlow(CreateAskToLeaveDialog());
                    CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
                }

            }), true);

            TextObject textObject19 = new TextObject("{=FLT0000037}Ask for a different assignment");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_ask_assignment", textObject19.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                conversation_type = "ask_assignment";
                Campaign.Current.ConversationManager.AddDialogFlow(CreateAskAssignmentDialog());
                CampaignMapConversation.OpenConversation(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, false, false, false, false), new ConversationCharacterData(followingHero.CharacterObject, null, false, false, false, false));
            }), true);

            TextObject textObject20 = new TextObject("{=FLT0000038}Lure bandits into ambush");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_attack", textObject20.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
                args.Tooltip = new TextObject("{=FLT0000039}A small group of bandits is way to nimble to catch normally.  The only way to catch them is to trick them into attacking, although there is a chance things could go wrong");
                if (!Hero.MainHero.CharacterObject.IsMounted)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
                }
                if (Hero.MainHero.IsWounded)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
                }
                return nearbyBandit().Count > 0;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                if(nearbyBandit().Count == 0)
                {
                    TextObject banditLureFailText = new TextObject("{=FLT0000042}The bandits did not fall for your trap");
                    InformationManager.DisplayMessage(new InformationMessage(banditLureFailText.ToString()));
                    GameMenu.ActivateGameMenu("party_wait");
                    return;
                }
                MobileParty banditParty = nearbyBandit().GetRandomElement();
                banditParty.SetMoveEngageParty(followingHero.PartyBelongedTo);
                banditParty.Ai.SetDoNotMakeNewDecisions(true);
                showPlayerParty();
                MobileParty.MainParty.IsActive = true;
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                if (Hero.MainHero.GetSkillValue(DefaultSkills.Tactics) - (5 * MobileParty.MainParty.Position2D.Distance(banditParty.Position2D)) < MBRandom.RandomInt(100))
                {
                    Hero.MainHero.HitPoints = Math.Max(0, Hero.MainHero.HitPoints - ( 5 + MBRandom.RandomInt(15)));
                    InformationManager.AddQuickInformation(new TextObject("{=FLT0000043}You took some minor injuries while being chased by bandits"), announcerCharacter: Hero.MainHero.CharacterObject);
                }
                Hero.MainHero.AddSkillXp(DefaultSkills.Tactics, 200);
                
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject23 = new TextObject("{=FLT0000184}Attack enemy villagers");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_villager_attack", textObject23.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Raid;
                args.Tooltip = new TextObject("{=FLT0000185}Ride out with the cavalry to attack enemy villagers");
                if (!Hero.MainHero.CharacterObject.IsMounted)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
                }
                if (Hero.MainHero.IsWounded)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
                }
                return nearbyVillagers().Count > 0;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                if (nearbyVillagers().Count == 0)
                {
                    TextObject VillagersFailText = new TextObject("{=FLT0000186}The villagers managed to escape");
                    InformationManager.DisplayMessage(new InformationMessage(VillagersFailText.ToString()));
                    GameMenu.ActivateGameMenu("party_wait");
                    return;
                }
                MobileParty Villagers = nearbyVillagers().GetRandomElement();
                showPlayerParty();
                MobileParty.MainParty.IsActive = true;
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                MobileParty.MainParty.Position2D = Villagers.Position2D;
                CavOnly(Test.followingHero.PartyBelongedTo).Position2D = Villagers.Position2D;
                MobileParty.MainParty.SetMoveEngageParty(Villagers);
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject24 = new TextObject("{=FLT0000188}Attack enemy caravan");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_villager_attack", textObject24.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.ForceToGiveGoods;
                args.Tooltip = new TextObject("{=FLT0000189}Ride out with the cavalry to attack enemy caravan");
                if (!Hero.MainHero.CharacterObject.IsMounted)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000040}You need to be mounted to do this");
                }
                if (Hero.MainHero.IsWounded)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=FLT0000041}You are wounded");
                }
                return nearbyCaravan().Count > 0;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                if (nearbyCaravan().Count == 0)
                {
                    TextObject VillagersFailText = new TextObject("{=FLT0000190}The caravan managed to escape");
                    InformationManager.DisplayMessage(new InformationMessage(VillagersFailText.ToString()));
                    GameMenu.ActivateGameMenu("party_wait");
                    return;
                }
                MobileParty caravan = nearbyCaravan().GetRandomElement();
                showPlayerParty();
                MobileParty.MainParty.IsActive = true;
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
                MobileParty.MainParty.Position2D = caravan.Position2D;
                CavOnly(Test.followingHero.PartyBelongedTo).Position2D = caravan.Position2D;
                MobileParty.MainParty.SetMoveEngageParty(caravan);
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            TextObject textObject21 = new TextObject("{=FLT0000044}Abandon Party");
            campaignStarter.AddGameMenuOption("party_wait", "party_wait_leave", textObject21.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                TextObject text = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}");
                text.SetTextVariable("FACTION", followingHero.MapFaction.Name.ToString());
                args.Tooltip = text;
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                return xp < 10000;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                TextObject titleText = new TextObject("{=FLT0000044}Abandon Party");
                TextObject text = new TextObject("{=FLT0000046}Are you sure you want to abandon the party>  This will harm relations with the entire faction");
                TextObject affrimativeText = new TextObject("{=FLT0000047}Yes");
                TextObject negativeText = new TextObject("{=FLT0000048}No");
                InformationManager.ShowInquiry(new InquiryData(titleText.ToString() , text.ToString() , true, true, affrimativeText.ToString(), negativeText.ToString(), new Action(delegate{ 
                    ChangeFactionRelation(followingHero.MapFaction, -100000);
                    ChangeCrimeRatingAction.Apply(followingHero.MapFaction, 55);
                    foreach (Clan clan in followingHero.Clan.Kingdom.Clans)
                    {
                        if (!clan.IsUnderMercenaryService)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -20);
                            foreach(Hero lord in clan.Heroes)
                            {
                                if (lord.IsNoble)
                                {
                                    ChangeLordRelation(lord, -100000);
                                }
                            }
                        }
                    }
                    LeaveLordPartyAction(true);
                    GameMenu.ExitToLast();
                }), new Action(delegate {
                    GameMenu.ActivateGameMenu("party_wait");
                })));
            }), true);

            campaignStarter.AddGameMenu("faction_reputation", "{REPUTATION}", (args) => {
                TextObject text = args.MenuContext.GameMenu.GetText();
                string s = "";
                foreach(Kingdom kingdom in Campaign.Current.Kingdoms)
                {
                    s += kingdom.Name.ToString() + " : " + GetFactionRelations(kingdom) + "\n";
                }
                text.SetTextVariable("REPUTATION", s);
            }, GameOverlays.MenuOverlayType.None);

            TextObject textObject22 = new TextObject("{=FLT0000049}Back");
            campaignStarter.AddGameMenuOption("faction_reputation", "faction_reputation_back", textObject22.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                GameMenu.ActivateGameMenu("party_wait");
            }), true);
        }

        private MobileParty CavOnly(MobileParty partyBelongedTo)
        {
            CavalryDetachment = MobileParty.CreateParty("calavlry detachment", null, null);
            TextObject customName = new TextObject("{=FLT0000187}Cavalry Detachment", null);
            CavalryDetachment.InitializeMobileParty(new TroopRoster(CavalryDetachment.Party), new TroopRoster(CavalryDetachment.Party), partyBelongedTo.GetPosition2D, 1f, 0.5f);
            CavalryDetachment.SetCustomName(customName);
            CavalryDetachment.ActualClan = partyBelongedTo.ActualClan;
            CavalryDetachment.ShouldJoinPlayerBattles = true;
            foreach(TroopRosterElement troop in partyBelongedTo.MemberRoster.GetTroopRoster())
            {
                if(troop.Character.IsMounted && !troop.Character.IsHero)
                {
                    int num = troop.Number;
                    CharacterObject character = troop.Character;
                    partyBelongedTo.MemberRoster.AddToCounts(character, -1 * num);
                    CavalryDetachment.MemberRoster.AddToCounts(character, num);
                }
            }
            return CavalryDetachment;
        }

        private List<MobileParty> nearbyCaravan()
        {
            float radius = MobileParty.MainParty.SeeingRange;
            List<MobileParty> list = new List<MobileParty>();
            foreach (MobileParty party in Campaign.Current.MobileParties)
            {
                if (party.IsCaravan && party.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.CurrentSettlement == null)
                {
                    list.Add(party);
                }
            }
            return list;
        }

        private List<MobileParty> nearbyVillagers()
        {
            float radius = MobileParty.MainParty.SeeingRange;
            List<MobileParty> list = new List<MobileParty>();
            foreach (MobileParty party in Campaign.Current.MobileParties)
            {
                if (party.IsVillager && party.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.CurrentSettlement == null)
                {
                    list.Add(party);
                }
            }
            return list;
        }

        private List<MobileParty> nearbyBandit()
        {
            float radius = MobileParty.MainParty.SeeingRange;
            List<MobileParty> list = new List<MobileParty>();
            foreach(MobileParty party in Campaign.Current.MobileParties)
            {
                if((party.IsBandit || party.IsBanditBossParty) && party.Position2D.Distance(MobileParty.MainParty.Position2D) < radius && party.TargetParty != followingHero.PartyBelongedTo)
                {
                    list.Add(party);
                }
            }
            return list;
        }

        private DialogFlow CreateAskAssignmentDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000050}{HERO}, I heard you are not happy with your current assignment.  What to would you rather do instead?");
            TextObject textObject2 = new TextObject("{=FLT0000051}Would this bag of {GOLD}{COIN} gold change your mind?");
            textObject2.SetTextVariable("COIN", "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            TextObject textObject3 = new TextObject("{=FLT0000052}Okay, you can do that");
            TextObject textObject4 = new TextObject("{=FLT0000053}I want to do manual labor");
            TextObject textObject5 = new TextObject("{=FLT0000054}I want to do guard duty");
            TextObject textObject6 = new TextObject("{=FLT0000055}I want to prepare the meals");
            TextObject textObject7 = new TextObject("{=FLT0000056}I want to forage for supplies");
            TextObject textObject8 = new TextObject("{=FLT0000057}I want to drill the troops");
            TextObject textObject9 = new TextObject("{=FLT0000058}Sure thing friend");
            TextObject textObject10 = new TextObject("{=FLT0000059}Of course, you are the perfect person for the job");
            TextObject textObject11 = new TextObject("{=FLT0000060}Sorry, but I dont think you have the skills for the job");
            TextObject textObject12 = new TextObject("{=FLT0000061}okay thanks bye");
            TextObject textObject13 = new TextObject("{=FLT0000062}You made a very persuasive argument. Fine, you can be one of my sergeants");
            TextObject textObject14 = new TextObject("{=FLT0000063}I want to lead the scouting expeditions");
            TextObject textObject15 = new TextObject("{=FLT0000064}You made a very persuasive argument. Fine, you can lead my scouts");
            TextObject textObject16 = new TextObject("{=FLT0000065}I want manage logistics");
            TextObject textObject17 = new TextObject("{=FLT0000066}You made a very persuasive argument. Fine, you can be my quartmaster");
            TextObject textObject18 = new TextObject("{=FLT0000067}I want to build war machines");
            TextObject textObject19 = new TextObject("{=FLT0000068}You made a very persuasive argument. Fine, you can be an engineer");
            TextObject textObject20 = new TextObject("{=FLT0000069}I want to take care of the wounded");
            TextObject textObject21 = new TextObject("{=FLT0000070}You made a very persuasive argument. Fine, you can be a surgeon");
            TextObject textObject22 = new TextObject("{=FLT0000071}I want to discuss war strategies");
            TextObject textObject23 = new TextObject("{=FLT0000072}You made a very persuasive argument. Fine, you can be my strategist");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return Hero.OneToOneConversationHero == followingHero && conversation_type == "ask_assignment";
            }).Consequence(delegate { 
                conversation_type = ""; 
            }).BeginPlayerOptions().PlayerOption(textObject4, null).Condition(delegate {
                return currentAssignment != Assignment.Grunt_Work;
            }).Consequence(delegate { 
                currentAssignment = Assignment.Grunt_Work; 
            }).NpcLine(textObject3, null, null).CloseDialog().PlayerOption(textObject5, null).Condition(delegate {
                return currentAssignment != Assignment.Guard_Duty;
            }).Consequence(delegate {
                currentAssignment = Assignment.Guard_Duty;
            }).NpcLine(textObject3, null, null).CloseDialog().PlayerOption(textObject6, null).Condition(delegate {
                return currentAssignment != Assignment.Cook;
            }).Consequence(delegate {
                currentAssignment = Assignment.Cook;
            }).NpcLine(textObject3, null, null).CloseDialog().PlayerOption(textObject7, null).Condition(delegate {
                return currentAssignment != Assignment.Foraging;
            }).Consequence(delegate {
                currentAssignment = Assignment.Foraging;
            }).NpcLine(textObject3, null, null).CloseDialog().PlayerOption(textObject8, null).Condition(delegate {
                return currentAssignment != Assignment.Sergeant;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Sergeant;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Leadership) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Sergeant;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject13, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Sergeant;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().PlayerOption(textObject14, null).Condition(delegate {
                return currentAssignment != Assignment.Scout;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Scout;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Scouting) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Scout;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject15, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Scout;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().PlayerOption(textObject16, null).Condition(delegate {
                return currentAssignment != Assignment.Quartermaster;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Quartermaster;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Steward) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Quartermaster;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject17, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Quartermaster;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().PlayerOption(textObject18, null).Condition(delegate {
                return currentAssignment != Assignment.Engineer;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Engineer;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Engineering) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Engineer;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject19, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Engineer;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().PlayerOption(textObject20, null).Condition(delegate {
                return currentAssignment != Assignment.Surgeon;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Surgeon;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Medicine) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Surgeon;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject21, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Surgeon;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().PlayerOption(textObject22, null).Condition(delegate {
                return currentAssignment != Assignment.Strategist;
            }).BeginNpcOptions().NpcOption(textObject9, delegate {
                return followingHero.GetRelationWithPlayer() >= 50;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Strategist;
            }).CloseDialog().NpcOption(textObject10, delegate {
                return Hero.MainHero.GetSkillValue(DefaultSkills.Tactics) >= 100;
            }, null).Consequence(delegate {
                currentAssignment = Assignment.Strategist;
            }).CloseDialog().NpcOption(textObject11, null, null).BeginPlayerOptions().PlayerOption(textObject12).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                textObject2.SetTextVariable("GOLD", (5 * wage()));
                return true;
            }).NpcLine(textObject23, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 5 * wage());
                currentAssignment = Assignment.Strategist;
            }).CloseDialog().EndPlayerOptions().EndNpcOptions().EndPlayerOptions().CloseDialog();
        }

        private DialogFlow CreateAskToLeaveDialog()
        {
            TextObject textObject = new TextObject("{=FLT0000073}{HERO}, your enlistment contact has not expired yet.  I can not have my soldiers leaving whenever they feel like");
            TextObject textObject2 = new TextObject("{=FLT0000051}Would this bag of {GOLD} {COIN} gold change your mind?");
            textObject2.SetTextVariable("COIN","<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            TextObject textObject3 = new TextObject("{=FLT0000074}You are right my lord my lord.  I will return to my duties");
            TextObject textObject4 = new TextObject("{=FLT0000075}I do need the money... Fine you may leave");
            return DialogFlow.CreateDialogFlow("start", 125).NpcLine(textObject, null, null).Condition(delegate
            {
                textObject.SetTextVariable("HERO", Hero.MainHero.EncyclopediaLinkWithName);
                return Hero.OneToOneConversationHero == followingHero && conversation_type == "ask_to_leave";
            }).Consequence(delegate { conversation_type = ""; }).BeginPlayerOptions().PlayerOption(textObject3, null).CloseDialog().PlayerOption(textObject2, null).Condition(delegate {
                 textObject2.SetTextVariable("GOLD", (10 * wage()));
                return true;
            }).NpcLine(textObject4, null, null).Consequence(delegate {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, followingHero, 10 * wage());
                LeaveLordPartyAction(false);
                GameMenu.ExitToLast();
            }).CloseDialog().EndPlayerOptions().CloseDialog();
        }

        private void DisbandParty()
        {
            if(MobileParty.MainParty.MemberRoster.TotalManCount > 1)
            {
                MobileParty party = MobileParty.CreateParty("main_disband_party");
                List<TroopRosterElement> list = new List<TroopRosterElement>();
                foreach(TroopRosterElement troop in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                {
                    if(troop.Character != Hero.MainHero.CharacterObject)
                    {
                        list.Add(troop);
                    }
                }
                foreach(TroopRosterElement troop in list)
                {
                    MobileParty.MainParty.MemberRoster.AddToCounts(troop.Character, -1 * troop.Number);
                    party.MemberRoster.AddToCounts(troop.Character, troop.Number);
                }
                party.InitializeMobileParty(new TroopRoster(party.Party), TroopRoster.CreateDummyTroopRoster(), MobileParty.MainParty.Position2D, 1f);
                party.IsVisible = true;
                DisbandPartyAction.ApplyDisband(party);
            }
        }

        private static void equipGear()
        {
            int weaponslot = 0;
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Head, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Horse, new EquipmentElement(null));
            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, new EquipmentElement(null));
            foreach (ItemRosterElement item in MobileParty.MainParty.ItemRoster)
            {
                
                if(item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.BodyArmor)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Body, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HeadArmor)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Head, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Cape)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Cape, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HandArmor)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Gloves, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.LegArmor)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Leg, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.Horse)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Horse, item.EquipmentElement);
                }
                else if (item.EquipmentElement.Item.Type == ItemObject.ItemTypeEnum.HorseHarness)
                {
                    Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, item.EquipmentElement);
                }else if (isWeapon(item.EquipmentElement.Item))
                {
                    for(int i = 0; i < item.Amount; i++)
                    {
                        if (weaponslot == 0)
                        {
                            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, item.EquipmentElement);
                        }
                        else if (weaponslot == 1)
                        {
                            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon1, item.EquipmentElement);
                        }
                        else if (weaponslot == 2)
                        {
                            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon2, item.EquipmentElement);
                        }
                        else if (weaponslot == 3)
                        {
                            Hero.MainHero.BattleEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon3, item.EquipmentElement);
                        }
                        weaponslot++;
                    }

                }
            }
            MobileParty.MainParty.ItemRoster.Clear();
        }

        public static bool isWeapon(ItemObject item)
        {
            return item.ItemType == ItemObject.ItemTypeEnum.Arrows || item.ItemType == ItemObject.ItemTypeEnum.Bolts || item.ItemType == ItemObject.ItemTypeEnum.Bow || item.ItemType == ItemObject.ItemTypeEnum.Bullets || item.ItemType == ItemObject.ItemTypeEnum.Crossbow || item.ItemType == ItemObject.ItemTypeEnum.Musket || item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon || item.ItemType == ItemObject.ItemTypeEnum.Pistol || item.ItemType == ItemObject.ItemTypeEnum.Polearm || item.ItemType == ItemObject.ItemTypeEnum.Shield || item.ItemType == ItemObject.ItemTypeEnum.Thrown || item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon;
        }

        private void SetOldGear()
        {
            EquipmentIndex[] slots = new EquipmentIndex[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3, EquipmentIndex.Head, EquipmentIndex.Cape, EquipmentIndex.Body,
            EquipmentIndex.Gloves, EquipmentIndex.Leg, EquipmentIndex.Horse, EquipmentIndex.HorseHarness};
            foreach (EquipmentIndex slot in slots)
            {
                if (Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item != null && Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item.Name != null)
                {
                    oldGear.AddToCounts(Hero.MainHero.CharacterObject.Equipment.GetEquipmentFromSlot(slot).Item, 1);
                }
            }
        }

        private static void GetOldGear()
        {
            MobileParty.MainParty.ItemRoster.Clear();
            List<ItemRosterElement> list = new List<ItemRosterElement>();
            foreach (ItemRosterElement item in oldGear)
            {
                MobileParty.MainParty.ItemRoster.AddToCounts(item.EquipmentElement, item.Amount);
            }
        }

        public static void TransferAllItems(ItemRoster items1, ItemRoster items2)
        {
            List<ItemRosterElement> move = new List<ItemRosterElement>();
            foreach(ItemRosterElement item in items1)
            {
                move.Add(item);
            }
            foreach (ItemRosterElement item in move)
            {
                items1.AddToCounts(item.EquipmentElement.Item, item.Amount * -1);
                items2.AddToCounts(item.EquipmentElement.Item, item.Amount);
            }
        }

        public static void SwitchGear()
        {
            List<InquiryElement> inquiryElements = new List<InquiryElement>();

            foreach (CharacterObject troop in GetTroopsList(followingHero.Culture))
            {
                if(troop.Tier <= EnlistTier)
                {
                    inquiryElements.Add(new InquiryElement((object)troop.Equipment, troop.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(troop)), true, EquipmentHint(troop.Equipment)));
                }
                
            }
            TextObject text = new TextObject("{=FLT0000076}Select equipment to use");
            TextObject affrimativetext = new TextObject("{=FLT0000077}Continue");
            InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(text.ToString(), "", inquiryElements, false, 1, affrimativetext.ToString(), (string)null, (Action<List<InquiryElement>>)(args =>
            {
                List<InquiryElement> source = args;
                if (source != null && !source.Any<InquiryElement>())
                {
                    return;
                }
                InformationManager.HideInquiry();
                Equipment equipment = (args.Select<InquiryElement, Equipment>((Func<InquiryElement, Equipment>)(element => element.Identifier as Equipment))).First<Equipment>();
                Hero.MainHero.CharacterObject.Equipment.FillFrom(equipment);
                GetOldGear();
                GetTournamentPrizes();
                GameMenu.ActivateGameMenu("party_wait");
            }), (Action<List<InquiryElement>>)null));
        }

        private void getRandomEquipmentSet()
        {
            List<Equipment> list = new List<Equipment>();
            int maxTier = -1;
            foreach (CharacterObject troop in GetTroopsList(followingHero.Culture))
            {
                if (troop.Tier <= EnlistTier && troop.Tier > maxTier)
                {
                    maxTier = troop.Tier;
                }
            }
            if (maxTier == -1)
            {
                return;
            }
            foreach (CharacterObject troop in GetTroopsList(followingHero.Culture))
            {
                if (troop.Tier == maxTier)
                {
                    list.Add(troop.Equipment);
                }
            }
            Hero.MainHero.CharacterObject.Equipment.FillFrom(list.GetRandomElement());
            GetOldGear();
            GetTournamentPrizes();
        }

        public static string EquipmentHint(Equipment equipment)
        {
            string s = "";
            EquipmentIndex[] slots = new EquipmentIndex[] { EquipmentIndex.Weapon0, EquipmentIndex.Weapon1, EquipmentIndex.Weapon2, EquipmentIndex.Weapon3, EquipmentIndex.Head, EquipmentIndex.Cape, EquipmentIndex.Body,
            EquipmentIndex.Gloves, EquipmentIndex.Leg, EquipmentIndex.Horse, EquipmentIndex.HorseHarness};
            foreach(EquipmentIndex slot in slots)
            {
                if (equipment.GetEquipmentFromSlot(slot).Item != null && equipment.GetEquipmentFromSlot(slot).Item.Name != null)
                {
                    s += equipment.GetEquipmentFromSlot(slot).Item.Name.ToString() + "\n";
                }
            }
            return s;
        }

        public static List<CharacterObject> GetTroopsList(CultureObject culture)
        {
            List<CharacterObject> MainLineUnits = new List<CharacterObject>();
            Stack<CharacterObject> stack = new Stack<CharacterObject>();
            stack.Push(culture.BasicTroop);
            MainLineUnits.Add(culture.BasicTroop);
            stack.Push(culture.EliteBasicTroop);
            MainLineUnits.Add(culture.EliteBasicTroop);
            foreach(Recruit recruit in SubModule.AdditonalTroops)
            {
                CharacterObject character = recruit.getCharacter();
                if (character!= null && character.Culture == culture && !MainLineUnits.Contains(character))
                {
                    stack.Push(character);
                    MainLineUnits.Add(character);
                }
            }

            while (!stack.IsEmpty())
            {
                CharacterObject popped = stack.Pop();

                if (popped.UpgradeTargets != null && popped.UpgradeTargets.Length > 0)
                {
                    for (int i = 0; i < popped.UpgradeTargets.Length; i++)
                    {
                        if (!MainLineUnits.Contains(popped.UpgradeTargets[i]))
                        {
                            MainLineUnits.Add(popped.UpgradeTargets[i]);
                            stack.Push(popped.UpgradeTargets[i]);
                        }
                    }
                }
            }
            return MainLineUnits;
        }

        public static void ChangeFactionRelation(IFaction faction, int amount)
        {
            if(FactionReputation == null)
            {
                FactionReputation = new Dictionary<IFaction, int>();
            }
            int value;
            if (FactionReputation.TryGetValue(faction, out value))
            {
                value += amount;
                FactionReputation.Remove(faction);
                FactionReputation.Add(faction, Math.Max(0, value));
            }
            else
            {
                FactionReputation.Add(faction, amount);
            }
            if(followingHero != null && amount > 0)
            {
                if(MBRandom.RandomInt(1000) <= amount)
                {
                    ChangeRelationAction.ApplyPlayerRelation(followingHero, 1);
                }
            }
        }

        public static void ChangeLordRelation(Hero hero, int amount)
        {
            if (LordReputation == null)
            {
                LordReputation = new Dictionary<Hero, int>();
            }
            int value;
            if (LordReputation.TryGetValue(hero, out value))
            {
                value += amount;
                LordReputation.Remove(hero);
                LordReputation.Add(hero, Math.Max(0, value));
            }
            else
            {
                LordReputation.Add(hero, Math.Max(0, amount));
            }
        }

        public static int GetFactionRelations(IFaction faction)
        {
            if (FactionReputation == null)
            {
                FactionReputation = new Dictionary<IFaction, int>();
                return 0;
            }
            int value;
            if (FactionReputation.TryGetValue(faction, out value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        public static int GetLordRelations(Hero hero)
        {
            if (LordReputation == null)
            {
                LordReputation = new Dictionary<Hero, int>();
                return 0;
            }
            int value;
            if (LordReputation.TryGetValue(hero, out value))
            {
                return value;
            }
            else
            {
                return 0;
            }
        }

        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            updatePartyMenu(args);
        }

        public static TextObject GetMobilePartyBehaviorText(MobileParty party)
        {
            if(Tracked != party.TargetSettlement)
            {
                Untracked = Tracked;
            }
            Tracked = party.TargetSettlement;
            TextObject textObject;
            if (party.DefaultBehavior == AiBehavior.Hold)
            {
                if (party.AtCampMode)
                {
                    textObject = new TextObject("{=FLT0000078}Camping");
                }
                else
                {
                    textObject = new TextObject("{=FLT0000079}Holding");
                }
                
            }
            else if (party.ShortTermBehavior == AiBehavior.EngageParty && party.ShortTermTargetParty != null)
            {
                textObject = new TextObject("{=FLT0000080}Engaging {TARGET_PARTY}");
                textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
                if(party.ShortTermTargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
            }
            else if (party.DefaultBehavior == AiBehavior.GoAroundParty && party.ShortTermBehavior == AiBehavior.GoToPoint)
            {
                textObject = new TextObject("{=FLT0000081}Chasing {TARGET_PARTY}", null);
                textObject.SetTextVariable("TARGET_PARTY", party.TargetParty.Name);
                if (party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null)
                if (party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
            }
            else if (party.ShortTermBehavior == AiBehavior.FleeToPoint && party.ShortTermTargetParty != null)
            {
                textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}", null);
                textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
                if (party.ShortTermTargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
            }
            else if (party.ShortTermBehavior == AiBehavior.FleeToGate && party.ShortTermTargetParty != null)
            {
                textObject = new TextObject("{=FLT0000083}Running from {TARGET_PARTY} to settlement", null);
                textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
                if (party.ShortTermTargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
            }
            else if (party.DefaultBehavior == AiBehavior.DefendSettlement)
            {
                textObject = new TextObject("{=FLT0000084}Defending {TARGET_SETTLEMENT}", null);
                textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
            }
            else if (party.DefaultBehavior == AiBehavior.RaidSettlement)
            {
                textObject = new TextObject("{=FLT0000085}Raiding {TARGET_SETTLEMENT}", null);
                textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
            }
            else if (party.DefaultBehavior == AiBehavior.BesiegeSettlement)
            {
                textObject = new TextObject("{=FLT0000086}Besieging {TARGET_SETTLEMENT}", null);
                textObject.SetTextVariable("TARGET_SETTLEMENT", party.TargetSettlement.EncyclopediaLinkWithName);
            }
            else if (party.ShortTermBehavior == AiBehavior.GoToPoint)
            {
                if (party.ShortTermTargetParty != null)
                {
                    textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}", null);
                    textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
                    if (party.ShortTermTargetParty.LeaderHero != null)
                    {
                        textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                    }
                }
                else if (party.TargetSettlement != null)
                {
                    textObject = ((party.DefaultBehavior == AiBehavior.PatrolAroundPoint) ? new TextObject("{=FLT0000087}Patrolling around {TARGET_SETTLEMENT}", null) : new TextObject("{=FLT0000089}Travelling.", null));
                    textObject.SetTextVariable("TARGET_SETTLEMENT", (party.TargetSettlement != null) ? party.TargetSettlement.EncyclopediaLinkWithName : party.HomeSettlement.EncyclopediaLinkWithName);
                }
                else if (party.DefaultBehavior == AiBehavior.PatrolAroundPoint)
                {
                    textObject = new TextObject("{=FLT0000088}Patrolling", null);
                }
                else
                {
                    textObject = new TextObject("{=FLT0000090}Going to a point", null);
                }
            }
            else if (party.ShortTermBehavior == AiBehavior.GoToSettlement)
            {
                if (party.ShortTermBehavior == AiBehavior.GoToSettlement && party.ShortTermTargetSettlement != null && party.ShortTermTargetSettlement != party.TargetSettlement)
                {
                    textObject = new TextObject("{=FLT0000091}Running to {TARGET_PARTY}", null);
                    textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetSettlement.EncyclopediaLinkWithName);
                }
                else if (party.DefaultBehavior == AiBehavior.GoToSettlement && party.TargetSettlement != null)
                {
                    textObject = new TextObject("{=FLT0000092}Travelling to {TARGET_PARTY}", null);
                    textObject.SetTextVariable("TARGET_PARTY", party.TargetSettlement.EncyclopediaLinkWithName);
                }
                else if (party.ShortTermTargetParty != null)
                {
                    textObject = new TextObject("{=FLT0000082}Running from {TARGET_PARTY}", null);
                    textObject.SetTextVariable("TARGET_PARTY", party.ShortTermTargetParty.Name);
                    if (party.ShortTermTargetParty.LeaderHero != null)
                    {
                        textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                    }
                }
                else
                {
                    textObject = new TextObject("{=FLT0000093}Traveling to a settlement", null);
                }
            }
            else if (party.ShortTermBehavior == AiBehavior.AssaultSettlement)
            {
                textObject = new TextObject("{=FLT0000094}Attacking {TARGET_SETTLEMENT}", null);
                textObject.SetTextVariable("TARGET_SETTLEMENT", party.ShortTermTargetSettlement.EncyclopediaLinkWithName);
            }
            else if (party.DefaultBehavior == AiBehavior.EscortParty)
            {
                textObject = new TextObject("{=FLT0000095}Following {TARGET_PARTY}", null);
                textObject.SetTextVariable("TARGET_PARTY", (party.ShortTermTargetParty != null) ? party.ShortTermTargetParty.Name : party.TargetParty.Name);
                if (party.ShortTermTargetParty != null && party.ShortTermTargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.ShortTermTargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
                else if (party.TargetParty != null && party.TargetParty.LeaderHero != null)
                {
                    textObject = HyperlinkTexts.GetHeroHyperlinkText(party.TargetParty.LeaderHero.EncyclopediaLink, textObject);
                }
            }
            else
            {
                textObject = new TextObject("{=FLT0000096}Unknown Behavior", null);
            }
            return textObject;
        }

        private string getFormation()
        {
            if (Hero.MainHero.CharacterObject.IsArcher && Hero.MainHero.CharacterObject.IsMounted)
            {
                return "{=FLT0000097}Horse Archer";
            }
            else if (Hero.MainHero.CharacterObject.IsMounted)
            {
                return "{=FLT0000098}Cavalry";
            }
            else if (Hero.MainHero.CharacterObject.IsArcher)
            {
                return "{=FLT0000099}Ranged";
            }
            else
            {
                return "{=FLT0000100}Infantry";
            }
        }

        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        private void wait_on_init(MenuCallbackArgs args)
        {
            updatePartyMenu(args);
        }

        private void updatePartyMenu(MenuCallbackArgs args)
        {
            if (followingHero == null || followingHero.PartyBelongedTo == null)
            {
                return;
            }
            TextObject text = args.MenuContext.GameMenu.GetText();
            string s = "";
            if (followingHero.PartyBelongedTo.Army == null || followingHero.PartyBelongedTo.AttachedTo == null)
            {
                TextObject text1 = new TextObject("{=FLT0000101}Party Objective");
                s += text1.ToString() + " : " + GetMobilePartyBehaviorText(followingHero.PartyBelongedTo) + "\n";
            }
            else
            {
                TextObject text2 = new TextObject("{=FLT0000102}Army Objective");
                s += text2.ToString() + " : " + GetMobilePartyBehaviorText(followingHero.PartyBelongedTo.Army.LeaderParty) + "\n";
            }
            TextObject text3 = new TextObject("{=FLT0000103}Enlistment Time");
            TextObject text4 = new TextObject("{=FLT0000104}Enlistment Tier");
            TextObject text5 = new TextObject("{=FLT0000105}Formation");
            TextObject text6 = new TextObject("{=FLT0000106}Wage");
            TextObject text7 = new TextObject("{=FLT0000107}Current Experience");
            TextObject text8 = new TextObject("{=FLT0000108}Next Level Experience");
            TextObject text9 = new TextObject("{=FLT0000109}When not fighting");
            s += text3.ToString() + " : " + enlistTime.ToString() + "\n";
            s += text4.ToString() + " : " + EnlistTier.ToString() + "\n";
            s += text5.ToString() + " : " + getFormation() + "\n";
            s += text6.ToString() + " : " + wage().ToString() + "<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">\n";
            s += text7.ToString() + " : " + xp.ToString() + "\n";
            if (EnlistTier < 7)
            {
                s += text8.ToString() + " : " + NextlevelXP[EnlistTier].ToString() + "\n";
            }
            s += text9.ToString() + " " + getAssignmentDescription(currentAssignment) + "\n";
            text.SetTextVariable("PARTY_LEADER", followingHero.EncyclopediaLinkWithName);
            text.SetTextVariable("PARTY_TEXT", s);
        }

        private static void hidePlayerParty()
        {
            if (((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals != null)
            {
                ((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);
            }
            if (((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals != null)
            {
                ((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(false);
            }
        }

        private static void showPlayerParty()
        {
            if (((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals != null)
            {
                ((PartyVisual)PartyBase.MainParty.Visuals).HumanAgentVisuals.GetEntity().SetVisibilityExcludeParents(true);
            }
            if (((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals != null)
            {
                ((PartyVisual)PartyBase.MainParty.Visuals).MountAgentVisuals.GetEntity().SetVisibilityExcludeParents(true);
            }
        }

        private static void UpdateDiplomacy()
        {
            foreach (IFaction faction in Campaign.Current.Factions)
            {
                if (faction != null && faction.IsAtWarWith(followingHero.MapFaction))
                {
                    FactionManager.DeclareWar(Hero.MainHero.MapFaction, faction);
                }
                else if(faction != null)
                {
                    FactionManager.SetNeutral(Hero.MainHero.MapFaction, faction);
                }
            }
        }

        private static void UndoDiplomacy()
        {
            if(MobileParty.MainParty.CurrentSettlement != null)
            {
                LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }
            foreach (IFaction faction in Campaign.Current.Factions)
            {
                if (faction != null && faction.IsAtWarWith(followingHero.MapFaction))
                {
                    FactionManager.SetNeutral(Hero.MainHero.MapFaction, faction);
                }
            }
        }

        public enum Assignment {
            None = 0, 
            Grunt_Work = 1, 
            Guard_Duty = 2, 
            Cook = 3, 
            Foraging = 4, 
            Surgeon = 5, 
            Engineer = 6, 
            Quartermaster = 7, 
            Scout = 8, 
            Sergeant = 9, 
            Strategist = 10 
        };

        public string getAssignmentDescription(Assignment assignment)
        {
            switch (assignment)
            {
                case Assignment.Grunt_Work:
                    return new TextObject("{=FLT0000110}you are currently assigned to perform grunt work.  Most tasks are unpleasant, tiring or involve menial labor (Passive Daily Athletics XP)").ToString();
                case Assignment.Guard_Duty:
                    return new TextObject("{=FLT0000111}you are currently assigned to guard duty.  You send many sleepless night keeping watch for signs intruders (Passive Daily Scouting XP)").ToString();
                case Assignment.Cook:
                    return new TextObject("{=FLT0000112}you are currently assigned as one of the cooks.  You prepare the camps meals with whatever limited ingredients avalible (Passive Daily Steward XP)").ToString();
                case Assignment.Foraging:
                    return new TextObject("{=FLT0000113}you are currently assigned to forage.  You ride through the nearby countryside looking for food (Passive Daily Riding XP and Daily Food To Party)").ToString();
                case Assignment.Surgeon:
                    return new TextObject("{=FLT0000114}you are currently assigned as the surgeon. You spend you time taking care of the wounded men (Medicine XP from party)").ToString();
                case Assignment.Engineer:
                    return new TextObject("{=FLT0000115}you are currently assigned as the engineer.  The party relies on your knowelege of siegecraft to build war machines (Engineering XP from party)").ToString();
                case Assignment.Quartermaster:
                    return new TextObject("{=FLT0000116}you are currently assigned to quartermaster. You make sure that the party is well supplied and the troops get paid on time (Steward XP from party)").ToString();
                case Assignment.Scout:
                    return new TextObject("{=FLT0000117}you are currently assigned to lead the scouting parties.  You and your men spend their time looking for signs of enemy parties and easy passages through difficult terrain  (Scouting XP from party)").ToString();
                case Assignment.Sergeant:
                    return new TextObject("{=FLT0000118}you are currently assigned as one of the sergeants.  You drill the men for war and discipline(beat) anyone who steps out of line (Passive Daily Leadership XP and Daily XP To Troops In Party)").ToString();
                case Assignment.Strategist:
                    return new TextObject("{=FLT0000119}you are currently assigned as the strategist.  You spend your time in the commader's tent discussing war plans (Tactics XP from party)").ToString();
                default:
                    return new TextObject("{=FLT0000120}you have no current assigned duties.  You spend your idle time drinking, gambling, and chatting with the idle soilders").ToString();
            }
        }

        public static int print(int value)
        {
            //InformationManager.DisplayMessage(new InformationMessage(value.ToString()));
            return value;
        }
        public override void SyncData(IDataStore dataStore)
        {
            MobileParty.MainParty.IsActive = true;
            dataStore.SyncData<Hero>("_following_hero", ref followingHero);
            dataStore.SyncData<Assignment>("_assigned_role", ref currentAssignment);
            dataStore.SyncData<List<IFaction>>("_vassal_offers", ref kingVassalOffered);
            dataStore.SyncData<int>("_enlist_tier", ref EnlistTier);
            dataStore.SyncData<int>("_enlist_xp", ref xp);
            dataStore.SyncData<Dictionary<IFaction, int>>("_faction_reputation", ref FactionReputation);
            dataStore.SyncData<Dictionary<IFaction, int>>("_retirement_xp", ref retirementXP);
            dataStore.SyncData<Dictionary<Hero, int>>("_lord_reputation", ref LordReputation);
            dataStore.SyncData<CampaignTime>("_enlist_date", ref enlistTime);
            dataStore.SyncData<ItemRoster>("_old_inventory", ref oldItems);
            dataStore.SyncData<ItemRoster>("_old_gear", ref oldGear);
            dataStore.SyncData<bool>("_ongoing_event", ref OngoinEvent);
            dataStore.SyncData<ItemRoster>("_tournament_prizes", ref tournamentPrizes);
        }
    }
}
