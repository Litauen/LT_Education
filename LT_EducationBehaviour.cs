using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private static LT_EducationBehaviour Instance { get; set; }

        private bool _debug = false;

        private static GauntletLayer? _gauntletLayer;
        private static GauntletMovie? _gauntletMovie;
        private static EducationPopupVM? _popupVM;

        IEnumerable<ItemObject>? _bookList; // all education books in the game
        List<Hero>? _vendorList;     // all book vendors in the game

        private float _canRead;
        private int _minINTToRead = 4;      // minimum INT to be able to learn to read
        private readonly int _readPrice = 10;        // price to learn to read /h
        private int _lastHourOfLearning;    // to keep track for paid hours

        private int _bookInProgress;
        private float[] _bookProgress;
        
        public LT_EducationBehaviour()
        {

            Instance = this;

            this._canRead = 0;
            this._bookInProgress = -1;
            this._bookProgress = new float[100];

            for (int i = 0; i < 100; i++)
            {
                this._bookProgress[i] = 0f;
            }

            _vendorList = new List<Hero>{};
        }


        public override void RegisterEvents()
        {
            //CampaignEvents.OnWorkshopChangedEvent.AddNonSerializedListener(this, OnWorkshopChangedEvent);
            //CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTownEvent);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));
            //CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnAfterSettlementEntered));
            // OnTavernEntered
            //CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
        }


        [CommandLineFunctionality.CommandLineArgumentFunction("debug", "lteducation")]
        public static string ConsoleDebug(List<string> args)
        {
            if (args.Count < 1)
            {
                return "You must provide an argument";
            }

            if (args[0] == "1")
            {
                Instance._debug = true;
                Instance._minINTToRead = 2;
                return $"Debug enabled";
            } else
            {
                Instance._debug = false;
                Instance._minINTToRead = 4;
                return $"Debug disabled";
            }
        }


        private void OnGameLoaded(CampaignGameStarter starter)
        {
            //Logger.IM("Game loaded");

            int size = _bookProgress.Length;
            //Logger.IM("_bookProgress length: " + size.ToString());

            // array length fix from previous versions to fit more books/scrolls
            if (size < 100)
            {
                Array.Resize(ref _bookProgress, 100);
            }

            //size = _bookProgress.Length;
            //Logger.IM("_bookProgress length: " + size.ToString());
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {

            // all education_books in the game
            _bookList = from x in Items.All where x.StringId.Contains("education_book") select x;
            if (_bookList == null) return;

            InitVendors();
            if (_vendorList == null) return;

            AddDialogs(starter);
            AddGameMenus(starter);
        }


        //private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        //{
        //    Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
        //    if (settlement.IsTown && CampaignMission.Current != null)
        //    {
        //        Location location = CampaignMission.Current.Location;
        //        if (location != null && location.StringId == "tavern")
        //        {

        //            Logger.IMGreen("We are in the Tavern");

        //            //location.AddLocationCharacters(new CreateLocationCharacterDelegate(TavernEmployeesCampaignBehavior.CreateRansomBroker), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
        //            //return;

        //        }
        //    }
        //}

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            //Logger.IMGreen("Welcome to " + settlement.Name.ToString());
        }


        private void OnAfterSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {

        }


        private void OnGameMenuOpened(MenuCallbackArgs obj)
        {
            //Logger.IM("OnGameMenuOpened");
            RelocateVendorInTownLocation();
        }

        //private void OnMissionEnded(IMission obj)
        //{
        //    Logger.IM("OnMissionEnded");
        //}


        private void WeeklyTickEvent()
        {                     
            if (_vendorList == null || _bookList == null) return;
            
            //LTEducation.RelocateBookVendors(_vendorList);

            RelocateVendorsToOtherTowns();

            GiveBooksToVendors();              
        }


        private void DailyTickEvent()
        {
            //if (_vendorList != null) LTEducation.RelocateBookVendors(_vendorList);

            if (_debug && _vendorList != null)
            {
                foreach (Hero vendor in _vendorList)
                {
                    Logger.IMGreen(vendor.FirstName.ToString() + " in " + vendor.CurrentSettlement.ToString());
                }
            }
        }

        private void HourlyTickEvent()
        {
            //Logger.IM("1h passed");
            ReadPlayerBook();
        }

        //private void DailyTickTownEvent(Town town)
        //{
        //    //foreach (var workshop in town.Workshops)
        //    //{
        //    //    Logger.IM(String.Format("{0} has a workshop {1}", town.Name, workshop.Name));
        //    //}
        //}

        //private void OnWorkshopChangedEvent(Workshop workshop, Hero oldOwningHero, WorkshopType type)
        //{

        //}

        public override void SyncData(IDataStore dataStore)
        {          
            dataStore.SyncData<int>("LTEducation_bookInProgress", ref this._bookInProgress);
            dataStore.SyncData<float[]>("LTEducation_bookProgress", ref this._bookProgress);
            dataStore.SyncData<float>("LTEducation_canRead", ref this._canRead);
        }


    }


}