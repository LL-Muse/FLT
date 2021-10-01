using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using SandBox.TournamentMissions.Missions;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.CharacterDevelopment.Managers;
using TaleWorlds.ObjectSystem;
using TaleWorlds.Localization;

namespace FreelancerTemplate
{
    internal class CustomArenaBattleMissionController : MissionLogic
    {
        private Agent _playerAgent;
        private List<MatrixFrame> _initialSpawnFrames;
        private static Dictionary<Agent, CharacterObject> dictionary = new Dictionary<Agent, CharacterObject>();
        private TroopRoster _troops;
        private String _training_type;
        public CustomArenaBattleMissionController(TroopRoster memberRoster)
        {
            this._troops = TroopRoster.CreateDummyTroopRoster();
            foreach(TroopRosterElement troop in memberRoster.GetTroopRoster())
            {
                if(troop.Number - troop.WoundedNumber > 0)
                {
                    _troops.AddToCounts(troop.Character, troop.Number - troop.WoundedNumber);
                }
            }
        }

        public override void AfterStart()
        {
            this.DeactivateOtherTournamentSets();
            this.InitializeMissionTeams();
            GameTexts.SetVariable("leave_key", Game.Current.GameTextManager.GetHotKeyGameText("CombatHotKeyCategory", 4));
            this._initialSpawnFrames = this.Mission.Scene.FindEntitiesWithTag("sp_arena").Select<GameEntity, MatrixFrame>((Func<GameEntity, MatrixFrame>)(e => e.GetGlobalFrame())).ToList<MatrixFrame>();
            for (int index = 0; index < this._initialSpawnFrames.Count; ++index)
            {
                MatrixFrame initialSpawnFrame = this._initialSpawnFrames[index];
                initialSpawnFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
                this._initialSpawnFrames[index] = initialSpawnFrame;
            }
            TrainingType();
        }

        public void TrainingType()
        {
            List<InquiryElement> inquiryElements = new List<InquiryElement>();

            TextObject text1 = new TextObject("{=FLT0000180}Melee");
            TextObject text2 = new TextObject("{=FLT0000181}Ranged");
            TextObject text3 = new TextObject("{=FLT0000182}Cavalry");
            inquiryElements.Add(new InquiryElement((object)"m", text1.ToString(), null, true, null));
            inquiryElements.Add(new InquiryElement((object)"r", text2.ToString(), null, true, null));
            inquiryElements.Add(new InquiryElement((object)"c", text3.ToString(), null, true, null));

            TextObject text = new TextObject("{=FLT0000183}Select training type");
            TextObject affrimativetext = new TextObject("{=FLT0000077}Continue");
            InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(text.ToString(), "", inquiryElements, false, 1, affrimativetext.ToString(), (string)null, (Action<List<InquiryElement>>)(args =>
            {
                List<InquiryElement> source = args;
                if (source != null && !source.Any<InquiryElement>())
                {
                    return;
                }
                InformationManager.HideInquiry();
                this._training_type = (args.Select<InquiryElement, string>((Func<InquiryElement, string>)(element => element.Identifier as string))).First<string>();
                this._playerAgent = this.SpawnAgent(CharacterObject.PlayerCharacter, this._initialSpawnFrames.GetRandomElement<MatrixFrame>(), this.Mission.PlayerTeam);
                dictionary.Add(_playerAgent, Hero.MainHero.CharacterObject);
                spawnTroop();
            }), (Action<List<InquiryElement>>)null));
        }

        private void spawnTroop()
        {
            if (this._troops.TotalManCount > 0)
            {
                TroopRosterElement troop = this._troops.GetTroopRoster().GetRandomElement();

                Agent agent = SpawnAgent(troop.Character, this._initialSpawnFrames.GetRandomElement<MatrixFrame>(), this.Mission.Teams[1]);
                agent.Defensiveness = 1f;
                dictionary.Add(agent, troop.Character);

                this._troops.AddToCounts(troop.Character, -1);
            }
            else
            {
                InformationManager.AddQuickInformation(new TextObject("{=FLT0000128}The men need time to rest and recover.  There is no one left to fight"), announcerCharacter: Test.followingHero.CharacterObject);
            }
        }

