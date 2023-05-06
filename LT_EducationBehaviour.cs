using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        public static LT_EducationBehaviour Instance { get; set; }

        private bool _debug = false;

        private static GauntletLayer? _gauntletLayer;
        private static GauntletMovie? _gauntletMovie;
        private static EducationPopupVM? _popupVM;
        
        readonly int _booksInMod = 36;              // how many books are actually implemented into the mod
        IEnumerable<ItemObject>? _bookList;         // all education books in the game
        List<Hero>? _vendorList;                    // all book vendors in the game

        List<ItemObject> _tradeRoster = new();      // Item Roster for book trade

        private float _canRead;
        private int _minINTToRead = 4;              // minimum INT to be able to learn to read
        private readonly int _readPrice = 10;       // price to learn to read /h
        private int _lastHourOfLearning;            // to keep track for paid hours

        bool _readingInMenu = false;                // mark that we are reading in menu to handle special village case

        private int _bookInProgress;
        private float[] _bookProgress;

        // Scholars
        readonly int _totalScholars = 72;
        Settlement[] _scholarSettlements = new Settlement[72];
        private CampaignTime _startTimeOfTraining;
        private int _trainingInterrupted = 0;
        private bool _inTraining = false;
        private readonly int _trainingDuration = 8;
        private int _trainingRest = 10;
        private int _trainingScholarIndex = -1;
        private List<Hero> _trainingHeroList = new();


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
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickEvent);

            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));      

            //CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            //CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnAfterSettlementEntered));  
            //CampaignEvents.OnWorkshopChangedEvent.AddNonSerializedListener(this, OnWorkshopChangedEvent);
            //CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTownEvent);
            //CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
            //CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));             // OnTavernEntered
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
                if (Instance == null) return $"Debug failed";

                Instance._debug = true;
                Instance._minINTToRead = 2;
                Instance._trainingRest = 0;

                //Hero.MainHero.ChangeHeroGold(-1000000);

                return $"Debug enabled";
            }
            else if (args[0] == "2")
            {

                if (Instance == null || Instance._vendorList == null) return $"Debug failed";

                foreach (Hero vendor in Instance._vendorList)
                {
                    KillCharacterAction.ApplyByDeathMarkForced(vendor, true);
                }               

                return $"Vendors are dead :(";
            }
            else 
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
            // array length fix from previous versions to fit more books/scrolls
            if (size < 100)
            {
                Array.Resize(ref _bookProgress, 100);
            }

            // add [Read] to the read books
            MarkReadBooks();

        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {

            InitBookList();
            if (_bookList == null) return;

            InitVendors();
            if (_vendorList == null) return;

            InitScholars();

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

        //private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        //{
            //if (MobileParty.MainParty.CurrentSettlement == settlement) { 
            //    Logger.IMGreen("Welcome to " + settlement.Name.ToString());
            //}
        //}


        //private void OnAfterSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        //{

        //}


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
            MoveScholars();

            LTEducationTutelage.TutelageRun();

            if (_debug) ShowVendorLocations();
        }

        private void HourlyTickEvent()
        {
            //Logger.IM("1h passed");
            ReadPlayerBook();

            //TextObject to = new("AddQuickInformation");
            //Logger.AddQuickNotificationWithSound(to);
        }



        public override void SyncData(IDataStore dataStore)
        {          
            dataStore.SyncData<int>("LTEducation_bookInProgress", ref this._bookInProgress);
            dataStore.SyncData<float[]>("LTEducation_bookProgress", ref this._bookProgress);
            dataStore.SyncData<float>("LTEducation_canRead", ref this._canRead);

            dataStore.SyncData<Settlement[]>("LTEducation_scholarSettlements", ref this._scholarSettlements);
        }


    }


}