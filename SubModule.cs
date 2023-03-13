using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace LT_Education
{

    public class SubModule : MBSubModuleBase
    {

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            try
            {
                if (gameStarterObject is not CampaignGameStarter) return;
                if (game.GameType is not Campaign) return;

                ((CampaignGameStarter)gameStarterObject).AddBehavior((CampaignBehaviorBase)new LT_EducationBehaviour());

                Logger.IM("{=LTE00000}LT_Education loaded");
            }
            catch (Exception ex)
            {
                Logger.IMRed("{=LTE00001}LT_Education: An Error occurred, when trying to load the mod into your current game.");
                Logger.LogError(ex);
            }
        }

        // works also
        //protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
        //{
        //    if (gameStarterObject is CampaignGameStarter started) { 
        //        started.AddBehavior(new LT_EducationBehaviour());
        //    }
        //}

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            //base.OnMissionBehaviorInitialize(mission);
            //mission.AddMissionBehavior(new LT_EducationMissionView());
        }

    }

}