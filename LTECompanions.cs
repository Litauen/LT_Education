using LT.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace LT_Education
{
    internal class LTECompanions
    {

        private List<LTECompanionEducationData> _companionEducationData;

        public LTECompanions(List<LTECompanionEducationData> companionEducationData) 
        {
            _companionEducationData = companionEducationData;
        }

        public void ProcessCompanionsEducation()
        {

            bool debug = false;

            if (debug) LTLogger.IMRed("ProcessCompanionsEducation");

            // get all companions from the party/ why party? clan?
            List<Hero> heroList = (from characterObject in Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopRoster()
                         where characterObject.Character.HeroObject != null && characterObject.Character.HeroObject != Hero.MainHero
                                   select characterObject.Character.HeroObject).ToList<Hero>();

            if (heroList.Count == 0) return;

            foreach (Hero hero in heroList)
            {
                LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
                if (heroData == null)
                {
                    heroData = new LTECompanionEducationData(hero.Id);
                    _companionEducationData.Add(heroData);
                    if (debug) LTLogger.IMRed(hero.Name.ToString() + " created");
                } else
                {
                    if (debug) LTLogger.IMRed(hero.Name.ToString() + " found");
                }

                int heroINT = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                if (heroINT > 10) heroINT = 10;

                if (heroData.CanRead < 100)
                {

                    if (heroINT < LT_EducationBehaviour.Instance.GetMinINTToRead())
                    {
                        if (debug) LTLogger.IMRed(hero.Name.ToString() + " too stupid to learn to read, INT: " + heroINT.ToString());
                        continue;
                    }

                    // companion learning to read
                    float progress = (heroINT - 4f) * 0.333f + 1f;
                    if (debug) progress += 50f;
                    heroData.CanRead += progress;
                    if (heroData.CanRead >= 100)
                    {
                        heroData.CanRead = 100;

                        TextObject msg = new("{=LTE00576}{HERO_NAME} learned how to read!");
                        msg.SetTextVariable("HERO_NAME", hero.Name.ToString());
                        LTLogger.IMGreen(msg.ToString());
                    }
                    else
                    {
                        if (debug) LTLogger.IMRed(hero.Name.ToString() + " learning to read, progress: " + heroData.CanRead.ToString());
                    }

                }
                else
                {

                    int bookIndex = heroData.BookInProgress;

                    // not reading anything - continue
                    if (bookIndex == -1)
                    {
                        if (debug) LTLogger.IMRed(hero.Name.ToString() + " is not reading anything");
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
                        if (debug) LTLogger.IMRed(hero.Name.ToString() + " lost the book");
                        heroData.BookInProgress = -1;
                        continue;
                    }

                    if (heroData.BookProgress[bookIndex] < 100) 
                    { 
                        // actual reading process
                        float progress = 100f / (80f - (heroINT * 5f));

                        if (bookIndex > 18) progress *= 3;  // scrolls x3 faster

                        if (debug) progress += 50f;

                        heroData.BookProgress[bookIndex] += progress;

                        if (debug) LTLogger.IMRed(hero.Name.ToString() + " reading book [" + bookIndex.ToString() + "], progress: " + heroData.BookProgress[bookIndex].ToString());
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
                    }


                }

            }

        }


        public float GetCompanionCanRead(Hero hero)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            //if (heroData == null) LTLogger.IMRed(hero.Name.ToString() + " not found");
            if (heroData == null) return 0f;
            //LTLogger.IMRed(hero.Name.ToString() + " CanRead: " + heroData.CanRead.ToString());
            return heroData.CanRead;
        }

        public int GetCompanionBookInProgress(Hero hero)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            if (heroData == null) return -1;
            return heroData.BookInProgress;
        }

        public float GetCompanionBookProgress(Hero hero, int bookIndex)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            if (heroData == null) return -1;

            return heroData.BookProgress[bookIndex];
        }

        public void CompanionStartReadBook(Hero hero, int bookIndex)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            if (heroData == null) return;

            heroData.BookInProgress = bookIndex;
        }

        public void CompanionStopReadBook(Hero hero)
        {
            LTECompanionEducationData heroData = _companionEducationData.Find((LTECompanionEducationData x) => x.Id == hero.Id && hero != Hero.MainHero);
            if (heroData == null) return;

            heroData.BookInProgress = -1;
        }

    }
}
