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
using TaleWorlds.CampaignSystem.Actions;
using System.Threading;
namespace FreelancerTemplate
{
    internal class CustomArenaTrainingMissionController : MissionLogic
    {
        private bool _requireCivilianEquipment;
        private bool _spawnBothSideWithHorses;
        private List<MatrixFrame> _initialSpawnFrames;
        private static Dictionary<Agent, CharacterObject> dictionary = new Dictionary<Agent, CharacterObject>();
        private TroopRoster _troops;
        private BasicTimer _duelTimer;
        private int _xp = 0;
        private int _recruits_left = 5;
        private bool spawned = false;
        public CustomArenaTrainingMissionController(TroopRoster memberRoster, bool requireCivilianEquipment, bool spawnBothSideWithHorses)
        {
            this._requireCivilianEquipment = requireCivilianEquipment;
            this._spawnBothSideWithHorses = spawnBothSideWithHorses;
            this._troops = TroopRoster.CreateDummyTroopRoster();
            foreach (TroopRosterElement troop in memberRoster.GetTroopRoster())
            {
                if (!troop.Character.IsHero)
                {
                    _troops.AddToCounts(troop.Character, troop.Number );
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

            InformationManager.AddQuickInformation(new TextObject("{=FLT0000121}I'm here to teach you miserable maggots how to fight, so you better listen up and do everything I say!"), announcerCharacter: CharacterObject.PlayerCharacter);
            InformationManager.AddQuickInformation(new TextObject("{=FLT0000122}Controll the recruits and gain enough xp throught dueling to be able to upgrade them and complete the mission"), announcerCharacter: null);
            InformationManager.AddQuickInformation(new TextObject("{=FLT0000123}You will fail the mission if all recruit get knocked out before getting enough xp"), announcerCharacter: null);
            this._duelTimer = new BasicTimer(MBCommon.TimeType.Mission);
            
        }

        private void spawnTroop(bool PlayerControled)
        {
            if (PlayerControled && _recruits_left == 0)
            {
                _recruits_left--;
                return;
            }
            TroopRosterElement troop = this._troops.GetTroopRoster().GetRandomElement();
            Agent agent = SpawnAgent(PlayerControled ? Test.followingHero.Culture.BasicTroop : troop.Character, this._initialSpawnFrames.GetRandomElement<MatrixFrame>(), PlayerControled ? this.Mission.Teams[0] : this.Mission.Teams[1]);
            agent.Defensiveness = 1f;
            dictionary.Add(agent, troop.Character);
            if (PlayerControled)
            {
                TextObject text = new TextObject("{=FLT0000124}Recruits left");
                InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _recruits_left.ToString()));
                _recruits_left--;
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
            Agent agent = mission.SpawnAgent(agentBuildData2.InitialDirection(vec).NoHorses(!this._spawnBothSideWithHorses).Equipment(this._requireCivilianEquipment ? WithSparingWeapons(character.FirstCivilianEquipment, character.IsHero) : WithSparingWeapons(character.FirstBattleEquipment, character.IsHero)).TroopOrigin(new SimpleAgentOrigin(character, -1, null, default(UniqueTroopDescriptor))), false, 0);
            agent.FadeIn();
            if (team == this.Mission.Teams[0])
                agent.Controller = Agent.ControllerType.Player;
            if (agent.IsAIControlled)
                agent.SetWatchState(Agent.WatchState.Alarmed);

            agent.Health = character.HitPoints;
            agent.BaseHealthLimit = character.MaxHitPoints();
            agent.HealthLimit = character.MaxHitPoints();
            if (agent.MountAgent != null)
            {
                agent.MountAgent.Health = character.Equipment.Horse.GetModifiedMountHitPoints();
            }

            return agent;
        }

        private Equipment WithSparingWeapons(Equipment equipment, bool all)
        {
            Equipment newEquipment = new Equipment();
            newEquipment.FillFrom(equipment);
            if (all)
            {
                newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
                newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
            }
            else
            {
                int rand = MBRandom.RandomInt(8);
                switch (rand)
                {
                    case 0:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                        break;
                    case 1:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_2hsword_t1"));
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    case 2:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    case 3:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_bow"));
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    case 4:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("training_longbow"));
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("blunt_arrows"));
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    case 5:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("western_javelin_1_t2_blunt"));
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("wooden_sword_t1"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    case 6:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("peasant_maul_t1_2"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(null);
                        break;
                    default:
                        newEquipment[EquipmentIndex.Weapon0] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon1] = new EquipmentElement(null);
                        newEquipment[EquipmentIndex.Weapon2] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("practice_spear_t1"));
                        newEquipment[EquipmentIndex.Weapon3] = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("bound_horsemans_kite_shield"));
                        break;
                }
            }

            return newEquipment;
        }

        public override void OnMissionTick(float dt)
        {
            if(this._duelTimer.ElapsedTime > 13f && !spawned)
            {
                spawnTroop(true);
                spawnTroop(false);
                spawned = true;
            }
            if (TrainingDone())
            {
                InformationManager.AddQuickInformation(new TextObject("{=FLT0000125}Training Completed"), 0, null, "");
            }
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

            if(affectorAgent.Team == this.Mission.Teams[0])
            {
                _xp += 10;
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 20);
                TextObject text = new TextObject("{=FLT0000126}Training progress");
                InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _xp.ToString() + "/1500"));
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
                    int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killedCharacter);
                    Test.followingHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(index).HeroObject.HitPoints = 0;
                }
                else
                {
                    Test.followingHero.PartyBelongedTo.MemberRoster.WoundTroop(killedCharacter, 1);
                }
            }
            if (affectorAgent.Team == this.Mission.Teams[0])
            {
                Hero.MainHero.AddSkillXp(DefaultSkills.Leadership, 50);
                _xp += 40;
                TextObject text = new TextObject("{=FLT0000126}Training progress");
                InformationManager.DisplayMessage(new InformationMessage(text.ToString() + " : " + _xp.ToString() + "/1500"));
            }
            if (dictionary.TryGetValue(affectorAgent, out killerCharacter))
            {
                int index = Test.followingHero.PartyBelongedTo.MemberRoster.FindIndexOfTroop(killerCharacter);
                if (index != -1)
                {
                    Test.followingHero.PartyBelongedTo.MemberRoster.SetElementXp(index, Test.followingHero.PartyBelongedTo.MemberRoster.GetElementXp(index) + 100);
                }
            }
            if (!TrainingDone())
            {
                spawnTroop(affectedAgent.Team == this.Mission.Teams[0]);
            }   
        }

        private bool TrainingDone()
        {
            return _xp >= 1500;
        }

        public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
        {
            canPlayerLeave = _recruits_left < 0 || TrainingDone();
            if (!TrainingDone() || _recruits_left < 0)
            {
                InformationManager.AddQuickInformation(new TextObject("{=FLT0000127}Can not leave before training finishes!"), 0, null, "");
            }
            TrainTroopsEvent.trainingDone = true;
            TrainTroopsEvent.success = TrainingDone();
            return (InquiryData)null;
        }
    }
}