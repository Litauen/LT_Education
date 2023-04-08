using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;

namespace LT_Education
{
    internal class LTEducationTutelage
    {

        public static void TutelageRun()
        {

            bool debug = false;

            if (Hero.MainHero.PartyBelongedTo == null) return;  // captive

            if (debug) Logger.IMBlue("Tutelage");

            List<Hero> heroList = (from characterObject in Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopRoster()
                                    where characterObject.Character.HeroObject != null && !characterObject.Character.HeroObject.IsWounded
                                    select characterObject.Character.HeroObject
                                  ).ToList<Hero>();

            if (heroList.Count < 2) return; // empty list, no companions/all wounded

            foreach (SkillObject skill in Skills.All)
            {
                int maxSkillLevel = heroList.Max(h => h.GetSkillValue(skill));
                if (maxSkillLevel == 0) continue;

                Hero tutor = (from hero in heroList
                                where hero.GetSkillValue(skill) == maxSkillLevel
                                select hero
                             ).FirstOrDefault();
                if (tutor == null) continue;    // just in case, shit happens

                float baseExp = (float)maxSkillLevel / 10;             
                int tutorCharm = tutor.GetSkillValue(DefaultSkills.Charm);

                if (debug) Logger.IMGreen(skill.ToString() + "  BaseExp: " + baseExp + "  tutor charm: " + tutorCharm);

                // tutor charm [0..300] adds exp ~[0%....+100%]
                baseExp += (baseExp * tutorCharm / 300);
                if (debug) Logger.IMGreen("  " + tutor.FirstName.ToString() + " best in " + skill.ToString() + " [" + maxSkillLevel.ToString() + "] exp: [" + baseExp.ToString() + "]");
                if ((int)baseExp == 0) continue;

                foreach (Hero hero in heroList)
                {
                    if (hero.GetSkillValue(skill) < maxSkillLevel)  // filter out the tutor and other members with the exact same skill level
                    {

                        int relation = CharacterRelationManager.GetHeroRelation(tutor, hero);
                        if (relation < -50) continue;  // no tutoring with such bad relations

                        // relation [-50..0] drops exp [-100%..0%]
                        float expChange;
                        if (relation > 0) expChange = 0;
                        else if (relation < -50) expChange = baseExp;
                        else expChange = (baseExp / -50) * relation;

                        float heroExp = baseExp - expChange;

                        int social = hero.GetAttributeValue(DefaultCharacterAttributes.Social);
                        int intelligence = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                        heroExp = heroExp * (social + intelligence) / 20;

                        hero.AddSkillXp(skill, (int)Math.Round(heroExp, MidpointRounding.ToEven));

                        if (debug) Logger.IMBlue("    " + hero.FirstName.ToString() + " +[" + heroExp.ToString() + "/" + (int)Math.Round(heroExp, MidpointRounding.ToEven) + "] " + skill.ToString());
                        
                    }
                }
            }

        }

    }
}
