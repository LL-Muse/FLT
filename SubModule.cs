using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using System.Collections.Generic;
using TaleWorlds.Library;
using HarmonyLib;
using System.Xml.Serialization;
using System.IO;
using System;

namespace FreelancerTemplate
{
    public class SubModule : MBSubModuleBase
    {
        public static Settings settings;
        public static readonly List<Action> ActionsToExecuteNextTick = new List<Action>();
        public static List<Recruit> AdditonalTroops;
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
        }

        public override void OnMissionBehaviourInitialize(Mission mission)
        {
            base.OnMissionBehaviourInitialize(mission);
            mission.AddMissionBehaviour((MissionBehaviour)new FreelancerMission());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (!(game.GameType is Campaign))
                return;
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new Test());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new TownRobberEvent());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new AbonadonedOrphanEvent());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new ExtortionByDesertersEvent());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new IllegalPoachersEvents());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new BanditAmbushEvent());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new RivalGangEvent());
            ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new TrainTroopsEvent());
        }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            loadSettings();
            loadEmpireRecruit();
            new Harmony("FreelancerTemplate").PatchAll();
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            foreach (Action action in SubModule.ActionsToExecuteNextTick)
                action();
            SubModule.ActionsToExecuteNextTick.Clear();
        }

        public static void ExecuteActionOnNextTick(Action action)
        {
            if (action == null)
            {
                return;
            }
            SubModule.ActionsToExecuteNextTick.Add(action);
        }

        private void loadSettings()
        {
            var path = Path.Combine(BasePath.Name, "Modules/FreelancerTemplate/settings.xml");
            XmlSerializer ser = new XmlSerializer(typeof(Settings));
            settings = ser.Deserialize(File.OpenRead(path)) as Settings;
        }

        private void loadEmpireRecruit()
        {
            var path = Path.Combine(BasePath.Name, "Modules/FreelancerTemplate/ModuleData/Additional_Troops.xml");
            XmlSerializer ser = new XmlSerializer(typeof(List<Recruit>));
            AdditonalTroops = ser.Deserialize(File.OpenRead(path)) as List<Recruit>;
        }

    }
}
