using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using Bannerlord.UIExtenderEx;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.InputSystem;
using TaleWorlds.Engine;
using LT.Logger;
using LT.Helpers;
using LT.UI;

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

                //Logger.IMGrey("LT_Education loaded");
            }
            catch (Exception ex)
            {
                LTLogger.IMRed("LT_Education: An Error occurred, when trying to load the mod into your current game.");
                LTLogger.LogError(ex);
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
            Harmony harmony = new Harmony("lt_education");
            harmony.PatchAll();

            UIExtender _UIextender = new UIExtender("lt_education");
            _UIextender.Register(typeof(SubModule).Assembly);
            _UIextender.Enable();

        }

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            //base.OnMissionBehaviorInitialize(mission);
            //mission.AddMissionBehavior(new LT_EducationMissionView());
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            LTLogger.IMGrey(LTHelpers.GetModName() + " Loaded");
        }


        protected override void OnApplicationTick(float dt)
        {
            if (Game.Current != null)
            {
                if (Input.IsKeyDown(InputKey.LeftAlt) && Input.IsKeyDown(InputKey.F12) && //Input.IsKeyDown(InputKey.O) && 
                    Game.Current.GameStateManager.ActiveState.GetType() == typeof(MapState) && !Game.Current.GameStateManager.ActiveState.IsMenuState && !Game.Current.GameStateManager.ActiveState.IsMission)
                {
                    SoundEvent.PlaySound2D("event:/ui/notification/quest_start");
                    LTUIManager.Instance.ShowWindow("BookStash", "");
                }

                //if (Input.IsKeyDown(InputKey.F11) && 
                //  Game.Current.GameStateManager.ActiveState.GetType() == typeof(MapState) && !Game.Current.GameStateManager.ActiveState.IsMenuState && !Game.Current.GameStateManager.ActiveState.IsMission)
                //{
                //    //LTLogger.IMRed("F11");
                //    //MBInformationManager.Clear();   // does not clear round notification
                //}

            }
        }


    }

}