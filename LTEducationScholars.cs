using Helpers;
using LT.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private void InitScholars()
        {
            List<Settlement> settlements = Settlement.FindAll((Settlement s) => s.IsTown || s.IsCastle || s.IsVillage).ToList<Settlement>();

            //Logger.IM("Total settlements: " + settlements.Count().ToString());

            //int town = 0;
            //int castle = 0;
            //int village = 0;
            //int hideout = 0;

            //foreach (Settlement s in Campaign.Current.Settlements)
            //    //foreach (Settlement s in settlements)
            //{
            //    if (s.IsTown) town ++;
            //    if (s.IsCastle) castle++;
            //    if (s.IsVillage) village++;
            //    if (s.IsHideout) hideout++;

            //    if (!s.IsTown && !s.IsCastle && !s.IsVillage && !s.IsHideout)
            //    {
            //        Logger.IM(s.Name.ToString());
            //    }
            //}

            //Logger.IM("Total towns: " + town.ToString());
            //Logger.IM("Total castles: " + castle.ToString());
            //Logger.IM("Total villages: " + village.ToString());
            //Logger.IM("Total hideouts: " + hideout.ToString());

            //Logger.IM("Total settlements: " + Campaign.Current.Settlements.Count().ToString());

            Random rand = new();
            for (int i=0; i < _totalScholars; i++)
            {
                if (_scholarSettlements[i] == null)
                {
                    _scholarSettlements[i] = settlements[rand.Next(settlements.Count)];
                }
            }
        }


        private void MoveScholars()
        {
            Random rand = new();

            //int closestCount = 6;
            int moving = _totalScholars / 7; // move 1/7 every day randomly

            for (int i=0; i < moving; i++)
            {

                int si = rand.Next(_totalScholars);

                Settlement s = _scholarSettlements[si];
                if (s == null) continue;

                Settlement ps = s;  // old settlement for debug

                // this way after some time all scholars will move to Battania where villages are very close to each other
                //List<Settlement> closestSettlements = LHelpers.GetClosestSettlementsFromSettlement(s, closestCount);
                //if (closestSettlements == null || closestSettlements.Count() < closestCount) continue;
                //_scholarSettlements[si] = closestSettlements[rand.Next(closestCount)];

                Settlement newSettlement = SettlementHelper.FindRandomSettlement((Settlement x) => x.IsTown || x.IsVillage || x.IsCastle);
                if (newSettlement == null) continue;
                _scholarSettlements[si] = newSettlement;

                if (_debug) LTLogger.IM("Scholar ["+si.ToString()+"] " + ps.Name.ToString() + "->" + _scholarSettlements[si].Name.ToString());
            }
        }

        public int GetScholarIndexbySettlement(Settlement settlement)
        {
            for ( int i = 0; i < _totalScholars; i++)
            {
                if (_scholarSettlements[i] == settlement) return i;
            }

            return -1;
        }


        private int GetScholarPrice(int scholarIndex)
        {
            int priceInd = scholarIndex % 4;
            switch (priceInd) {
                case 0: return 250;
                case 1: return 500;
                case 2: return 1000;
                case 3: return 2000;
            }
            return 500;
        }


        private TextObject GetScholarLevel(int scholarIndex)
        {
            switch (scholarIndex % 4)
            {
                case 0: return new("{=LTE00533}novice");
                case 1: return new("{=LTE00534}intermediate");
                case 2: return new("{=LTE00535}advanced");
                case 3: return new("{=LTE00536}expert");
            }
            return new("UNDEFINED");
        }


        private string GetScholarImage(int scholarIndex)
        {
            if (scholarIndex < 0 || scholarIndex > _totalScholars - 1) return "lt_education_scholar0";
            return "lt_education_scholar" + scholarIndex.ToString();
        }


        private SkillObject GetScholarSkill(int scholarIndex)
        {
            int skillInd = scholarIndex / 4;

            switch (skillInd)
            {
                case 0: return DefaultSkills.OneHanded;
                case 1: return DefaultSkills.TwoHanded;
                case 2: return DefaultSkills.Polearm;
                case 3: return DefaultSkills.Bow;
                case 4: return DefaultSkills.Crossbow;
                case 5: return DefaultSkills.Throwing;
                case 6: return DefaultSkills.Riding;
                case 7: return DefaultSkills.Athletics;
                case 8: return DefaultSkills.Crafting;
                case 9: return DefaultSkills.Scouting;
                case 10: return DefaultSkills.Tactics;
                case 11: return DefaultSkills.Roguery;
                case 12: return DefaultSkills.Charm;
                case 13: return DefaultSkills.Leadership;
                case 14: return DefaultSkills.Trade;
                case 15: return DefaultSkills.Steward;
                case 16: return DefaultSkills.Medicine;
                case 17: return DefaultSkills.Engineering;
            }
            return DefaultSkills.OneHanded;
        }


        private bool ScholarsNearby(Settlement currentSettlement)
        {

            if (currentSettlement == null) return false;

            List<Settlement> closestSettlements = LHelpers.GetClosestSettlementsFromSettlement(currentSettlement, 20);
            if (closestSettlements.Count == 0) return false;

            TextObject answer = new TextObject("{=LTE01317}Try searching in ");

            string locations = "";

            int foundScholars = 0;
            foreach (Settlement settlement in closestSettlements)
            {
                if (GetScholarIndexbySettlement(settlement) > -1)
                {
                    locations = locations + settlement.EncyclopediaLinkWithName.ToString() + ", ";
                    foundScholars++;
                }
            }

            if (foundScholars == 0) return false;

            // fix the string
            locations = locations.Remove(locations.Length - 2);

            if (foundScholars > 1)
            {
                // change last ',' to 'and'
                int lastIndex = locations.LastIndexOf(',');
                TextObject and = new TextObject("{=LTE00000}and");
                locations = locations.Substring(0, lastIndex) + " " + and.ToString() + locations.Substring(lastIndex + 1);
            }
            
            MBTextManager.SetTextVariable("SCHOLARS_NEARBY_LOCATIONS", answer.ToString() + locations, false);

            return true;
        }


        private void PrintScholars()
        {
            for (int i = 0; i < 10; i++)
            {
                LTLogger.IM(_scholarSettlements[i].Name.ToString());
            }
        }


        private TextObject GetScholarName(int scholarIndex)
        {
            switch (scholarIndex)
            {
                case 0: return new("{=LTE01500}Lady Pokesalot");
                case 1: return new("{=LTE01501}Lady Blade Belle");
                case 2: return new("{=LTE01502}Fencing Devil");
                case 3: return new("{=LTE01503}Ironclad");
                case 4: return new("{=LTE01504}Holey Watnic");
                case 5: return new("{=LTE01505}Simmon the Brute");
                case 6: return new("{=LTE01506}Snakeface");
                case 7: return new("{=LTE01507}Blorg the Usplit");
                case 8: return new("{=LTE01508}Lady Thrustalot");
                case 9: return new("{=LTE01509}Speargertha");
                case 10: return new("{=LTE01510}Ingvar the Impaler");
                case 11: return new("{=LTE01511}Gruumsh the Grim");
                case 12: return new("{=LTE01512}Sir Robin of the Bow");
                case 13: return new("{=LTE01513}Rona Eagle Eye");
                case 14: return new("{=LTE01514}Dame Archeress");
                case 15: return new("{=LTE01515}Malik al-Qaws");
                case 16: return new("{=LTE01516}Pik the Dokson");
                case 17: return new("{=LTE01517}Malcolm the Fast");
                case 18: return new("{=LTE01518}Eve the Hunter");
                case 19: return new("{=LTE01519}John Wrongside");
                case 20: return new("{=LTE01520}Mavik Vog Kacher");
                case 21: return new("{=LTE01521}Benzur the Leftie");
                case 22: return new("{=LTE01522}Raven's Eye");
                case 23: return new("{=LTE01523}Master Li");
                case 24: return new("{=LTE01524}Lady Stirrup Struggler");
                case 25: return new("{=LTE01525}Sir Edward Equestrian");
                case 26: return new("{=LTE01526}Lady Isabella of the Iron Horse");
                case 27: return new("{=LTE01527}Black Horseman");
                case 28: return new("{=LTE01528}Goodwill Gesturo");
                case 29: return new("{=LTE01529}Lady Sprintalot");
                case 30: return new("{=LTE01530}Countess Flexington");
                case 31: return new("{=LTE01531}Ed the Rock");
                case 32: return new("{=LTE01532}John the Ironbender");
                case 33: return new("{=LTE01533}Edward Smellstone");
                case 34: return new("{=LTE01534}Henry Furnaceheart");
                case 35: return new("{=LTE01535}Nicholas Anvilborn");
                case 36: return new("{=LTE01536}Lora the Curious");
                case 37: return new("{=LTE01537}Tracker Ziggy");
                case 38: return new("{=LTE01538}Struck Swampman");
                case 39: return new("{=LTE01539}Ixtamnetov the Greenman");
                case 40: return new("{=LTE01540}Vova the Loser");
                case 41: return new("{=LTE01541}Voral the Horseface");
                case 42: return new("{=LTE01542}Gerda Teikitol");
                case 43: return new("{=LTE01543}Gen 'Behind the Puddle'");
                case 44: return new("{=LTE01544}Kasap Bloodhand");
                case 45: return new("{=LTE01545}Niko the Childthief");
                case 46: return new("{=LTE01546}Alex the Latrine Grabber");
                case 47: return new("{=LTE01547}Uskii the Liberator");
                case 48: return new("{=LTE01548}Lady Winsome");
                case 49: return new("{=LTE01549}Lady Gracealot");
                case 50: return new("{=LTE01550}Dame Charmelle");
                case 51: return new("{=LTE01551}Baroness Lovelyn");
                case 52: return new("{=LTE01552}Sir Baaalot");
                case 53: return new("{=LTE01553}Lord Xi");
                case 54: return new("{=LTE01554}Titus Valerius");
                case 55: return new("{=LTE01555}Master Greenzee");
                case 56: return new("{=LTE01556}Simon the Shrewd");
                case 57: return new("{=LTE01557}Moishe the Counter");
                case 58: return new("{=LTE01558}Jacob the Jeweler");
                case 59: return new("{=LTE01559}Mustafa the Golden Palm");
                case 60: return new("{=LTE01560}Dondon Silenius");
                case 61: return new("{=LTE01561}Petro the Mule");
                case 62: return new("{=LTE01562}Reginald de Cornhill");
                case 63: return new("{=LTE01563}Ursula the Unflappable");
                case 64: return new("{=LTE01564}Vet the Broomstick");
                case 65: return new("{=LTE01565}Madam Tonicia");
                case 66: return new("{=LTE01566}Baron Mercury");
                case 67: return new("{=LTE01567}Archduke Paracelsus Magnificus");
                case 68: return new("{=LTE01568}Hilda Geargrinder");
                case 69: return new("{=LTE01569}Jason the Builder");
                case 70: return new("{=LTE01570}Constructor Cedric");
                case 71: return new("{=LTE01571}Erik the Boatmaker");
            }
            return new("UNDEFINED");
        }

    }
}
