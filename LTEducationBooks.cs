using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using LT.Logger;
using TaleWorlds.CampaignSystem.GameMenus;
using LT.UI;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private void MarkReadBooks()
        {
            for (int i = 1; i <= _booksInMod; i++)
            {
                MarkBookItemAsRead(i);
            }
        }


        private void MarkBookItemAsRead(int bookIndex)
        {
            if (bookIndex < 1 || bookIndex > _booksInMod) return;   // wrong book
            if (_bookProgress[bookIndex] < 100) return;     // book is not finished yet

            //ItemObject bookItem = _bookList.ElementAt(bookIndex);

            ItemObject bookItem = GetBookItem(bookIndex);

            if (bookItem == null) return; // can't find our book

            if (!bookItem.Name.Contains("[Read]")) {
                // Harmony magic
                var propertyInfo = AccessTools.Property(typeof(ItemObject), "Name");
                var setter = propertyInfo.GetSetMethod(true);
                TextObject readTO = new("{=LTE00531} [Read]");
                TextObject newItemName = new(bookItem.Name + readTO.ToString());
                setter.Invoke(bookItem, new object[] { newItemName });
            }

            //Logger.IM(bookItem.Name.ToString());
            //Logger.IM(bookIndex.ToString());
        }


        public ItemObject GetBookItem(int bookIndex)
        {
            return TaleWorlds.ObjectSystem.MBObjectManager.Instance.GetObject<ItemObject>(GetBookStringId(bookIndex));
        }


        private void InitBookList()
        {
            // TODO: rewrite to be ordered by bookIndex
            _bookList = from x in Items.All where x.StringId.Contains("education_book") select x;
        }

        private void GiveBooksToVendors()
        {

            if (_vendorList == null) return;

            bool debug = false; // local debug

            int maxVendorBooks = 12;
            int minVendorBooks = 8;

            int bookCount = _bookList.Count<ItemObject>() - 1; // remove testing book
            Random rand = new();
            foreach (Hero vendor in _vendorList)
            {

                List<ItemObject> vendorBooks = GetVendorBooks(vendor).ToList();

                //vendor.SpecialItems.Remove(book);
                //vendor.SpecialItems.Add(book);
                //Hero.MainHero.SpecialItems.RemoveAll((ItemObject x) => x.StringId.Contains("education_book"));

                int count = vendorBooks.Count;
                if (debug) LTLogger.IM(vendor.FirstName.ToString() + " has books: " + count);



                // if > maxVendorBooks, leave maxVendorBooks - if somehow vendor got many books, let's keep it manageable, it is hard to carry so many books :)
                if (count > maxVendorBooks)
                {
                    int removeCount = count - maxVendorBooks;
                    if (removeCount > 0)
                    {
                        vendorBooks.RemoveRange(maxVendorBooks, removeCount);
                    }
                    count = vendorBooks.Count;
                    if (debug) LTLogger.IM("Removed " + removeCount + " from " + vendor.FirstName.ToString() + " books left: " + count);
                }

                // simulate vendor selling random amount of books to somebody
                //int sellRndAmount = rand.Next(count);
                //for (int i = 0;i < sellRndAmount; i++)
                //{
                //    vendorBooks.RemoveAt(0);
                //}
                //count = vendorBooks.Count;
                //Logger.IM("After selling " + vendor.FirstName.ToString() + " has books left: " + count);


                // remove 3 random book from the vendor, let's say they were 'sold' to somebody else
                if (count > 3)
                {
                    vendorBooks.RemoveAt(rand.Next(vendorBooks.Count));
                    vendorBooks.RemoveAt(rand.Next(vendorBooks.Count));
                    vendorBooks.RemoveAt(rand.Next(vendorBooks.Count));
                    count = vendorBooks.Count;
                    if (debug) LTLogger.IM("Removed 3 books from " + vendor.FirstName.ToString() + ", books left: " + count);
                }

                // if vendor has less then minVendorBooks books, make him have [minVendorBooks-maxVendorBooks] books, to refresh his inventory
                if (count < minVendorBooks)
                {

                    int booksToAdd = rand.Next(minVendorBooks - 1) + (minVendorBooks - count);

                    string addedBooks = "";
                    for (int i = 0; i < booksToAdd; i++)
                    {
                        int rndBook = rand.Next(bookCount) + 1;
                        vendorBooks.Add(_bookList.ElementAt(rndBook));
                        addedBooks = addedBooks + " " + rndBook;
                    }
                    if (debug) LTLogger.IM("Added books to " + vendor.FirstName.ToString() + ": " + addedBooks);

                }

                // update vendor's special items
                vendor.SpecialItems.RemoveAll((ItemObject x) => x.StringId.Contains("education_book"));
                foreach (ItemObject book in vendorBooks)
                {
                    vendor.SpecialItems.Add(book);
                }

                if (debug) LTLogger.IM(vendor.FirstName.ToString() + " has " + GetVendorBooksCount(vendor).ToString() + " books");
            }
        }



        private List<ItemObject> GetPartyBooks()
        {
            List<ItemObject> bookList = new();

            if (_bookList == null) return bookList;

            foreach (ItemObject item in _bookList)
            {
                int bookCount = MobileParty.MainParty.ItemRoster.GetItemNumber(item);
                if (bookCount > 0)
                {
                    for (int i = 0; i < bookCount; i++)
                    {
                        bookList.Add(item);
                    }
                }
            }
            return bookList;
        }


        // bookType 0 - books+scrolls, 1 - books, 2 - scrolls
        public List<ItemObject> GetUniquePartyBooks(int bookType = 0)
        {
            List<ItemObject> bookList = new();

            if (_bookList == null) return bookList;

            int range1 = 0, range2 = 37;
            if (bookType == 1) { range1 = 0; range2 = 19; } else if (bookType == 2) { range1 = 18; range2 = 37; }

            foreach (ItemObject item in _bookList)
            {
                int bookIndex = GetBookIndex(item.StringId);
                if (bookIndex > range1 && bookIndex < range2)
                {
                    int bookCount = MobileParty.MainParty.ItemRoster.GetItemNumber(item);
                    if (bookCount > 0)
                    {
                        bookList.Add(item);
                    }
                }

            }
            return bookList;
        }



        static public IEnumerable<ItemObject> GetVendorBooks(Hero vendor)
        {
            IEnumerable<ItemObject> bookList = from x in vendor.SpecialItems where x.StringId.Contains("education_book") select x;
            return bookList;
        }


        static public int GetVendorBooksCount(Hero vendor)
        {
            if (vendor == null) return 0;
            IEnumerable<ItemObject> bookList = from x in vendor.SpecialItems where x.StringId.Contains("education_book") select x;
            return bookList.Count();
        }

        //returns amount/count of all books Player's party has
        static public int GetPartyBookAmount(IEnumerable<ItemObject> bookList)
        {
            int partyBookCount = 0;
            foreach (ItemObject item in bookList)
            {
                partyBookCount += MobileParty.MainParty.ItemRoster.GetItemNumber(item);
            }
            return partyBookCount;
        }






        private float LearningToReadPerHourProgress()
        {
            float intelligence = Hero.MainHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
            if (intelligence > 10) intelligence = 10;
            float progress = (intelligence - 4f) * 0.2f + 1f;
            return progress;
        }


        private void ReadPlayerBookPassive()
        {
            if (_canRead < 100) return;
            if (_bookInProgress < 1 || _bookInProgress > _booksInMod) return;
            if (Hero.MainHero.IsPrisoner) return;

            _bookProgress[_bookInProgress] += 0.5f;

            if (_bookProgress[_bookInProgress] >= 100f)
            {
                MainHeroFinishReading();
            }


        }


        private void ReadPlayerBookActive()
        {

            if (_canRead < 100) return;

            if (!_readingInMenu) return;    // read only from menu

            if (this._inTraining) return;


            Hero hero = Hero.MainHero;
            MobileParty party = hero.PartyBelongedTo;
            //Logger.IMRed("Reading a book");

            if (party == null) return;  // when we are captured

            bool computeIsWaiting = party.ComputeIsWaiting();



            // special case with retarted village logic when reading in the village menu
            if (_readingInMenu && !computeIsWaiting && hero.CurrentSettlement != null && hero.CurrentSettlement.IsVillage) computeIsWaiting = true;

            if (_bookInProgress > -1 && _bookInProgress < _booksInMod + 1 && _bookProgress[_bookInProgress] < 100 &&
               !hero.IsPrisoner && computeIsWaiting && party.BesiegedSettlement == null && party.AttachedTo == null)
            {
                //Logger.IMRed("Reading a book");

                if (hero.CurrentSettlement == null)     // we are not in town/castle
                {
                    // maybe we are in/near the village?
                    //hero.CurrentSettlement == null in villages, !null in towns/castles when "waiting"
                    if (party.LastVisitedSettlement != null && party.LastVisitedSettlement.IsVillage)
                    {
                        float distance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, party.LastVisitedSettlement);
                        //Logger.IM("Distance to last visited settlement " + party.LastVisitedSettlement.Name.ToString() + ": " + distance.ToString());
                        if (distance < 1.05f) // distance, to decide if we are "inside" the village with 0.05 safety margin, usually its <1
                        {
                            // all good, we are staying/reading in the village
                        }
                        else
                        {
                            return; // last visited village is too far
                        }
                    }
                    else
                    {
                        return;     // last visited Settlement is not village
                    }
                }
                // all good, we are in the town/castle, or using read menu in the village
            }
            else
            {
                return;
            }

            if (!HeroHasBook(Hero.MainHero, _bookInProgress))
            {
                // mark that we are not reading anything if we lost/sold the book we were reading
                HeroStopReadingAndReturnBookToParty(Hero.MainHero);
                LTLogger.IMRed("{=LTE00524}You don't have a book you were reading before...");
                return;
            }

            float intelligence = Hero.MainHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
            if (intelligence < 2) intelligence = 2;
            if (intelligence > 10) intelligence = 10;

            float daysToReadWithINT2 = 15;
            float daysToReadWithINT10 = 3;
            float progressPerHour = (float)100 / 24 / (daysToReadWithINT2 - (daysToReadWithINT2 - daysToReadWithINT10) / 8 * (intelligence - 2));

            if (_bookInProgress > 18 && _bookInProgress < 37) progressPerHour *= 3; // read scrolls x3 faster than books

            _bookProgress[_bookInProgress] += progressPerHour;

            if (_debug) _bookProgress[_bookInProgress] += 10;

            float progress = (float)_bookProgress[_bookInProgress];

            if (progress < 100)
            {
                // log only each 6 hours to avoid message spamming
                int hour = (int)CampaignTime.Now.CurrentHourInDay;
                if (hour % 6 == 0 && _bookList != null)
                {
                    MBTextManager.SetTextVariable("READING_DATA", GetBookNameByIndex(_bookInProgress) + " [" + progress.ToString("0") + "%]", false);
                    LTLogger.IM("{=LTE00525}Reading {READING_DATA}");
                    //Logger.IM("Reading " + GetBookNameByIndex(_bookInProgress) + " [" + progress.ToString("0") + "%]");
                }

            }
            else
            {
                MainHeroFinishReading();

                GameMenu.SwitchToMenu("education_menu");
            }

        }


        private void MainHeroFinishReading()
        {
            _bookProgress[_bookInProgress] = 100;
            if (_bookInProgress < 19)
            {
                FinishBook(Hero.MainHero, _bookInProgress);
            }
            else
            {
                FinishScroll(Hero.MainHero, _bookInProgress);
            }

            MarkBookItemAsRead(_bookInProgress);

            HeroStopReadingAndReturnBookToParty(Hero.MainHero);
        }


        public void FinishBook(Hero hero, int bookIndex)
        {
            if (hero == null) return;
            if (bookIndex < 0 || bookIndex > 18 || _bookList == null) return;

            if (bookIndex == 0) bookIndex = 1;  // to avoid problems with testing book

            string bookName = GetBookNameByIndex(bookIndex);

            // Increase skills
            SkillObject skill = GetSkillByBookIndex(bookIndex);
            //SkillObject skill = bookIndex switch
            //{
            //    1 => DefaultSkills.OneHanded,
            //    2 => DefaultSkills.TwoHanded,
            //    3 => DefaultSkills.Polearm,
            //    4 => DefaultSkills.Bow,
            //    5 => DefaultSkills.Crossbow,
            //    6 => DefaultSkills.Throwing,
            //    7 => DefaultSkills.Riding,
            //    8 => DefaultSkills.Athletics,
            //    9 => DefaultSkills.Crafting,
            //    10 => DefaultSkills.Scouting,
            //    11 => DefaultSkills.Tactics,
            //    12 => DefaultSkills.Roguery,
            //    13 => DefaultSkills.Charm,
            //    14 => DefaultSkills.Leadership,
            //    15 => DefaultSkills.Trade,
            //    16 => DefaultSkills.Steward,
            //    17 => DefaultSkills.Medicine,
            //    18 => DefaultSkills.Engineering,
            //    _ => DefaultSkills.OneHanded,
            //};

            int skillValue = hero.GetSkillValue(skill);

            //Logger.IMGreen("skillValue " + skillValue);

            Random rand = new();

            // with slight variation to be not so boring
            int increase = 30 + rand.Next(7) - 3;
            if (skillValue > 300)
            {
                increase = 0;
            }
            else if (skillValue > 250)
            {
                increase = 5 + rand.Next(3) - 1;
            }
            else if (skillValue > 200)
            {
                increase = 10 + rand.Next(3) - 1;
            }
            else if (skillValue > 150)
            {
                increase = 15 + rand.Next(3) - 1;
            }
            else if (skillValue > 100)
            {
                increase = 20 + rand.Next(5) - 2;
            }

            hero.HeroDeveloper.ChangeSkillLevel(skill, increase, true);

            if (hero == Hero.MainHero)
            {
                // stop waiting in settlement
                PlayerEncounter.Current.IsPlayerWaiting = false;

                MBTextManager.SetTextVariable("FINISHED_BOOK", bookName, false);
                LTLogger.IMGreen("{=LTE00526}Finished reading: {FINISHED_BOOK}!");

                // show popup
                MBTextManager.SetTextVariable("BOOK_SKILL", skill.ToString(), false);
                MBTextManager.SetTextVariable("BOOK_SKILL_INC", increase.ToString(), false);
                TextObject popupText;
                if (increase > 0) popupText = new("{=LTE00527}That increased your {BOOK_SKILL} skill by {BOOK_SKILL_INC}!");
                else popupText = new("{=LTE00528}You are too skilled in {BOOK_SKILL},\n so the book was a waste of time...");
                string spriteName = "lt_education_book" + bookIndex.ToString();
                SoundEvent.PlaySound2D("event:/ui/notification/peace");
                LT_EducationBehaviour.CreatePopupVMLayer("{=LTE00529}Finished reading", "", bookName, popupText.ToString(), spriteName, "{=LTE00530}Continue");
            } else
            {
                TextObject msg = new("{=LTE00577}{HERO_NAME} finished reading {BOOK_NAME}");
                msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                msg.SetTextVariable("BOOK_NAME", bookName);
                LTLogger.IMGreen(msg.ToString());

                //msg = new("{=LTE00578}{HERO_NAME}: {BOOK_SKILL} skill increased by {BOOK_SKILL_INC}");
                //msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                //msg.SetTextVariable("BOOK_SKILL", skill.ToString());
                //msg.SetTextVariable("BOOK_SKILL_INC", increase.ToString());
                //LTLogger.IMGreen(msg.ToString());
            }

        }


        public void FinishScroll(Hero hero, int bookIndex)
        {

            if (hero == null) return;
            if (bookIndex < 19 || bookIndex > 36 || _bookList == null) return;

            string bookName = GetBookNameByIndex(bookIndex);

            // Increase skills
            SkillObject skill = GetSkillByBookIndex(bookIndex);
            //SkillObject skill = bookIndex switch
            //{
            //    19 => DefaultSkills.OneHanded,
            //    20 => DefaultSkills.TwoHanded,
            //    21 => DefaultSkills.Polearm,
            //    22 => DefaultSkills.Bow,
            //    23 => DefaultSkills.Crossbow,
            //    24 => DefaultSkills.Throwing,
            //    25 => DefaultSkills.Riding,
            //    26 => DefaultSkills.Athletics,
            //    27 => DefaultSkills.Crafting,
            //    28 => DefaultSkills.Scouting,
            //    29 => DefaultSkills.Tactics,
            //    30 => DefaultSkills.Roguery,
            //    31 => DefaultSkills.Charm,
            //    32 => DefaultSkills.Leadership,
            //    33 => DefaultSkills.Trade,
            //    34 => DefaultSkills.Steward,
            //    35 => DefaultSkills.Medicine,
            //    36 => DefaultSkills.Engineering,
            //    _ => DefaultSkills.OneHanded,
            //};

            int skillValue = hero.GetSkillValue(skill);

            //Logger.IMGreen("skillValue " + skillValue);

            Random rand = new();

            // with slight variation to be not so boring
            int increase = 10 + rand.Next(5) - 2;
            if (skillValue > 300)
            {
                increase = 0;
            }
            else if (skillValue > 250)
            {
                increase = 1;
            }
            else if (skillValue > 200)
            {
                increase = 3 + rand.Next(3) - 1;
            }
            else if (skillValue > 150)
            {
                increase = 5 + rand.Next(3) - 1;
            }
            else if (skillValue > 100)
            {
                increase = 8 + rand.Next(3) - 1;
            }

            hero.HeroDeveloper.ChangeSkillLevel(skill, increase, true);

            if (hero == Hero.MainHero)
            {
                // stop waiting in settlement (if waiting)
                if (PlayerEncounter.Current != null) PlayerEncounter.Current.IsPlayerWaiting = false;

                MBTextManager.SetTextVariable("FINISHED_BOOK", bookName, false);
                LTLogger.IMGreen("{=LTE00526}Finished reading {FINISHED_BOOK}!");

                // show popup
                MBTextManager.SetTextVariable("BOOK_SKILL", skill.ToString(), false);
                MBTextManager.SetTextVariable("BOOK_SKILL_INC", increase.ToString(), false);
                TextObject popupText;
                if (increase > 0) popupText = new("{=LTE00527}That increased your {BOOK_SKILL} skill by {BOOK_SKILL_INC}!");
                else popupText = new("{=LTE00528}You are too skilled in {BOOK_SKILL},\n so the book was a waste of time...");
                string spriteName = "lt_education_book" + bookIndex.ToString();
                SoundEvent.PlaySound2D("event:/ui/notification/peace");
                LT_EducationBehaviour.CreatePopupVMLayer("{=LTE00529}Finished reading", "", bookName, popupText.ToString(), spriteName, "{=LTE00530}Continue");
            } else
            {
                TextObject msg = new("{=LTE00577}{HERO_NAME} finished reading {BOOK_NAME}");
                msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                msg.SetTextVariable("BOOK_NAME", bookName);
                LTLogger.IMGreen(msg.ToString());

                //msg = new("{=LTE00578}{HERO_NAME}: {BOOK_SKILL} skill increased by {BOOK_SKILL_INC}");
                //msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                //msg.SetTextVariable("BOOK_SKILL", skill.ToString());
                //msg.SetTextVariable("BOOK_SKILL_INC", increase.ToString());
                //LTLogger.IMGreen(msg.ToString());
            }

        }





        public bool HeroHasBook(Hero hero, int bookIndex)
        {
            if (hero == null) return false;
            if (hero.SpecialItems == null) return false;
            if (hero.SpecialItems.Count == 0) return false;

            ItemObject book = GetBookItem(bookIndex);
            if (book == null) return false;

            if (hero.SpecialItems.Contains(book)) return true;

            return false;
        }



        public void HeroStopReadingAndReturnBookToParty(Hero hero)
        {
            if (hero == null) return;
            if (hero.SpecialItems == null) return;
            if (hero.SpecialItems.Count == 0) return;
            if (_bookList == null) return;

            foreach (ItemObject book in _bookList)
            {
                if (hero.SpecialItems.Contains(book))
                {
                    hero.SpecialItems.Remove(book);

                    ItemRosterElement itemRoster = new ItemRosterElement(book, 1, null);
                    MobileParty.MainParty.ItemRoster.Add(itemRoster);
                }
            }

            if (hero == Hero.MainHero)
            {
                _bookInProgress = -1;
            } else
            {
                this._LTECompanions.CompanionStopReadBook(hero);
            }
        }

        public void HeroSelectBookToRead(Hero hero, ItemObject book)
        {
            if (hero == null) return;
            if (book == null) return;
            if (MobileParty.MainParty.ItemRoster == null) return;

            // return the book already reading
            HeroStopReadingAndReturnBookToParty(hero);

            // remove book from MainParty
            ItemRosterElement itemRoster = new ItemRosterElement(book, 1, null);
            MobileParty.MainParty.ItemRoster.Remove(itemRoster);

            // add book to heroe's 'Special Items'
            hero.SpecialItems.Add(book);

            int bookIndex = GetBookIndex(book.StringId);
            if (hero == Hero.MainHero)
            {
                _bookInProgress = bookIndex;
            } else
            {
                this._LTECompanions.CompanionStartReadBook(hero, bookIndex);
            }

            TextObject willReadTO = new("{=LTE00556}will read");
            LTLogger.IM(hero.FirstName.ToString() + " " + willReadTO.ToString() + " " + book.Name.ToString());

        }

        public void HeroStartReadBookFromUI(Hero hero, int bookIndex)
        {
            if (hero == null) return;
            if (bookIndex < 1 || bookIndex > _booksInMod) return;

            ItemObject book = GetBookItem(bookIndex);

            if (book == null) return;

            HeroSelectBookToRead(hero, book);
            LTUIManager.Instance.Refresh();
        }

        private string GetBookName(string StringId)
        {

            if (_bookList == null) return "ERROR: No book found";

            foreach (ItemObject book in _bookList)
            {
                if (book != null && book.StringId == StringId)
                {
                    return book.Name.ToString();
                }

            }
            return "ERROR: No book found";
        }

        public string GetBookNameByIndex(int index)
        {
            return GetBookName(GetBookStringId(index));
        }

        private string GetBookStringId(int index)
        {
            return index switch
            {
                //0 => "education_book_test",
                1 => "education_book_onehanded1",
                2 => "education_book_twohanded1",
                3 => "education_book_polearm1",
                4 => "education_book_bow1",
                5 => "education_book_crossbow1",
                6 => "education_book_throwing1",
                7 => "education_book_riding1",
                8 => "education_book_ahthletics1",
                9 => "education_book_smithing1",
                10 => "education_book_scouting1",
                11 => "education_book_tactics1",
                12 => "education_book_roguery1",
                13 => "education_book_charm1",
                14 => "education_book_leadership1",
                15 => "education_book_trade1",
                16 => "education_book_steward1",
                17 => "education_book_medicine1",
                18 => "education_book_engineering1",

                19 => "education_book_onehanded2",
                20 => "education_book_twohanded2",
                21 => "education_book_polearm2",
                22 => "education_book_bow2",
                23 => "education_book_crossbow2",
                24 => "education_book_throwing2",
                25 => "education_book_riding2",
                26 => "education_book_ahthletics2",
                27 => "education_book_smithing2",
                28 => "education_book_scouting2",
                29 => "education_book_tactics2",
                30 => "education_book_roguery2",
                31 => "education_book_charm2",
                32 => "education_book_leadership2",
                33 => "education_book_trade2",
                34 => "education_book_steward2",
                35 => "education_book_medicine2",
                36 => "education_book_engineering2",

                _ => "education_book_onehanded1",
            };
        }

        public int GetBookIndex(string stringId)
        {
            return stringId switch
            {
                //"education_book_test" => 0,
                "education_book_onehanded1" => 1,
                "education_book_twohanded1" => 2,
                "education_book_polearm1" => 3,
                "education_book_bow1" => 4,
                "education_book_crossbow1" => 5,
                "education_book_throwing1" => 6,
                "education_book_riding1" => 7,
                "education_book_ahthletics1" => 8,
                "education_book_smithing1" => 9,
                "education_book_scouting1" => 10,
                "education_book_tactics1" => 11,
                "education_book_roguery1" => 12,
                "education_book_charm1" => 13,
                "education_book_leadership1" => 14,
                "education_book_trade1" => 15,
                "education_book_steward1" => 16,
                "education_book_medicine1" => 17,
                "education_book_engineering1" => 18,

                "education_book_onehanded2" => 19,
                "education_book_twohanded2" => 20,
                "education_book_polearm2" => 21,
                "education_book_bow2" => 22,
                "education_book_crossbow2" => 23,
                "education_book_throwing2" => 24,
                "education_book_riding2" => 25,
                "education_book_ahthletics2" => 26,
                "education_book_smithing2" => 27,
                "education_book_scouting2" => 28,
                "education_book_tactics2" => 29,
                "education_book_roguery2" => 30,
                "education_book_charm2" => 31,
                "education_book_leadership2" => 32,
                "education_book_trade2" => 33,
                "education_book_steward2" => 34,
                "education_book_medicine2" => 35,
                "education_book_engineering2" => 36,

                _ => 1,
            };
        }

        public int GetMinINTToRead()
        {
            return _minINTToRead;
        }

        public float HeroCanRead(Hero hero)
        {
            if (hero == null) return 0;

            if (hero == Hero.MainHero)
            {
                return _canRead;
            } else
            {
                return this._LTECompanions.GetCompanionCanRead(hero);
            }

        }

        public int GetHeroesBookInProgress(Hero hero)
        {
            if (hero == null) return -1;

            if (hero == Hero.MainHero)
            {
                return _bookInProgress;
            } else
            {
                return this._LTECompanions.GetCompanionBookInProgress(hero);
            }

        }


        public float GetHeroesBookProgress(Hero hero, int bookIndex)
        {
            if (hero == null) return 0;
            if (bookIndex < 0 || bookIndex > _booksInMod) return 0;

            if (hero == Hero.MainHero)
            {
                return _bookProgress[bookIndex];
            } else
            {
                return this._LTECompanions.GetCompanionBookProgress(hero, bookIndex);
            }

        }


        public List<ItemObject> GetAllBooks()
        {
            List<ItemObject> bookList = new();

            for (int i = 1; i < 19; i++)
            {
                ItemObject item = GetBookItem(i);
                if (item != null) bookList.Add(item);
            }
            return bookList;
        }

        public List<ItemObject> GetAllScrolls()
        {
            List<ItemObject> scrollList = new();

            for (int i = 19; i < 37; i++)
            {
                ItemObject item = GetBookItem(i);
                if (item != null) scrollList.Add(item);
            }
            return scrollList;
        }


        public SkillObject GetSkillByBookIndex(int bookIndex)
        {
            SkillObject skill = bookIndex switch
            {
                1 => DefaultSkills.OneHanded,
                2 => DefaultSkills.TwoHanded,
                3 => DefaultSkills.Polearm,
                4 => DefaultSkills.Bow,
                5 => DefaultSkills.Crossbow,
                6 => DefaultSkills.Throwing,
                7 => DefaultSkills.Riding,
                8 => DefaultSkills.Athletics,
                9 => DefaultSkills.Crafting,
                10 => DefaultSkills.Scouting,
                11 => DefaultSkills.Tactics,
                12 => DefaultSkills.Roguery,
                13 => DefaultSkills.Charm,
                14 => DefaultSkills.Leadership,
                15 => DefaultSkills.Trade,
                16 => DefaultSkills.Steward,
                17 => DefaultSkills.Medicine,
                18 => DefaultSkills.Engineering,
                19 => DefaultSkills.OneHanded,
                20 => DefaultSkills.TwoHanded,
                21 => DefaultSkills.Polearm,
                22 => DefaultSkills.Bow,
                23 => DefaultSkills.Crossbow,
                24 => DefaultSkills.Throwing,
                25 => DefaultSkills.Riding,
                26 => DefaultSkills.Athletics,
                27 => DefaultSkills.Crafting,
                28 => DefaultSkills.Scouting,
                29 => DefaultSkills.Tactics,
                30 => DefaultSkills.Roguery,
                31 => DefaultSkills.Charm,
                32 => DefaultSkills.Leadership,
                33 => DefaultSkills.Trade,
                34 => DefaultSkills.Steward,
                35 => DefaultSkills.Medicine,
                36 => DefaultSkills.Engineering,
                _ => DefaultSkills.OneHanded,
            };
            return skill;
        }
    }

}
