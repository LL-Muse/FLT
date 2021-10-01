using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Localization;
using System.Reflection;

namespace FreelancerTemplate
{
    class AbonadonedOrphanEvent : CampaignBehaviorBase
    {
        private bool isFemale;

        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, new Action(Tick));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(MenuItems));
        }

        private void MenuItems(CampaignGameStarter campaignStarter)
        {
            TextObject textObject = new TextObject("{=FLT0000175}As you walk through the ravaged village, you notice one house in particular that is covered in blood.  As you walk closer the stench of death hits your nostrils.  It become clear to you what happened here.  The sturbon yet foolish family of this house decided to stay behind and defend what little they owned.  The looting soldiers of your army had little patience for those that resisteted and cut down the family where they stood");
            TextObject textObject2 = new TextObject("{=FLT0000176}Suddenly the sound of crys and screams catches your attention.  Someone is still inside.  As you go in to investigate, you quickly find source of the noise.  It is a child, barely older than an infant.  The soldiers that came by earlier either didn't notice or didn't have the heart to kill the child.  You realize that with all the villagers driven away, it might be days or even weeks before anyone return and you are not sure what will happen to the child by then");
            TextObject textObject3 = new TextObject("{=FLT0000177}Take the child with you");
            TextObject textObject4 = new TextObject("{=FLT0000178}Leave");
            campaignStarter.AddGameMenu("abonadoned_orphan", textObject.ToString(), (args) =>
            {
            }, GameOverlays.MenuOverlayType.None);

            campaignStarter.AddGameMenuOption("abonadoned_orphan", "abonadoned_orphan_continue", "Continue", (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                GameMenu.SwitchToMenu("abonadoned_orphan_2");
            }), true);

            campaignStarter.AddGameMenu("abonadoned_orphan_2", textObject2.ToString(), (args) => {
            }, GameOverlays.MenuOverlayType.None);

            campaignStarter.AddGameMenuOption("abonadoned_orphan_2", "abonadoned_orphan_a", textObject3.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.ShowMercy;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                generateChild();
                GameMenu.ActivateGameMenu("party_wait");
            }), true);

            campaignStarter.AddGameMenuOption("abonadoned_orphan_2", "abonadoned_orphan_b", textObject4.ToString(), (GameMenuOption.OnConditionDelegate)(args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }), (GameMenuOption.OnConsequenceDelegate)(args => {
                GameMenu.ActivateGameMenu("party_wait");
            }), true);
        }

        private void generateChild()
        {
            Settlement homeSettlement = Helpers.SettlementHelper.FindNearestVillage();
            CharacterObject template = isFemale ? homeSettlement.Culture.VillagerFemaleChild : homeSettlement.Culture.VillagerMaleChild;
            Hero child = HeroCreator.CreateSpecialHero(template, homeSettlement, null, Hero.MainHero.Clan, MBRandom.RandomInt(0, 3));
            int age1 = MBRandom.RandomInt(20,30);
            int age2 = age1 + MBRandom.RandomInt(0, 3);
            Hero mother = HeroCreator.CreateSpecialHero(homeSettlement.Culture.VillageWoman, age : age1);
            Hero father = HeroCreator.CreateSpecialHero(homeSettlement.Culture.Villager, age : age2);
            mother.Spouse = father;
            mother.ChangeState(Hero.CharacterStates.Dead);
            father.ChangeState(Hero.CharacterStates.Dead);
            child.ChangeState(Hero.CharacterStates.Active);
            mother.DeathDay = CampaignTime.Now;
            father.DeathDay = CampaignTime.Now;
            child.Mother = mother;
            child.Father = father;
            HeroCreationCampaignBehavior herocreationbehavior = new HeroCreationCampaignBehavior();
            herocreationbehavior.DeriveSkillsFromTraits(child, template);
            child.IsNoble = true;
            PropertyInfo property1 = typeof(CharacterObject).GetProperty("Occupation");
            if ((PropertyInfo)null != property1 && (Type)null != property1.DeclaringType)
            {
                PropertyInfo property2 = property1.DeclaringType.GetProperty("Occupation");
                if ((PropertyInfo)null != property2)
                {
                    property2.SetValue((object)child.CharacterObject, (object)Occupation.Lord, (object[])null);
                }
            }
            AdoptHeroAction.Apply(child);
            TextObject text = new TextObject("{=FLT0000179}{HERO} adopted a child named {CHILD}");
            text.SetTextVariable("HERO", Hero.MainHero.Name.ToString());
            text.SetTextVariable("CHILD", child.Name.ToString());
            InformationManager.AddQuickInformation(text, announcerCharacter: CharacterObject.PlayerCharacter);
        }

        private void Tick()
        {
            if (Test.followingHero != null && Test.followingHero.PartyBelongedTo != null && Test.followingHero.PartyBelongedTo.IsRaiding && Test.print(MBRandom.RandomInt(100)) == 1)
            {
                isFemale = MBRandom.RandomInt(100) < 50;
                GameMenu.SwitchToMenu("abonadoned_orphan");
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }
    }
}
