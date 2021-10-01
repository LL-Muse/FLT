using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;

namespace FreelancerTemplate
{
    internal class FreelancerMission : MissionBehaviour
    {
        public override MissionBehaviourType BehaviourType => MissionBehaviourType.Other;

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon,bool isBlocked, float damage, float movementSpeedDamageModifier, float hitDistance, AgentAttackType attackType, float shotDifficulty, BoneBodyPartType victimHitBodyPart)
        {
            if (Test.followingHero != null && !Test.disable_XP && affectorAgent.IsPlayerControlled && affectedAgent.Character != null && isenemey(affectedAgent, affectorAgent))
            {
                if (affectedAgent.Character.IsHero)
                {
                    Test.xp += 25;
                    Test.ChangeFactionRelation(Test.followingHero.MapFaction, 25);
                    Test.ChangeLordRelation(Test.followingHero, 25);
                }
                else
                {
                    int xpgain = ((affectedAgent.Character.Level / 5) + 4);
                    Test.xp += xpgain;
                    Test.ChangeFactionRelation(Test.followingHero.MapFaction, xpgain);
                    Test.ChangeLordRelation(Test.followingHero, xpgain);
                }
            }
        }

        private bool isenemey(Agent affectedAgent, Agent affectorAgent)
        {
            if(affectedAgent.Team == null || affectorAgent.Team == null)
            {
                return false;
            }
            return affectedAgent.Team.IsEnemyOf(affectorAgent.Team);
        }

        public override void OnAgentRemoved(
          Agent affectedAgent,
          Agent affectorAgent,
          AgentState agentState,
          KillingBlow killingBlow)
        {
            if(affectedAgent == null || affectorAgent == null || affectedAgent.Character == null)
            {
                return;
            }
            if (Test.followingHero != null && !Test.disable_XP && affectorAgent.IsPlayerControlled && isenemey(affectedAgent, affectorAgent))
            {
                if (affectedAgent.Character.IsHero)
                {
                    Test.xp += 100;
                    Test.ChangeFactionRelation(Test.followingHero.MapFaction, 100);
                    Test.ChangeLordRelation(Test.followingHero, 100);
                }
                else
                {
                    int xpgain = 4*((affectedAgent.Character.Level / 5) + 4);
                    Test.xp += xpgain;
                    Test.ChangeFactionRelation(Test.followingHero.MapFaction, xpgain);
                    Test.ChangeLordRelation(Test.followingHero, xpgain);
                }
            }
        }
    }
}