using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using LT.Helpers;
using LT.Logger;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        // populate vendorList
        private void InitVendors()
        {
            if (_vendorList == null) _vendorList = new List<Hero> { };

            // to allow name translations
            //_vendorList = Hero.FindAll((Hero x) => x.Name.Contains("Book Vendor")); // this does not allow to translate a name
            foreach (Hero hero in Hero.FindAll((Hero x) => x.Occupation == Occupation.Special))
            {
                if (hero.CharacterObject != null && hero.CharacterObject.OriginalCharacter != null && hero.CharacterObject.OriginalCharacter.StringId.Contains("lt_education_book_vendor"))
                {
                    _vendorList.Add(hero);
                    //Logger.IM(hero.FirstName.ToString() + " found");
                }
            }

            if (_vendorList.Count<Hero>() < 3)
            {
                CreateVendors();
            }
        }


        private void CreateVendors()
        {

            if (_vendorList == null) _vendorList = new List<Hero> { };

            CharacterObject lt_vendor1_character_object = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>("lt_education_book_vendor1");
            CharacterObject lt_vendor2_character_object = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>("lt_education_book_vendor2");
            CharacterObject lt_vendor3_character_object = TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<CharacterObject>("lt_education_book_vendor3");

            if (lt_vendor1_character_object == null || lt_vendor2_character_object == null || lt_vendor3_character_object == null)
            {
                LTLogger.IMRed("LT_Education: Can't create book vendors... Reinstall the mod.");
                return;
            }

            bool vendor1OK = false;
            bool vendor2OK = false;
            bool vendor3OK = false;

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.CharacterObject.OriginalCharacter.StringId == "lt_education_book_vendor1") vendor1OK = true;
                if (vendor.CharacterObject.OriginalCharacter.StringId == "lt_education_book_vendor2") vendor2OK = true;
                if (vendor.CharacterObject.OriginalCharacter.StringId == "lt_education_book_vendor3") vendor3OK = true;
            }

            TextObject vendorTO = new("{=LTE00304} the Book Vendor");

            //if (Hero.FindAll((Hero x) => x.Name.Contains("Eadric the Book Vendor")).Count<Hero>() < 1)
            if (!vendor1OK)
            {
                //Logger.IMRed("Eadric is not present");
                Hero vendor = HeroCreator.CreateSpecialHero(lt_vendor1_character_object, null, null, null, 45);
                TextObject VendorFirstName = new("{=LTE00301}Eadric", null);
                TextObject VendorFullName = new(VendorFirstName.ToString() + vendorTO.ToString());
                vendor.SetName(VendorFullName, VendorFirstName);
                _vendorList.Add(vendor);
            }

            //if (Hero.FindAll((Hero x) => x.Name.Contains("Ingeborg the Book Vendor")).Count<Hero>() < 1)
            if (!vendor2OK)
            {
                //Logger.IMRed("Ingeborg is not present");
                Hero vendor = HeroCreator.CreateSpecialHero(lt_vendor2_character_object, null, null, null, 25);
                TextObject VendorFirstName = new("{=LTE00302}Ingeborg", null);
                //TextObject VendorFullName = new(VendorFirstName + "{=LTE00304} the Book Vendor");
                TextObject VendorFullName = new(VendorFirstName.ToString() + vendorTO.ToString());
                vendor.SetName(VendorFullName, VendorFirstName);
                _vendorList.Add(vendor);
            }

            //if (Hero.FindAll((Hero x) => x.Name.Contains("Ahsan the Book Vendor")).Count<Hero>() < 1)
            if (!vendor3OK)
            {
                //Logger.IMRed("Ahsan is not present");
                Hero vendor = HeroCreator.CreateSpecialHero(lt_vendor3_character_object, null, null, null, 61);
                TextObject VendorFirstName = new("{=LTE00303}Ahsan", null);
                //TextObject VendorFullName = new(VendorFirstName + "{=LTE00304} the Book Vendor");
                TextObject VendorFullName = new(VendorFirstName.ToString() + vendorTO.ToString());
                vendor.SetName(VendorFullName, VendorFirstName);

                // does not apply like this, vendor is still skinny
                //vendor.Weight = 0.9522f;
                //vendor.Build = 0.9738f;
                _vendorList.Add(vendor);
            }

            //_vendorList = Hero.FindAll((Hero x) => x.Name.Contains("Book Vendor"));

            // TODO: barber does not show in the top tab, check top bar template and find out based on which property NPC is shown or not

            // TODO: hide from Encyclopedia
            //'CharacterObject.HiddenInEncylopedia' cannot be assigned to --it is read only
            //bookVendor.CharacterObject.HiddenInEncylopedia = true;

            //int bookCount = _bookList.Count<ItemObject>();
            //Logger.IM("Found books: " + bookCount.ToString());

            //Random rand = new();

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.IsNotSpawned)
                {
                    Settlement? randomSettlement = LTHelpers.GetRandomTown();
                    if (randomSettlement != null)
                    {
                        vendor.SetNewOccupation(Occupation.Special);
                        vendor.ChangeHeroGold(10000);
                        //Logger.IMGreen("Spawning vendor: " + vendor.Name.ToString() + " in " + randomSettlement.Name.ToString());
                        HeroHelper.SpawnHeroForTheFirstTime(vendor, randomSettlement);
                    }
                }

                SetVendorSkills(vendor);

            }

            GiveBooksToVendors();

        }


        // changing vendor location inside the city based on time of the day
        private void RelocateVendorInTownLocation()
        {

            if (Campaign.Current.GameMode != CampaignGameMode.Campaign || MobileParty.MainParty.CurrentSettlement == null || LocationComplex.Current == null || _vendorList == null) return;
            Settlement settlement = MobileParty.MainParty.CurrentSettlement;
            //if (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage) return;
            if (!settlement.IsTown) return;     // currently only towns

            // different location in town based on hour
            int hour = (int)CampaignTime.Now.CurrentHourInDay;
            string locationName;
            if (hour > 15) locationName = "tavern";
            else if (hour > 7) locationName = "center";
            else locationName = "lordshall";

            // random animations, kind of
            Random rand = new();
            string[] spawnTagLines = new string[]
            {
                "npc_common",
                "sp_notable"
            };
            string spawnTag = spawnTagLines[rand.Next(spawnTagLines.Length)];

            //Logger.IM(hour + " " + locationName + " " + spawnTag);

            //List<Hero> tmpVendorList = new List<Hero>(_vendorList);
            //foreach (Hero vendor in tmpVendorList)

            foreach (Hero vendor in _vendorList)
            {
                
                // update encyclopedia only if we met
                if (vendor.EncyclopediaText == null && vendor.HasMet)
                {
                    SetVendorBackStory(vendor);
                }
                
                if (vendor.CurrentSettlement == settlement)
                {

                    if (vendor.IsDead)
                    {
                        //handling special case when vendor is dead but somehow still in the settlement -> resurrect
                        //when vendor truly dies, CurrentSettlement is null and he is gone
                        vendor.ChangeState(Hero.CharacterStates.Active);                     
                    }
                    else
                    {
                        MoveVendorToLocation(vendor, locationName, spawnTag);
                    }
                   
                }
            }

        }


        // moves vendor to another location inside the same settlement (when player is in this settlement)
        private void MoveVendorToLocation(Hero vendor, string locationName, string spawnTag = "sp_notable")
        {
            if (LocationComplex.Current == null || LocationComplex.Current.GetLocationWithId(locationName) == null) return;

            IFaction mapFaction3 = vendor.MapFaction;
            uint color3 = (mapFaction3 != null) ? mapFaction3.Color : 4291609515U;
            IFaction mapFaction4 = vendor.MapFaction;
            uint color4 = (mapFaction4 != null) ? mapFaction4.Color : 4291609515U;

            Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(vendor.CharacterObject.Race);
            AgentData agentData = new AgentData(new PartyAgentOrigin(PartyBase.MainParty, vendor.CharacterObject, -1, default(UniqueTroopDescriptor), false)).Monster(baseMonsterFromRace).NoHorses(true).ClothingColor1(color3).ClothingColor2(color4);
            LocationCharacter locationCharacter = new(agentData, new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddCompanionBehaviors), spawnTag, false,
                    LocationCharacter.CharacterRelations.Friendly, null, true, false, null, false, true, true);

            Location location = LocationComplex.Current.GetLocationWithId(locationName);

            if (location == null) return;

            location.AddCharacter(locationCharacter);

        }


        private void RelocateVendorsToOtherTowns()
        {
            if (_vendorList == null || _vendorList.Count<Hero>() == 0) return;

            Random rand = new();

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.CurrentSettlement != null)
                {
                    List<Settlement> closestSettlements = LTHelpers.GetClosestTownsFromSettlement(vendor.CurrentSettlement, 4);
                    if (closestSettlements.Count > 0)
                    {
                        int rndTown = rand.Next(closestSettlements.Count);
                        TeleportHeroAction.ApplyImmediateTeleportToSettlement(vendor, closestSettlements[rndTown]);
                    }
                }
                else
                {
                    // somehow vendor.CurrenSettlement == null
                    Settlement? rndTown = LTHelpers.GetRandomTown();
                    if (rndTown != null)
                    {
                        TeleportHeroAction.ApplyImmediateTeleportToSettlement(vendor, rndTown);
                    }
                }
            }
        }



        private int GetVendorID(CharacterObject co)
        {
            if (co.OriginalCharacter.StringId == "lt_education_book_vendor2") return 2;
            if (co.OriginalCharacter.StringId == "lt_education_book_vendor3") return 3;
            return 1;
        }


        private void SetVendorBackStory(Hero vendor)
        {
            int vendorID = GetVendorID(vendor.CharacterObject);

            switch(vendorID)
            {
                case 1:   
                    vendor.EncyclopediaText = new TextObject("{=LTE00305}Eadric is a middle-aged book vendor who hails from the Empire but pretends to be from the North. Short in height and always wearing monk clothes, he speaks in a pleasant voice and is incredibly polite. However, Eadric is also mysterious, always careful and cautious, as if hiding from someone. His past is shrouded in secrecy, and he keeps himself hidden from those around him, never revealing too much about himself. Despite this, he is known for his vast knowledge of literature and his impeccable customer service."); 
                    break;
                case 2:
                    vendor.EncyclopediaText = new TextObject("{=LTE00306}Ingeborg is a young, tall, blonde girl from the Nord who can defend herself. Her father, a former soldier, taught her how to fight, while her mother instilled in her a passion for literature. She travels and sells books, using her skills to protect herself along the way. Despite her tough exterior, Ingeborg has a kind heart and is known for her extensive knowledge of literature.");
                    break;
                case 3:
                    vendor.EncyclopediaText = new TextObject("{=LTE00307}Ahsan, an old man from a southern village, was always known for his hot temper. Despite his abrasive personality, he had a deep love of books, becoming a respected book vendor. As he grew older, Ahsan became increasingly isolated, retreating into his books as a shield against the world. Despite his grumpiness, he was revered for his knowledge and dedication to literature.");
                    break;
            }
        }


        private void SetVendorSkills(Hero vendor)
        {
            int vendorID = GetVendorID(vendor.CharacterObject);

            switch (vendorID)
            {
                case 1:
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Throwing, 262);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Crossbow, 211);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Athletics, 226);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Roguery, 249);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Trade, 211);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Engineering, 197);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Charm, 31);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Steward, 15);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Leadership, 22);
                    break;
                case 2:
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.TwoHanded, 255);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Athletics, 276);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Scouting, 189);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Charm, 231);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Trade, 171);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Medicine, 183);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Steward, 17);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Leadership, 25);
                    break;
                case 3:
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.OneHanded, 265);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Athletics, 251);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Riding, 177);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Tactics, 254);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Trade, 224);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Leadership, 163);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Charm, 11);
                    vendor.HeroDeveloper.SetInitialSkillLevel(DefaultSkills.Steward, 18);
                    break;
            }
        }


        private void ChangeRelationWithVendor(Hero vendor)
        {

            // Eadric   +4-6 once per week on meeting
            // Ingeborg +9-11 once per week on meeting
            // Ahsan    -3 on each meeting :D

            Random rand = new();

            int vendorID = GetVendorID(vendor.CharacterObject);
            CampaignTime lastMeetingTimeWithPlayer = vendor.LastMeetingTimeWithPlayer;

            if (vendorID == 1 && lastMeetingTimeWithPlayer.ElapsedDaysUntilNow > 7)  // Eadric
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, vendor, 4 + rand.Next(2), false);
            } 
            else if (vendorID == 2 && lastMeetingTimeWithPlayer.ElapsedDaysUntilNow > 7)  // Ingeborg
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, vendor, 9 + rand.Next(2), false);
            }
            else if (vendorID == 3)   // Ahsan
            {
                ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, vendor, -3, false);
            }
        }


        private void ShowVendorLocations()
        {

            if (_vendorList == null) return;

            foreach (Hero vendor in _vendorList)
            {
                LTLogger.IMGreen(vendor.FirstName.ToString() + " in " + vendor.CurrentSettlement.ToString());
            }
        }


        public bool IsAnyVendorInTown(Settlement settlement)
        {
            if (settlement == null) return false;
            if (!settlement.IsTown) return false;   // vendors only in towns
            if (_vendorList == null) return false;
            if (_vendorList.Count<Hero>() == 0) return false;

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.CurrentSettlement == settlement) return true;
            }
            return false;
        }

    }
}
