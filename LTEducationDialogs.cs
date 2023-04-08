using Helpers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {


        private void AddDialogs(CampaignGameStarter starter)
        {

            if (_bookList == null) return;

            {   // Tavern Keeper - Vendors
                starter.AddPlayerLine("tavernkeeper_book", "tavernkeeper_talk", "tavernkeeper_book_seller_location", "{TAVERN_KEEPER_GREETING}", TavernKeeperOnCondition, () => { GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -5, false); }, 100, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 5)
                    {
                        explanation = new TextObject("{=LTE01209}Not enough gold...");
                        return false;
                    }
                    else
                    {
                        explanation = new TextObject("5 {GOLD_ICON}");
                        return true;
                    }
                });

                starter.AddDialogLine("tavernkeeper_book_a", "tavernkeeper_book_seller_location", "tavernkeeper_books_thanks", "{=LTE01300}Yeah, saw {VENDOR.FIRSTNAME} recently. Look around the town.", () =>
                {
                    return IsBookVendorInTown();
                }, null, 100, null);

                starter.AddDialogLine("tavernkeeper_book_a", "tavernkeeper_book_seller_location", "tavernkeeper_books_thanks", "{=LTE01301}I heard you can find what you are looking for in {SETTLEMENT}.", () =>
                {
                    return IsBookVendorNearby();
                }, null, 100, null);

                starter.AddDialogLine("tavernkeeper_book_b", "tavernkeeper_book_seller_location", "tavernkeeper_books_thanks", "{=LTE01302}No, haven't heard lately.", null, null, 100, null);

                starter.AddPlayerLine("tavernkeeper_book", "tavernkeeper_books_thanks", "tavernkeeper_pretalk", "{=LTE01303}Thanks!", null, null, 100, null, null);
                starter.AddDialogLine("tavernkeeper_book", "tavernkeeper_pretalk", "tavernkeeper_talk", "{=LTE01304}Anything else?", null, null, 100, null);

            }

            {   // Tavern Keeper - Scholars
                starter.AddPlayerLine("tavernkeeper_scholars", "tavernkeeper_talk", "tavernkeeper_scholar_location", "{TAVERN_KEEPER_GREETING_SCHOLAR}", TavernKeeperOnCondition, () => { GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -15, false); }, 100, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 15)
                    {
                        explanation = new TextObject("{=LTE01209}Not enough gold...");
                        return false;
                    }
                    else
                    {
                        explanation = new TextObject("15 {GOLD_ICON}");
                        return true;
                    }
                });

                starter.AddDialogLine("tavernkeeper_scholars_a", "tavernkeeper_scholar_location", "tavernkeeper_scholars_thanks", "{SCHOLARS_NEARBY_LOCATIONS}.", () =>
                {
                    return ScholarsNearby(Settlement.CurrentSettlement);
                }, null, 100, null);

                starter.AddDialogLine("tavernkeeper_scholars_b", "tavernkeeper_scholar_location", "tavernkeeper_books_thanks", "{=LTE01302}No, haven't heard lately.", null, null, 100, null);

                starter.AddPlayerLine("tavernkeeper_scholars", "tavernkeeper_scholars_thanks", "tavernkeeper_pretalk", "{=LTE01303}Thanks!", null, null, 100, null, null);
                starter.AddPlayerLine("tavernkeeper_scholars", "tavernkeeper_scholars_thanks", "tavernkeeper_pretalk", "{=LTE01318}Great! I will mark it on my map.", null, () =>               
                {                
                    foreach (Settlement settlement in LHelpers.GetClosestSettlementsFromSettlement(Settlement.CurrentSettlement, 20))
                    {
                        if (GetScholarIndexbySettlement(settlement) > -1)
                        {
                            if (!Campaign.Current.VisualTrackerManager.CheckTracked(settlement))    // do not mark already marked settlements
                            {
                                if (_debug) Logger.IMBlue("Marking " + settlement.Name.ToString());
                                Campaign.Current.VisualTrackerManager.RegisterObject(settlement);
                            }
                        }
                    }
                }, 100, null, null);
                //starter.AddDialogLine("tavernkeeper_scholars", "tavernkeeper_pretalk", "tavernkeeper_talk", "{=LTE01304}Anything else?", null, null, 100, null);
            }



            // Book Vendor
            {
                // greeting
                starter.AddDialogLine("bookvendor_talk", "start", "bookvendor", "{=!}{VOICED_LINE}{TEXT_LINE}", BookVendorStartTalkOnCondition, null, 110, null);


                starter.AddPlayerLine("bookvendor_talk", "bookvendor", "bookvendor_yes", "{=LTE01200}I would like to see what you have", null, () => {

                    Hero vendor = CharacterObject.OneToOneConversationCharacter.HeroObject;

                    ItemRoster itemRoster2 = new();

                    // copy books from vendor to roster for selling
                    foreach (ItemObject item in GetVendorBooks(vendor))
                    {

                        //var itemType = typeof(ItemObject);
                        //var propertyInfo = AccessTools.Property(itemType, "Name");
                        //var setter = propertyInfo.GetSetMethod(true);
                        //TextObject newItemName = new(item.Name + " ***");
                        //setter.Invoke(item, new object[] { newItemName });

                        itemRoster2.Add(new ItemRosterElement(item, 1, null));
                    }

                    // remove all Education Books from Hero.SpecialItems to make sure it's clean
                    Hero.MainHero.SpecialItems.RemoveAll((ItemObject x) => x.StringId.Contains("education_book"));

                    // copy Hero (Player's) books from his roster to Hero.SpecialItems to compare after the sale and know what we bough/sold
                    List<ItemObject> playerBooks = GetPlayerBooks(_bookList);
                    foreach (ItemObject book in playerBooks)
                    {
                        Hero.MainHero.SpecialItems.Add(book);
                    }
                    //Logger.IM("H.Special Books: " + Hero.MainHero.SpecialItems.Count);

                    Town town = Settlement.CurrentSettlement.Town;

                    // Town name and gold
                    InventoryManager.OpenScreenAsTrade(itemRoster2, town, InventoryManager.InventoryCategoryType.None, new InventoryManager.DoneLogicExtrasDelegate(OnInventoryScreenDone));


                }, 110, (out TextObject explanation) =>
                {
                    if (Hero.MainHero.Gold < 100)
                    {
                        explanation = new TextObject("{=LTE01209}Not enough gold...");
                        return false;
                    }
                    else
                    {
                        explanation = TextObject.Empty;
                        return true;
                    }
                });

                starter.AddPlayerLine("bookvendor_talk", "bookvendor", "bookvendor_bye", "{=LTE01201}I don't need anything else for now. Bye.", null, null, 110, null, null);

                starter.AddDialogLine("bookvendor_talk", "bookvendor_yes", "bookvendor_yes_resp", "{=!}{VOICED_LINE}{TEXT_LINE}", () =>
                {
                    int vendorID = GetVendorID(CharacterObject.OneToOneConversationCharacter);
                    TextObject textLine = new("{=LTE01202}Pleasure doing business with you. [ib:hip][rb:positive][rf:happy]");
                    if (vendorID == 2) textLine = new("{=LTE01203}Found everything you need? [ib:hip][rb:positive][rf:happy]");
                    if (vendorID == 3) textLine = new("{=LTE01204}Satisfied? [ib:hip][rb:negative][if:angry]");
                    MBTextManager.SetTextVariable("VOICED_LINE", "{=lteducation_vendor_here_you_go}", false);
                    MBTextManager.SetTextVariable("TEXT_LINE", textLine, false);
                    return true;
                }, null, 110, null);

                starter.AddPlayerLine("bookvendor_talk", "bookvendor_yes_resp", "bookvendor_help", "{=LTE01205}Thank you!", null, null, 110, null);

                starter.AddDialogLine("bookvendor_talk", "bookvendor_help", "bookvendor", "{=!}{VOICED_LINE}{TEXT_LINE}", () =>
                {
                    int vendorID = GetVendorID(CharacterObject.OneToOneConversationCharacter);
                    TextObject textLine = new("{=LTE01206}Can I help you with anything else? [ib:confident][if:convo_calm_friendly]");
                    if (vendorID == 2) textLine = new("{=LTE01207}Anything else? [ib:confident][if:convo_calm_friendly]");
                    if (vendorID == 3) textLine = new("{=LTE01208}What else? [ib:hip][if:angry]");
                    MBTextManager.SetTextVariable("VOICED_LINE", "{=lteducation_vendor_anything_else}", false);
                    MBTextManager.SetTextVariable("TEXT_LINE", textLine, false);
                    return true;
                }, null, 110, null);

                starter.AddDialogLine("bookvendor_talk", "bookvendor_bye", "end", "{=!}{VOICED_LINE}{TEXT_LINE}{BODYFACE_LINE}", () =>
                {           
                    FormatVendorRandomByeText(GetVendorID(CharacterObject.OneToOneConversationCharacter));
                    return true;
                }, null, 110, null);
            }
        }


        // we come here after we close trade window to finalize the sale
        private void OnInventoryScreenDone()
        {
            //Logger.IMGreen("After trade");
            //Logger.IM("H.Roster books: " + GetPlayerBookAmount());
            //Logger.IM("H.Special books: " + Hero.MainHero.SpecialItems.Count);

            if (_bookList == null) return;

            Hero vendor = CharacterObject.OneToOneConversationCharacter.HeroObject;
            //Logger.IM("V.Special books: " + vendor.SpecialItems.Count);

            // H.Roster books to list
            List<ItemObject> heroRoster = GetPlayerBooks(_bookList).ToList();
            // H.Special books to list
            List<ItemObject> heroSpecial = Hero.MainHero.SpecialItems.ToList();

            List<ItemObject> iter = heroRoster.ToList(); // for iteration

            foreach (ItemObject book in iter)
            {
                if (heroSpecial.Contains(book))
                {
                    heroSpecial.Remove(book);
                    heroRoster.Remove(book);
                }
            }

            // find sold books
            //List<ItemObject> soldBooks = heroSpecial.ToList();

            // find bought books
            //List<ItemObject> boughtBooks = heroRoster.ToList();

            TextObject boughTO = new("{=LTE01210}Bought ");
            TextObject soldTO = new("{=LTE01211}Sold ");

            foreach (ItemObject book in heroRoster) Logger.IM(boughTO.ToString() + book.Name.ToString());
            foreach (ItemObject book in heroSpecial) Logger.IM(soldTO.ToString() + book.Name.ToString());

            // remove bought books from V.Special
            foreach (ItemObject book in heroRoster)
            {
                vendor.SpecialItems.Remove(book);
                //Logger.IM("Removed from v.S: " + book.Name);
            }

            // add sold books to V.Special
            foreach (ItemObject book in heroSpecial)
            {
                vendor.SpecialItems.Add(book);
            }

        }

        private bool TavernKeeperOnCondition()
        {
            //MBTextManager.SetTextVariable("TAVERN_KEEPER_GREETING", "hi", false);

            MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">", false);

            Random rand = new();

            string[] greeting_lines = new string[]
            {
                "{=LTE01305}Good sir, I was hoping to purchase a book, and I was wondering if you have seen any book vendors in the area. Have any passed through your establishment recently?",
                "{=LTE01306}Pray, kind sir, have you had any news of any book vendors traveling in the vicinity? I am in search of some new reading material.",
                "{=LTE01307}Excuse me, good sir. I am a bibliophile and I am on the lookout for a book vendor. Have any such merchants come through your establishment recently?",
                "{=LTE01308}My good man, I am in need of a book and was hoping to find a vendor nearby. Might you have seen any such tradespeople in recent days?",
                "{=LTE01309}Hail, sir. I am a lover of books and am seeking to purchase one. Would you happen to know of any book vendors that have passed through here lately?",
                "{=LTE01310}Greetings, sir. I am in quest of a book and I was wondering if you have come across any book vendors recently. I would be much obliged if you could assist me in my search.",
                "{=LTE01311}Might you be so kind as to inform me, good sir, if any book vendors have been seen in these parts recently? I am eager to add to my collection.",
                "{=LTE01312}Good day, sir. I am in need of a book, and I was hoping to find a vendor nearby. Have you heard of any such merchants traveling in the area?",
                "{=LTE01313}I hope this finds you well, sir. I am a passionate reader and I am searching for a book vendor. Have any such individuals come to your establishment in recent times?",
                "{=LTE01314}Excuse me, sir. I am a scholar and I am in need of some new reading material. Have you seen any book vendors around these parts lately?",
                "{=LTE01315}Good sir, I was hoping to expand my library, and I was wondering if you have seen any book vendors in the area recently. Any information you could provide would be greatly appreciated."
            };
            MBTextManager.SetTextVariable("TAVERN_KEEPER_GREETING", greeting_lines[rand.Next(greeting_lines.Length)], false);


            MBTextManager.SetTextVariable("TAVERN_KEEPER_GREETING_SCHOLAR", "{=LTE01316}Do you know of anyone who can help me to improve my skills?", false);

            return true;
        }


        private bool BookVendorStartTalkOnCondition()
        {

            //if (!CharacterObject.OneToOneConversationCharacter.Name.Contains("Book Vendor")) return false;

            CharacterObject co = CharacterObject.OneToOneConversationCharacter;
            if (co == null || co.OriginalCharacter == null || !co.OriginalCharacter.StringId.Contains("lt_education_book_vendor")) return false;

            //int vendorID = LTEducation.GetVendorID(CharacterObject.OneToOneConversationCharacter);
            //int vendorID = GetVendorID(co);
            //LTEducation.FormatBookVendorWelcomeRandomText(Hero.MainHero.IsFemale, vendorID);

            FormatVendorRandomWelcomeText(GetVendorID(co));

            ChangeRelationWithVendor(co.HeroObject);

            return true;
        }


        private bool IsBookVendorInTown()
        {
            //IEnumerable<Hero> vendorList = Hero.FindAll((Hero x) => x.Name.Contains("Book Vendor"));
            if (_vendorList == null) return false;
            if (_vendorList.Count<Hero>() == 0) return false;

            foreach (Hero vendor in _vendorList)
            {
                if (vendor.CurrentSettlement == Settlement.CurrentSettlement)
                {
                    StringHelpers.SetCharacterProperties("VENDOR", vendor.CharacterObject, null, false);
                    return true;
                }

            }
            return false;
        }


        private bool IsBookVendorNearby()
        {
            if (_vendorList == null) return false;
            if (_vendorList.Count<Hero>() == 0) return false;

            List<Settlement> closestSettlements = LHelpers.GetClosestTownsFromSettlement(Settlement.CurrentSettlement, 10);

            foreach (Hero vendor in _vendorList)
            {
                foreach (Settlement town in closestSettlements)
                {
                    if (vendor.CurrentSettlement == town)
                    {
                        MBTextManager.SetTextVariable("SETTLEMENT", town.EncyclopediaLinkWithName, false);
                        return true;
                    }
                }
            }
            return false;
        }




        static public void FormatVendorRandomWelcomeText(int vendorID)
        {
            Random rand = new();

            bool isFemale = Hero.MainHero.IsFemale;

            // Greeting voiced line
            string voicedLine;
            TextObject textLine = new("NOT IMPLEMENTED");
            int randLine = rand.Next(10) + 1;


            string[] eadric_lines_m = new string[]
            {
                "{=LTE01000}Good morrow, sir! It is my pleasure to welcome you to my humble stall.",
                "{=LTE01001}Greetings, my good man! Welcome to peruse the literary treasures I have to offer.",
                "{=LTE01002}Well met, my friend! I am delighted to welcome you to the finest collection of books in all the land.",
                "{=LTE01003}Hail to thee, noble sir! You are most welcome to explore the riches of my books.",
                "{=LTE01004}Welcome, good sir! It is an honor to have you peruse my selection of tomes.",
                "{=LTE01005}Greetings, kind sir! I bid you welcome to my humble stall, where knowledge and wonder await.",
                "{=LTE01006}Good day to you, my good fellow! Welcome to indulge in the knowledge and entertainment my books provide.",
                "{=LTE01007}Salutations, my esteemed patron! It is my pleasure to welcome you to my collection of fine literature.",
                "{=LTE01008}Well met, good sir! Welcome to explore the literary delights that await you.",
                "{=LTE01009}Greetings, my dear sir! I am honored to welcome you to my stall, where you'll find a wealth of knowledge and wisdom."
            };

            string[] eadric_lines_f = new string[]
            {
                "{=LTE01010}Good morrow, madam! It is my pleasure to welcome you to my humble stall.",
                "{=LTE01011}Greetings, fair lady! Welcome to peruse the literary treasures I have to offer.",
                "{=LTE01012}Well met, my lady! I am delighted to welcome you to the finest collection of books in all the land.",
                "{=LTE01013}Hail to thee, noble madam! You are most welcome to explore the riches of my books.",
                "{=LTE01014}Welcome, fair maiden! It is an honor to have you peruse my selection of tomes.",
                "{=LTE01015}Greetings, kind lady! I bid you welcome to my humble stall, where knowledge and wonder await.",
                "{=LTE01016}Good day to you, my dear lady! Welcome to indulge in the knowledge and entertainment my books provide.",
                "{=LTE01017}Salutations, my esteemed patroness! It is my pleasure to welcome you to my collection of fine literature.",
                "{=LTE01018}Well met, fair damsel! Welcome to explore the literary delights that await you.",
                "{=LTE01019}Greetings, my dear lady! I am honored to welcome you to my stall, where you'll find a wealth of knowledge and wisdom."
            };


            string[] ingeborg_lines_m = new string[]
            {
                "{=LTE01020}Welcome, good sir! May I interest you in a book on chivalry? I believe it would be a perfect match for a knight as handsome as yourself.",
                "{=LTE01021}Greetings, my lord! Your presence here has made my day brighter than the sun itself.",
                "{=LTE01022}My good sir, it seems as though the stars have aligned today, for I have the pleasure of meeting a man as gallant as yourself.",
                "{=LTE01023}Blessed be the day I set eyes upon you, my lord. How may I be of service to you today?",
                "{=LTE01024}Good day, kind sir! If only the pages of my books could be as charming as your smile.",
                "{=LTE01025}What a pleasure it is to see a man of such taste and refinement browsing my humble collection of literature.",
                "{=LTE01026}You must be a scholar, my lord, for only a man of great intellect could appreciate the beauty of my books as much as you do.",
                "{=LTE01027}It is said that knowledge is power, but I would argue that the real power lies in the company of a handsome gentleman such as yourself.",
                "{=LTE01028}I must confess, my lord, that I have been waiting for a man of your caliber to visit my humble bookshop for some time now.",
                "{=LTE01029}You must be a man of great discernment to have found your way to my little corner of the world. Might I suggest a volume on the art of courtly love to match your own romantic nature?"
            };

            string[] ingeborg_lines_f = new string[]
            {
                "{=LTE01030}Welcome, fair lady! You grace my humble bookshop with your beauty and charm.",
                "{=LTE01031}It is an honor to serve a woman of such refinement and grace. Might I recommend a volume on the art of courtly love to match your own romantic nature?",
                "{=LTE01032}My lady, I cannot help but be struck by your radiance. Please allow me to assist you in finding the perfect book to match your brilliance.",
                "{=LTE01033}It is rare to find a woman with such discerning taste as yourself. I am eager to show you the treasures within my collection.",
                "{=LTE01034}What a delight it is to see a woman of your stature in my bookshop! Allow me to introduce you to some of the finest literature in the land.",
                "{=LTE01035}My lady, your presence here is like a breath of fresh air. Might I interest you in a book on poetry to match your own lyrical beauty?",
                "{=LTE01036}I have never seen a woman with such elegance and grace as yourself. I would be honored to assist you in finding the perfect volume to match your sophistication.",
                "{=LTE01037}It is clear that you are a woman of great intellect and wisdom. Might I suggest a tome on philosophy or theology to match your own sharp mind?",
                "{=LTE01038}My lady, I cannot help but be captivated by your charm and charisma. Please allow me to be your guide in this literary journey.",
                "{=LTE01039}What a pleasure it is to see a woman of your beauty and intelligence in my humble bookshop. Might I suggest a volume on history to match your own knowledge and wisdom?"
            };

            string[] ahsan_lines_m = new string[]
            {
                "{=LTE01040}What do you want, knave? Speak quickly, for I have better things to do than listen to your prattle.",
                "{=LTE01041}Who disturbs me from my rest? State your business or be gone, lest I set my dogs upon you.",
                "{=LTE01042}What brings you to my humble establishment, wretch? If it is coin you seek, know that I demand payment up front.",
                "{=LTE01043}Speak plainly, for I have no patience for those who waste my time with idle chatter. What do you desire?",
                "{=LTE01044}What is it you seek, traveler? Speak quickly, for my temper grows short with every passing moment.",
                "{=LTE01045}Greetings, stranger. What business have you with me? Make it brief, for I am not in the mood for company.",
                "{=LTE01046}Who dares disturb my peace? Speak your mind or leave me to my thoughts.",
                "{=LTE01047}What brings you to my doorstep, fool? State your business, or I shall have my guards remove you.",
                "{=LTE01048}Well met, traveler. State your purpose or be on your way. I have no patience for aimless wanderers.",
                "{=LTE01049}What do you want, peasant? I am not in the mood for pleasantries. Speak quickly and be on your way."
            };

            string[] ahsan_lines_f = new string[]
            {
                "{=LTE01050}What do you want, wench? Speak quickly, for I have no time for idle chatter.",
                "{=LTE01051}Who are you to disturb me? State your business or face the consequences.",
                "{=LTE01052}What brings you to my doorstep, maiden? If it is coin you seek, be prepared to pay a fair price.",
                "{=LTE01053}Speak plainly, for I have no patience for those who prattle on. What is it you desire?",
                "{=LTE01054}What do you seek, woman? Speak quickly, for I have more important matters to attend to.",
                "{=LTE01055}Greetings, fair lady. What business have you with me? Make it brief, for my time is precious.",
                "{=LTE01056}Who dares to interrupt my solitude? Speak your mind or be on your way.",
                "{=LTE01057}What brings you here, girl? State your purpose or risk facing my wrath.",
                "{=LTE01058}Well met, traveler. What is it that you seek? Speak quickly and be on your way.",
                "{=LTE01059}What do you want, harlot? I have no time for your games. State your business and be gone."
            };


            if (vendorID == 1)
            {
                if (!isFemale) textLine = new(eadric_lines_m[randLine - 1]); else textLine = new(eadric_lines_f[randLine - 1]);
            }

            if (vendorID == 2)
            {
                if (!isFemale) textLine = new(ingeborg_lines_m[randLine - 1]); else textLine = new(ingeborg_lines_f[randLine - 1]);
            }

            if (vendorID == 3)
            {
                if (!isFemale) textLine = new(ahsan_lines_m[randLine - 1]); else textLine = new(ahsan_lines_f[randLine - 1]);
            }



            if (!isFemale)
            {
                voicedLine = "{=lteducation_vendor_welcome_m" + randLine.ToString() + "}";
            }
            else
            {
                voicedLine = "{=lteducation_vendor_welcome_f" + randLine.ToString() + "}";
            }

            //voicedLine = "{=lteducation_promo} PROMO TEXT";

            MBTextManager.SetTextVariable("VOICED_LINE", voicedLine, false);
            MBTextManager.SetTextVariable("TEXT_LINE", textLine, false);

        }


        static public void FormatVendorRandomByeText(int vendorID)
        {
            Random rand = new();

            bool isFemale = Hero.MainHero.IsFemale;

            // Bye voiced line
            string voicedLine;
            TextObject textLine = new("NOT IMPLEMENTED");
            TextObject bodyFace = TextObject.Empty;
            int randLine = rand.Next(10) + 1;

            string[] eadric_lines_m = new string[]
            {
                "{=LTE01060}Farewell, good sir! I thank you for gracing my stall with your presence.",
                "{=LTE01061}Adieu, my friend! May the knowledge you've acquired from my books enrich your life.",
                "{=LTE01062}Farewell, my dear sir! It was a pleasure to assist you in your quest for knowledge.",
                "{=LTE01063}Godspeed, noble sir! I bid you farewell and hope to see you again soon.",
                "{=LTE01064}Farewell, my good man! I thank you for your patronage and bid you a safe journey.",
                "{=LTE01065}May the blessings of the Almighty be upon you, my friend! Farewell, and happy reading.",
                "{=LTE01066}Fare thee well, kind sir! I hope the books you've acquired bring you much joy and enlightenment.",
                "{=LTE01067}May your thirst for knowledge never be quenched, my dear sir! Farewell, and may you find what you seek.",
                "{=LTE01068}Farewell, my esteemed patron! It was an honor to serve you and share my love of books with you.",
                "{=LTE01069}Goodbye, my dear sir! I bid you farewell with the hope that my books will bring you many hours of joy and learning."
            };

            string[] eadric_lines_f = new string[]
            {
                "{=LTE01070}Farewell, fair lady! I thank you for gracing my stall with your presence.",
                "{=LTE01071}Adieu, my dear madam! May the knowledge you've acquired from my books enrich your life.",
                "{=LTE01072}Farewell, my esteemed patroness! It was a pleasure to assist you in your quest for knowledge.",
                "{=LTE01073}Godspeed, noble lady! I bid you farewell and hope to see you again soon.",
                "{=LTE01074}Farewell, my kind lady! I thank you for your patronage and bid you a safe journey.",
                "{=LTE01075}May the blessings of the Almighty be upon you, my dear lady! Farewell, and happy reading.",
                "{=LTE01076}Fare thee well, fair maiden! I hope the books you've acquired bring you much joy and enlightenment.",
                "{=LTE01077}May your thirst for knowledge never be quenched, my dear lady! Farewell, and may you find what you seek.",
                "{=LTE01078}Farewell, my dear lady! It was an honor to serve you and share my love of books with you.",
                "{=LTE01079}Goodbye, my esteemed patroness! I bid you farewell with the hope that my books will bring you many hours of joy and learning."
            };


            string[] ingeborg_lines_m = new string[]
            {
                "{=LTE01080}Farewell, good sir! I hope you will visit me again soon, so that we may continue our discussions of literature and romance.",
                "{=LTE01081}Until we meet again, my lord. May your heart be as full of joy as your mind is full of knowledge.",
                "{=LTE01082}It has been a pleasure serving you, my gallant knight. I shall look forward to the day when you return to my humble bookshop.",
                "{=LTE01083}I bid you adieu, my lord. May your journey be as bright and fulfilling as the pages of the books you so avidly peruse.",
                "{=LTE01084}Until next time, my good sir. May the knowledge you have gleaned from my books serve you well in all your endeavors.",
                "{=LTE01085}Farewell, my lord. Remember that my bookshop is always open to a man of your taste and refinement.",
                "{=LTE01086}It has been an honor serving a man of your stature and grace. May your travels be safe and your mind be ever expanded by the words within my books.",
                "{=LTE01087}Until we meet again, my lord. May the beauty and wisdom within my books be a constant source of inspiration for you.",
                "{=LTE01088}I shall miss your charming company, my dear sir. May the lessons you have learned within my books serve you well on your journey through life.",
                "{=LTE01089}Farewell, my lord. Know that the memory of your presence here shall linger within my heart like the fragrance of the roses in my garden."
            };

            string[] ingeborg_lines_f = new string[]
            {
                "{=LTE01090}Farewell, fair lady! May the pages of the books you have selected be as enlightening and beautiful as your own countenance.",
                "{=LTE01091}Until we meet again, my lady. May the knowledge you have gained within my books serve to further enhance your already sparkling intellect.",
                "{=LTE01092}It has been a pleasure serving a woman of such elegance and grace. May the journey of life bring you as much joy as your presence here has brought me.",
                "{=LTE01093}I bid you adieu, my lady. May the romance and passion within my books inspire you to ever greater heights of love and devotion.",
                "{=LTE01094}Until next time, my dear lady. May the words within my books continue to kindle the fires of your own creativity and inspiration.",
                "{=LTE01095}Farewell, fair maiden. Remember that my bookshop is always open to a woman of your beauty and discerning taste.",
                "{=LTE01096}It has been an honor serving you, my lady. May the lessons within my books guide you on your journey through life with ever greater wisdom and insight.",
                "{=LTE01097}Until we meet again, my lady. May the knowledge and beauty within my books serve as a constant source of inspiration for your own life and endeavors.",
                "{=LTE01098}I shall miss your charming company, my dear lady. May the memories of our conversations and the knowledge within my books be a constant comfort to you.",
                "{=LTE01099}Farewell, my lady. Know that the beauty and radiance of your presence here shall remain within my heart like the glow of the stars in the night sky..."
            };

            string[] ahsan_lines_m = new string[]
            {
                "{=LTE01100}Depart swiftly from my sight, you pestilent rat!",
                "{=LTE01101}Away with you, you foul and contemptuous knave!",
                "{=LTE01102}May the devil take you quickly, and swiftly at that!",
                "{=LTE01103}I bid you farewell, but do not return to my establishment!",
                "{=LTE01104}Leave at once, and may you never darken my doorway again!",
                "{=LTE01105}Take your leave without delay, before I take my own.",
                "{=LTE01106}Your company has grown increasingly tiresome, away with you, and quickly!",
                "{=LTE01107}Be gone from my sight, before I summon the guards to remove you!",
                "{=LTE01108}May the road you travel be long and arduous for you, and may you not find rest!",
                "{=LTE01109}I care not where you go, so long as it is far away from my presence!"
            };

            // same as for male
            //string[] ahsan_lines_f = new string[]
            //{
            //    "Depart swiftly from my sight, you pestilent rat!",
            //    "Away with you, you foul and contemptuous knave!",
            //    "May the devil take you quickly, and swiftly at that!",
            //    "I bid you farewell, but do not return to my establishment!",
            //    "Leave at once, and may you never darken my doorway again!",
            //    "Take your leave without delay, before I take my own.",
            //    "Your company has grown increasingly tiresome, away with you, and quickly!",
            //    "Be gone from my sight, before I summon the guards to remove you!",
            //    "May the road you travel be long and arduous for you, and may you not find rest!",
            //    "I care not where you go, so long as it is far away from my presence!"
            //};

            if (vendorID == 1)
            {
                if (!isFemale) textLine = new(eadric_lines_m[randLine - 1]); else textLine = new(eadric_lines_f[randLine - 1]);
                bodyFace = new("[ib:demure][if:convo_bemused]");
            }

            if (vendorID == 2)
            {
                if (!isFemale) textLine = new(ingeborg_lines_m[randLine - 1]); else textLine = new(ingeborg_lines_f[randLine - 1]);
                bodyFace = new("[ib:demure][if:convo_bemused]");
            }

            if (vendorID == 3)
            {
                //if (!isFemale) textLine = ahsan_lines_m[randLine - 1]; else textLine = ahsan_lines_f[randLine - 1];
                textLine = new(ahsan_lines_m[randLine - 1]);
                bodyFace = new("[ib:hip][if:angry]");
            }


            if (!isFemale)
            {
                voicedLine = "{=lteducation_vendor_bye_m" + randLine.ToString() + "}";
            }
            else
            {
                voicedLine = "{=lteducation_vendor_bye_f" + randLine.ToString() + "}";
            }
            MBTextManager.SetTextVariable("VOICED_LINE", voicedLine, false);
            MBTextManager.SetTextVariable("TEXT_LINE", textLine, false);
            MBTextManager.SetTextVariable("BODYFACE_LINE", bodyFace, false);

        }




    }
}