        private void InitializeMissionTeams()
        {
            this.Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, banner: Hero.MainHero.ClanBanner);

            this.Mission.Teams.Add(BattleSideEnum.Attacker, uint.MaxValue, uint.MaxValue, banner: Hero.MainHero.ClanBanner);
            this.Mission.PlayerTeam = this.Mission.Teams.Defender;
            this.Mission.Teams[0].SetIsEnemyOf(this.Mission.Teams[1], true);
        }

        private void DeactivateOtherTournamentSets() => TournamentBehavior.DeleteTournamentSetsExcept(this.Mission.Scene.FindEntityWithTag("tournament_fight"));

        private Agent SpawnAgent(CharacterObject character, MatrixFrame spawnFrame, Team team)
        {
            AgentBuildData agentBuildData = new AgentBuildData(character);
            agentBuildData.BodyProperties(character.GetBodyPropertiesMax());
            Mission mission = base.Mission;
            AgentBuildData agentBuildData2 = agentBuildData.Team(team).InitialPosition(spawnFrame.origin);
            Vec2 vec = spawnFrame.rotation.f.AsVec2;
            vec = vec.Normalized();
            Agent agent = mission.SpawnAgent(agentBuildData2.InitialDirection(vec).NoHorses(false).Equipment(WithSparingWeapons(character.FirstBattleEquipment, character.IsHero)).TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))), false, 0);
            agent.FadeIn();
            if (character == CharacterObject.PlayerCharacter)
                agent.Controller = Agent.ControllerType.Player;
            if (agent.IsAIControlled)
                agent.SetWatchState(Agent.WatchState.Alarmed);

            agent.Health = character.HitPoints;
            agent.BaseHealthLimit = character.MaxHitPoints();
            agent.HealthLimit = character.MaxHitPoints();
            if (agent.MountAgent != null)
            {
                agent.MountAgent.Health = 1000000000;
            }

            return agent;
        }

        private Equipment WithSparingWeapons(Equipment equipment, bool all)
        {
            Equipment newEquipment = new Equipment();
            newEquipment.FillFrom(equipment);
            if (all)
            {
                if (this._training_type == "m")
                {
                    newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                    newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
                     newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                    newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                    newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                }
                else if (this._training_type == "r")
                {
                    newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
                    newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                    newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                    newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                    newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                }
                else if (this._training_type == "c")
                {
                    newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                    newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                    newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                    newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                    newEquipment[EquipmentIndex.Horse] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
                    newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
                }
            }
            else
            {
                if (this._training_type == "m")
                {
                    int rand = MBRandom.RandomInt(6);
                    switch (rand)
                    {
                        case 0:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        case 1:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        case 2:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        case 3:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        case 4:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("peasant_maul_t1_2"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        default:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                    }
                }
                else if (this._training_type == "r")
                {
                    int rand = MBRandom.RandomInt(3);
                    switch (rand)
                    {
                        case 0:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        case 1:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("crossbow_a"));
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("tournament_bolts"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                        default:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_longbow"));
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(null);
                            break;
                    }
                }
                else if (this._training_type == "c")
                {
                    int rand = MBRandom.RandomInt(4);
                    switch (rand)
                    {
                        case 0:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
                            newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
                            break;
                        case 1:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
                            newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
                            break;
                        case 2:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
                            newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
                            break;
                        default:
                            newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                            newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
                            newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                            newEquipment[EquipmentIndex.Horse] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("noble_horse"));
                            newEquipment[EquipmentIndex.HorseHarness] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_of_aeneas"));
                            break;
                    }
                }
            }
            
            return newEquipment;
        }

        public override void OnMissionTick(float dt)
        {
        }

        public override void OnScoreHit(
          Agent affectedAgent,
          Agent affectorAgent,
          WeaponComponentData attackerWeapon,
          bool isBlocked,
          float damage,
          float movementSpeedDamageModifier,
          float hitDistance,
          AgentAttackType attackType,
          float shotDifficulty,
          BoneBodyPartType victimHitBodyPart)
        {
            if (affectorAgent == null || affectorAgent.Character == null || affectedAgent.Character == null)
                return;

            CharacterObject HitCharacter;
            CharacterObject HitterCharacter;

            if (dictionary.TryGetValue(affectorAgent, out HitterCharacter))
            {
                int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(HitterCharacter);
                if (index != -1)
                {
                    Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 50);
                }
            }
            if (dictionary.TryGetValue(affectedAgent, out HitCharacter))
            {
                if (HitCharacter.IsHero)
                {
                    HitCharacter.HeroObject.HitPoints = (int)(affectedAgent.Health);
                }
            }

            if ((double)damage > (double)affectedAgent.HealthLimit)
                damage = affectedAgent.HealthLimit;
            float num = damage / affectedAgent.HealthLimit;
            this.EnemyHitReward(affectedAgent, affectorAgent, movementSpeedDamageModifier, shotDifficulty, attackerWeapon, 0.5f * num, damage);
        }

        private void EnemyHitReward(
              Agent affectedAgent,
              Agent affectorAgent,
              float lastSpeedBonus,
              float lastShotDifficulty,
              WeaponComponentData attackerWeapon,
              float hitpointRatio,
              float damageAmount)
        {
            CharacterObject HitterCharacter;
            if (dictionary.TryGetValue(affectorAgent, out HitterCharacter))
            {
                int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(HitterCharacter);
                if (index != -1)
                {
                    Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 50);
                }
            }

            CharacterObject character1 = (CharacterObject)affectedAgent.Character;
            CharacterObject character2 = (CharacterObject)affectorAgent.Character;
            if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null)
                return;
            SkillLevelingManager.OnCombatHit(character2, character1, (CharacterObject)null, (Hero)null, lastSpeedBonus, lastShotDifficulty, attackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, false, damageAmount, affectedAgent.Health < 1f);
        }

        public override void OnAgentRemoved(
          Agent affectedAgent,
          Agent affectorAgent,
          AgentState agentState,
          KillingBlow killingBlow)
        {
            CharacterObject killedCharacter;
            CharacterObject killerCharacter;
            if (dictionary.TryGetValue(affectedAgent, out killedCharacter))
            {
                if (killedCharacter.IsHero)
                {
                    if(killedCharacter == CharacterObject.PlayerCharacter)
                    {
                        InformationManager.AddQuickInformation(new TextObject("{=FLT0000129}I should stop before I seriously injury myself"), announcerCharacter: CharacterObject.PlayerCharacter);
                        return;
                    }
                    InformationManager.AddQuickInformation(getDeafeatedMessage(), announcerCharacter: killedCharacter);
                    int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killedCharacter);
                    Test.followingHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(index).HeroObject.HitPoints = 0;
                }
                else
                {
                    InformationManager.AddQuickInformation(getDeafeatedMessage(), announcerCharacter: killedCharacter);
                    Test.followingHero.PartyBelongedTo.MemberRoster.WoundTroop(killedCharacter, 1);
                }
            }
            if (dictionary.TryGetValue(affectorAgent, out killerCharacter))
            {
                int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killerCharacter);
                if (index != -1)
                {
                    Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 100);
                }
            }
            spawnTroop();
        }

        private TextObject getDeafeatedMessage()
        {
            List<TextObject> message = new List<TextObject>();
            message.Add(new TextObject("{=FLT0000130}You win, I give up!"));
            message.Add(new TextObject("{=FLT0000131}Ouch that really hurt, I'm done!"));
            message.Add(new TextObject("{=FLT0000132}You just got lucky, I will beat you next time!"));
            message.Add(new TextObject("{=FLT0000133}I am going to be sore in the morning!"));
            message.Add(new TextObject("{=FLT0000134}Well fought friend!"));
            message.Add(new TextObject("{=FLT0000135}I shouldn't have agreed to this!"));
            message.Add(new TextObject("{=FLT0000136}Ahhh! I think that broke a bone!"));
            message.Add(new TextObject("{=FLT0000137}I didn't expect training weapons to hurt this much!"));
            message.Add(new TextObject("{=FLT0000138}No fair, you cheated!"));
            message.Add(new TextObject("{=FLT0000139}I yield!"));
            SoundEvent.PlaySound2D(SoundEvent.GetEventIdFromString("event:/mission/ambient/detail/arena/cheer_big"));
            return message.GetRandomElement();
        }

        public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
        {
            canPlayerLeave = true;
            return (InquiryData)null;
        }
    }
}