using LT.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private bool ProperWeaponPartsSeller()
        {
            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null) return false;
            if (co.Occupation == Occupation.Blacksmith || co.Occupation == Occupation.Armorer || co.Occupation == Occupation.Weaponsmith) return true;
            return false;
        }


        private bool HasWeaponPartsToTrade()
        {
            if (_currentWeaponPartsInventory == null) return false;
            if (_currentWeaponPartsInventory.Count == 0) return false;
            return true;
        }


        private void OpenWeaponPartsTrade()
        {

            //RefreshWeaponPartsCurrentInventory();

            List<InquiryElement> list = new();

            GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"2\">");

            // format list
            foreach (CraftingPiece craftingPiece in _currentWeaponPartsInventory)
            {
                CraftingPiece p = craftingPiece;
                //string tradename = "{PieceType} Tier {PieceTier}   Price {PieceCost}:{NewLine}{PieceName}";

                string pieceName = p.Name.ToString();
                int maxLength = 30;
                if (pieceName.Length > maxLength) pieceName = pieceName.Substring(0, maxLength-2) + "..";

                string itemName = "{PieceName}{NewLine}Tier: {PieceTier}   {PieceCost}{GOLD_ICON}";
                itemName = itemName.Replace("{PieceType}", p.PieceType.Description()).Replace("{PieceTier}", Common.ToRoman(p.PieceTier).ToString()).Replace("{PieceName}", pieceName).Replace("{PieceCost}", this.GetWeaponPartPriceToUnlock(p).ToString()).Replace("{NewLine}", "\n");

                string itemNameTO = new TextObject(itemName, null).ToString();

                string itemHint = "{PieceName}{NewLine}[{PieceType}]   Tier: {PieceTier}   {PieceCost}{GOLD_ICON}";
                itemHint = itemHint.Replace("{PieceType}", p.PieceType.Description()).Replace("{PieceTier}", Common.ToRoman(p.PieceTier).ToString()).Replace("{PieceName}", p.Name.ToString()).Replace("{PieceCost}", this.GetWeaponPartPriceToUnlock(p).ToString()).Replace("{NewLine}", "\n");

                string hint = new TextObject(itemHint, null).ToString();
                bool activeItem = true;

                list.Add(new InquiryElement(craftingPiece, itemNameTO, new ImageIdentifier(craftingPiece, ""), activeItem, hint));

                //LTLogger.IMGreen(itemName);
            }


            MultiSelectionInquiryData data = new(new TextObject("{=LTE01404}Select what parts to learn:").ToString(), "",
            list, true, 0, 100, new TextObject("{=LTE00519}Select").ToString(), new TextObject("{=LTE00504}Leave").ToString(), (List<InquiryElement> list) => {

                // what we do with selected items
                
                int totalPrice = 0;
                
                // count total price
                foreach (InquiryElement inquiryElement in list)
                {
                    if (inquiryElement != null && inquiryElement.Identifier != null)
                    {
                        CraftingPiece? craftingPiece = inquiryElement.Identifier as CraftingPiece;
                        if (craftingPiece != null)
                        {
                            int price = GetWeaponPartPriceToUnlock(craftingPiece);
                            totalPrice += price;

                            TextObject msg = new TextObject("{=LTE01405}Selected: " + craftingPiece.Name.ToString() + ": " + price.ToString() + "{GOLD_ICON}");
                            LTLogger.IMGrey(msg.ToString());
                        }
                    }
                }

                //LTLogger.IMGreen("Total price: " + totalPrice.ToString());

                // gold check
                if (totalPrice > Hero.MainHero.Gold)
                {
                    TextObject msg = new TextObject("{=LTE01406}You don't have enough gold. Total price: " + totalPrice.ToString() + "{GOLD_ICON}");
                    LTLogger.IMTARed(msg.ToString());
                } else
                {
                    // deduct gold
                    GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -1 * totalPrice, false);

                    // all good, can purchase
                    foreach (InquiryElement inquiryElement in list)
                    {
                        if (inquiryElement != null && inquiryElement.Identifier != null)
                        {
                            CraftingPiece? craftingPiece = inquiryElement.Identifier as CraftingPiece;
                            if (craftingPiece != null)
                            {
                                // learn the part
                                this.OpenPart(craftingPiece);

                                // remove from the sale list
                                this._currentWeaponPartsInventory.Remove(craftingPiece);
                            }
                        }
                    }
                }

            }, (List<InquiryElement> list) => { }, "");

            MBInformationManager.ShowMultiSelectionInquiry(data);


            //LTLogger.IMRed("OpenWeaponPartsTrade: " + _lockedWeaponParts?.Count.ToString());
        }


        public void OpenPart(CraftingPiece selectedPiece)
        {
            ICraftingCampaignBehavior craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            MethodInfo dynMethod = craftingBehavior.GetType().GetMethod("OpenPart", BindingFlags.Instance | BindingFlags.NonPublic);

            int parameters = dynMethod.GetParameters().Count<ParameterInfo>();
            //LTLogger.IMRed("Params: " + parameters.ToString());

            if (parameters == 3)
            {
                foreach (CraftingTemplate craftingTemplate in CraftingTemplate.All)
                {
                    dynMethod.Invoke(craftingBehavior, new object[]
                    {
                        selectedPiece,
                        craftingTemplate,
                        false
                    });
                    //LTLogger.IMBlue("CraftingTemplate: " + craftingTemplate.TemplateName.ToString());
                }
            }
            else
            {
                LTLogger.IMRed("LTEducation: Unable to call OpenPart, incompatible game version!");
            }
            
        }


        public void RefreshWeaponPartsCurrentInventory()
        {
            this.RefreshLockedWeaponParts();

            List<CraftingPiece> myList = new List<CraftingPiece>();

            // list of parts that hero has enough money to purchase
            myList = (from x in this._lockedWeaponParts
                      where this.GetWeaponPartPriceToUnlock(x) < Hero.MainHero.Gold
                      select x).ToList<CraftingPiece>();

            int maxOffers = 10;

            this._currentWeaponPartsInventory = new List<CraftingPiece>();

            Random r = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < maxOffers; i++)
            {
                if (myList.Count == 0) break;

                int num = (myList.Count == 1) ? 0 : r.Next(myList.Count - 1);
                this._currentWeaponPartsInventory.Add(myList[num]);
                myList.Remove(myList[num]);
            }

            LTLogger.IMGrey("RefreshWeaponPartsCurrentInventory");
        }



        public void RefreshLockedWeaponParts()
        {
            // get locked parts
            this._lockedWeaponParts = new List<CraftingPiece>();
            this._lockedWeaponParts = (from x in CraftingPiece.All
                                where !x.IsGivenByDefault && !CraftingPieceIsOpened(x)   //!craftingBehavior.IsOpened(x)
                                select x).ToList<CraftingPiece>();          
        }

        bool CraftingPieceIsOpened(CraftingPiece piece)
        {
            //LTLogger.IMGrey(piece.Name.ToString());
            ICraftingCampaignBehavior craftingBehavior = Campaign.Current.GetCampaignBehavior<ICraftingCampaignBehavior>();
            foreach (CraftingTemplate craftingTemplate in CraftingTemplate.All)
            {
                if (craftingBehavior.IsOpened(piece, craftingTemplate)) return true;
            }
            return false;
        }


        private int GetWeaponPartPriceToUnlock(CraftingPiece p)
        {
            int priceToUnlock = 0;
            int basePrice = 1000000;

            int tier = p.PieceTier;

            Dictionary<int, int> tierToBasePrice = new Dictionary<int, int>
                {
                    { 1, 2000 },
                    { 2, 20000 },
                    { 3, 50000 },
                    { 4, 130000 },
                    { 5, 300000 }
                };

            if (tierToBasePrice.TryGetValue(tier, out int price))
            {
                basePrice = price;
            }

            int tradeSkillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Trade);
            int smithSkillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Crafting);
            int charmSkillValue = Hero.MainHero.GetSkillValue(DefaultSkills.Charm);

            if (tradeSkillValue > 300) tradeSkillValue = 300;
            if (smithSkillValue > 300) smithSkillValue = 300;
            if (charmSkillValue > 300) charmSkillValue = 300;

            float tradeDiscount = (float)tradeSkillValue / 15f;
            float smithDiscount = (float)smithSkillValue / 15f;
            float charmDiscount = (float)charmSkillValue / 15f;

            float totalDiscount = (float)tradeDiscount + (float)smithDiscount + (float)charmDiscount;
            //LTLogger.IMRed("Total discount: " + totalDiscount.ToString());

            priceToUnlock = (int)((float)basePrice / 100f * (100f - totalDiscount));

            if (!_bannerKingsActive) priceToUnlock /= 2; // lower prices if not BK

            return priceToUnlock;
        }


    }
}
