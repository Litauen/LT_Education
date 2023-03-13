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
                Logger.IMRed("LT_Education: Can't create book vendors... Reinstall the mod.");
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
                Hero vendor = HeroCreator.CreateSpecialHero(lt_vendor3_character_object, null, null, null, 65);
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
                    Settlement randomSettlement = LHelpers.GetRandomTown();
                    if (randomSettlement != null)
                    {
                        vendor.SetNewOccupation(Occupation.Special);
                        vendor.ChangeHeroGold(10000);
                        //Logger.IMGreen("Spawning vendor: " + vendor.Name.ToString() + " in " + randomSettlement.Name.ToString());
                        HeroHelper.SpawnHeroForTheFirstTime(vendor, randomSettlement);
                    }
                }
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

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.CurrentSettlement == settlement)
                {
                    //Logger.IM(vendor.FirstName.ToString() + " is here");
                    //LTEducation.MoveVendorToLocation(vendor, locationName, spawnTag);
                    MoveVendorToLocation(vendor, locationName, spawnTag);
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
                    List<Settlement> closestSettlements = LHelpers.GetClosestTownsFromSettlement(vendor.CurrentSettlement, 4);
                    if (closestSettlements.Count > 0)
                    {
                        int rndTown = rand.Next(closestSettlements.Count);
                        TeleportHeroAction.ApplyImmediateTeleportToSettlement(vendor, closestSettlements[rndTown]);
                    }
                }
                else
                {
                    // somehow vendor.CurrenSettlement == null
                    Settlement? rndTown = LHelpers.GetRandomTown();
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


    }
}
