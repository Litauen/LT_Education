using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using LT.Helpers;
using LT.Logger;
using LT.UI.MapNotification;

namespace LT_Education
{
    public class LTECompanions
    {

        bool _debug = false;

        private List<LTECompanionEducationData> _companionEducationData;

        public LTECompanions(List<LTECompanionEducationData> companionEducationData) 
        {
            _companionEducationData = companionEducationData;
        }

        public void ProcessCompanionsEducation()
        {

            if (_debug) LTLogger.IMRed("ProcessCompanionsEducation");

            List<Hero> heroList = LTHelpers.GetPartyCompanionsList();
            if (heroList.Count == 0) return;

            foreach (Hero hero in heroList)
            {
                LTECompanionEducationData heroData = GetCompanionEducationData(hero);

                int heroINT = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                if (heroINT > 10) heroINT = 10;

                if (heroData.CanRead < 100)
                {

                    if (heroINT < LT_EducationBehaviour.Instance.GetMinINTToRead())
                    {
                        if (_debug) LTLogger.IMRed(hero.Name.ToString() + " too stupid to learn to read, INT: " + heroINT.ToString());
                        continue;
                    }

                    // companion learning to read
                    float progress = (heroINT - 4f) * 0.333f + 1f;
                    if (_debug) progress += 50f;
                    heroData.CanRead += progress;
                    if (heroData.CanRead >= 100)
                    {
                        heroData.CanRead = 100;

                        TextObject msg = new("{=LTE00576}{HERO_NAME} learned how to read!");
                        msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                        LTLogger.IMGreen(msg.ToString());
                       
                        Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new LTECanReadMapNotification(msg));

                        //if (hero.CharacterObject != null) 
                        //{
                        //    MBInformationManager.AddQuickInformation(msg, 0, hero.CharacterObject, "event:/ui/notification/levelup");
                        //}
                    }
                    else
                    {
                        if (_debug) LTLogger.IMRed(hero.Name.ToString() + " learning to read, progress: " + heroData.CanRead.ToString());
                    }

                }
                else
                {

                    int bookIndex = heroData.BookInProgress;

                    // not reading anything - continue
                    if (bookIndex == -1)
                    {
                        if (_debug) LTLogger.IMRed(hero.Name.ToString() + " is not reading anything");
                        continue;
                    }

                    if (bookIndex < 0 || bookIndex > 99)
                    {
                        heroData.BookInProgress = -1;
                        continue;
                    }

                    // does companion have this book that he is reading?
                    if (!LT_EducationBehaviour.Instance.HeroHasBook(hero, bookIndex))
                    {
                        if (_debug) LTLogger.IMRed(hero.Name.ToString() + " lost the book");
                        heroData.BookInProgress = -1;
                        continue;
                    }

                    if (heroData.BookProgress[bookIndex] < 100) 
                    { 
                        // actual reading process
                        float progress = (100f / (80f - (heroINT * 5f))) * 0.75f;

                        if (bookIndex > 18) progress *= 3;  // scrolls x3 faster

                        if (_debug) progress += 50f;

                        heroData.BookProgress[bookIndex] += progress;

                        if (_debug) LTLogger.IMRed(hero.Name.ToString() + " reading book [" + bookIndex.ToString() + "], progress: " + heroData.BookProgress[bookIndex].ToString());
                    }


                    // finish book/scroll
                    if (heroData.BookProgress[bookIndex] >= 100)
                    {
                        heroData.BookProgress[bookIndex] = 100;

                        if (bookIndex < 19)
                        {
                            LT_EducationBehaviour.Instance.FinishBook(hero, bookIndex);
                        }
                        else
                        {
                            LT_EducationBehaviour.Instance.FinishScroll(hero, bookIndex);
                        }

                        // return book on finish
                        LT_EducationBehaviour.Instance.HeroStopReadingAndReturnBookToParty(hero);


                        // if Auto-Read enabled
                        CompanionSelectNextBook(hero);
                    }

                }

            }

        }



        public void CompanionComesOfAge(Hero hero)
        {
            if (_debug) LTLogger.IM("CompanionComesOfAge: " + hero.Name.ToString());

            LTECompanionEducationData heroData = GetCompanionEducationData(hero);

            int heroINT = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);

            if (heroINT >= LT_EducationBehaviour.Instance.GetMinINTToRead())
            {
                heroData.CanRead = 100;
                if (_debug) LTLogger.IM(hero.Name.ToString() + " INT: " + heroINT.ToString() + " - can read!");
            }
        }


        public void CompanionSelectNextBook(Hero hero)
        {
            if (hero == Hero.MainHero) return;  // only manual for main hero
            if (GetCompanionAutoRead(hero) == 0) return;    // no auto-read enabled

            // get all the books the party has
            List<ItemObject> partyBooks = LT_EducationBehaviour.Instance.GetUniquePartyBooks(0);
            if (partyBooks.Count == 0) return;  // party has no books

            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            if (heroData.BookInProgress != -1) return;  // still reading other book

            List<ItemObject> notReadBooks = new();

            // remove the books companion already completed
            foreach (ItemObject book in partyBooks)
            {
                int bookIndex = LT_EducationBehaviour.Instance.GetBookIndex(book.StringId);
                if (heroData.BookProgress[bookIndex] < 100)
                {
                    notReadBooks.Add(book);
                }
            }
            if (notReadBooks.Count == 0) return;    // all books the party has are completed

            // select random from the remaining ones
            Random rand = new();
            ItemObject selectedBook = notReadBooks[rand.Next(notReadBooks.Count)];
            if (selectedBook == null) return;   // just in case

            LT_EducationBehaviour.Instance.HeroSelectBookToRead(hero, selectedBook);

        }


        private LTECompanionEducationData GetCompanionEducationData(Hero hero)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            if (heroData == null)
            {
                heroData = new LTECompanionEducationData(hero.Id);
                _companionEducationData.Add(heroData);
                if (_debug) LTLogger.IMRed(hero.Name.ToString() + " created");
            }
            else
            {
                //if (_debug) LTLogger.IMRed(hero.Name.ToString() + " found");
            }

            return heroData;
        }

        public int GetCompanionAutoRead(Hero hero)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            return heroData.AutoRead;
        }

        public void SetCompanionAutoRead(Hero hero, int val)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            heroData.AutoRead = val;
        }

        public float GetCompanionCanRead(Hero hero)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            return heroData.CanRead;
        }

        public int GetCompanionBookInProgress(Hero hero)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            return heroData.BookInProgress;
        }

        public float GetCompanionBookProgress(Hero hero, int bookIndex)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            return heroData.BookProgress[bookIndex];
        }

        public void CompanionStartReadBook(Hero hero, int bookIndex)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            heroData.BookInProgress = bookIndex;
        }

        public void CompanionStopReadBook(Hero hero)
        {
            LTECompanionEducationData heroData = GetCompanionEducationData(hero);
            heroData.BookInProgress = -1;
        }

    }
}
