using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using LT.UI;
using LT.Logger;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private void AddGameMenus(CampaignGameStarter starter)
        {

            if (_bookList == null) return;

            // ------- Learn to read menu town --------

            starter.AddGameMenuOption("education_menu", "education_learn_to_read_menu_option", "{=LTE00500}Learn to read",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return true;
                return false;
            },
            delegate (MenuCallbackArgs args)
            {
                string settlement = "village";
                if (Settlement.CurrentSettlement.IsTown)
                {
                    settlement = "town";
                } else if (Settlement.CurrentSettlement.IsCastle)
                {
                    settlement = "castle";
                }

                GameMenu.SwitchToMenu("education_learn_read_menu_" + settlement);
            }, false, 9, false);

            starter.AddGameMenu("education_learn_read_menu_town", "{MENU_TEXT}",
             (MenuCallbackArgs args) => {

                 if (Hero.MainHero.IsFemale || _debug) { args.MenuContext.SetBackgroundMeshName("book_menu_sprite5"); }
                 else { args.MenuContext.SetBackgroundMeshName("book_menu_sprite4"); }

                 int intelect = Hero.MainHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                 if (intelect < _minINTToRead)
                 {
                     MBTextManager.SetTextVariable("MIN_INT", _minINTToRead.ToString(), false);
                     MBTextManager.SetTextVariable("MENU_TEXT", "{=LTE00501}Nobody wants to teach you how to read. They think it's hopeless... (INT < {MIN_INT})", false);
                 }
                 else
                 {
                     MBTextManager.SetTextVariable("READ_PRICE", _readPrice.ToString(), false);
                     MBTextManager.SetTextVariable("MENU_TEXT", "{=LTE00502}You found a scholar who agreed to teach you how to read for {READ_PRICE}{GOLD_ICON} per hour.", false);
                 }
             }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_learn_read_menu_town", "teach", "{=LTE00503}Let's do this!", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.WaitQuest;
                if (Hero.MainHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence) < _minINTToRead) return false;

                if (Hero.MainHero.Gold < _readPrice)
                {
                    args.IsEnabled = false;
                    args.Tooltip = new TextObject("{=LTE01209}Not enough gold...", null);
                }

                return true;
            }, (MenuCallbackArgs args) =>
            {
                _lastHourOfLearning = -10; // reset
                GameMenu.SwitchToMenu("education_learn_read_menu_town_progress");
            }, true);


            starter.AddGameMenuOption("education_learn_read_menu_town", "leave", "{LEAVE_MENU_TEXT}", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;

                int intelect = Hero.MainHero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                if (intelect < _minINTToRead) { MBTextManager.SetTextVariable("LEAVE_MENU_TEXT", "{=LTE00504}Leave", false); }
                else { MBTextManager.SetTextVariable("LEAVE_MENU_TEXT", "{=LTE00505}Maybe next time...", false); }

                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("education_menu"); }, true);





            // ------ learn to read -------

            starter.AddWaitGameMenu("education_learn_read_menu_town_progress", "{=LTE00506}The scholar begins by introducing the concept of the alphabet and the sounds each letter makes. He moves on to basic words and teaches how to pronounce them, as well as how to read them in context. The scholar emphasizes the importance of pronunciation and enunciation to accurately read and understand texts.",
            delegate (MenuCallbackArgs args)
            {
                if (Hero.MainHero.IsFemale || _debug) { args.MenuContext.SetBackgroundMeshName("book_menu_sprite7"); }
                else { args.MenuContext.SetBackgroundMeshName("book_menu_sprite6"); }

                args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(100 / LearningToReadPerHourProgress(), (float)_canRead / 100);
            }, delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                this.OnLearningEnd();
            }, delegate (MenuCallbackArgs args, CampaignTime dt) //OnTickDelegate
            {

                int hour = (int)CampaignTime.Now.CurrentHourInDay;
                if (_lastHourOfLearning != -10 && _lastHourOfLearning != hour)
                {
                    Hero.MainHero.ChangeHeroGold(_readPrice * -1); // without sound

                    GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");

                    MBTextManager.SetTextVariable("READ_PAID", _readPrice.ToString(), false);
                    TextObject msg = new("{=LTE00507}Paid {READ_PAID}{GOLD_ICON}");
                    LTLogger.IMGrey(msg.ToString());

                    // learn to read
                    _canRead += LearningToReadPerHourProgress();
                    if (_debug) _canRead += 10;
                }
                _lastHourOfLearning = hour;     // remember last hour the hero was charged

                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu((float)_canRead / 100);

                // control if we can pay for another hour
                if (Hero.MainHero.Gold < _readPrice)
                {
                    LTLogger.IMRed("{=LTE00508}Not enough gold to continue learning...");
                    args.MenuContext.GameMenu.EndWait();
                    GameMenu.ExitToLast();
                }

            },
            GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);


            starter.AddGameMenuOption("education_learn_read_menu_town_progress", "leave", "{=LTE00509}Stop for now", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, -1, false);




            //// -- learn to read village --
            //starter.AddGameMenuOption("village", "education_learn_read_menu_village", "{=LTE00500}Learn to read",
            //(MenuCallbackArgs args) => {
            //    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            //    if (_canRead < 100) return true;
            //    return false;
            //},
            //delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("education_learn_read_menu_village"); }, false, 4, false);

            starter.AddGameMenu("education_learn_read_menu_village", "{=LTE00510}Local villagers look confused. After a short talk between themselves, the brightest of them points towards the town...",
             (MenuCallbackArgs args) => {
                 args.MenuContext.SetBackgroundMeshName("book_menu_sprite8");
             }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_learn_read_menu_village", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("education_menu"); }, true);


            // -- learn to read castle --
            //starter.AddGameMenuOption("castle", "education_learn_read_menu_castle", "{=LTE00500}Learn to read",
            //(MenuCallbackArgs args) => {
            //    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
            //    if (_canRead < 100) return true;
            //    return false;
            //},
            //delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("education_learn_read_menu_castle"); }, false, 5, false);

            starter.AddGameMenu("education_learn_read_menu_castle", "{=LTE00511}Locals look puzzled. After scratching their heads they suggest you should go to the town...",
             (MenuCallbackArgs args) => {
                 args.MenuContext.SetBackgroundMeshName("book_menu_sprite9");
             }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_learn_read_menu_castle", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("education_menu"); }, true);



            // --------- Manage your education menu -----------

            starter.AddGameMenuOption("town", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                //if (_canRead < 100) return false;
                //return GetPlayerBookAmount(_bookList) > 0;
                return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 4, false, null);


            // ----------------- test popup menu ------------------
            if (_debug)
            {
                starter.AddGameMenuOption("town", "test_popup", "Test popup",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Default;
                    return true;
                },
                delegate (MenuCallbackArgs args) { 
                    //CreateStatsPageVMLayer("Education", "", "You are awesome!", "You smart-ass skill increased by 10000!", "lt_education_book17", "{=LTE00530}Continue");
                    LTUIManager.Instance.ShowWindow("TestPopup");
                }, false, 9, false);
            }



            // ---------------------------------------------------------------------------------

            starter.AddGameMenuOption("village", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                //if (_canRead < 100) return false;
                //return GetPlayerBookAmount(_bookList) > 0;
                return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 1, false);


            starter.AddGameMenuOption("castle", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                //if (_canRead < 100) return false;
                //return GetPlayerBookAmount(_bookList) > 0;
                return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 4, false);


            starter.AddGameMenu("education_menu", "{CURRENTLY_READING}",
            (MenuCallbackArgs args) => {

                this._readingInMenu = false;
                this._inTraining = false;

                //if (PlayerEncounter.EncounterSettlement.IsCastle)
                if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsCastle)
                {
                    args.MenuContext.SetBackgroundMeshName("book_menu_sprite3");
                }
                else
                //if (PlayerEncounter.EncounterSettlement.IsTown)
                if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown)
                {
                    args.MenuContext.SetBackgroundMeshName("book_menu_sprite1");
                }
                else
                {
                    args.MenuContext.SetBackgroundMeshName("book_menu_sprite2");
                }

                //args.MenuContext.SetPanelSound("event:/ui/panels/settlement_village");
                //args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/village");

                // let's check if player still has the book he was reading, maybe he dropped it or sold it?
                //if (!PlayerHasBook(_bookInProgress)) _bookInProgress = -1;
                if (!HeroHasBook(Hero.MainHero, _bookInProgress)) _bookInProgress = -1;

                if (_canRead < 100)
                {
                    MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00555}You can't read...", false);
                }
                else
                {
                    if (_bookInProgress != -1)
                    {
                        int progress = (int)_bookProgress[_bookInProgress];
                        MBTextManager.SetTextVariable("READING_DATA", GetBookNameByIndex(_bookInProgress) + " [" + progress + "%]", false);
                        MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00513}Currently reading: \n\n{READING_DATA}", false);
                    }
                    else
                    {
                        MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00514}You are not reading anything currently.", false);
                    }
                }

            }, GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_menu", "start_reading", "{=LTE00532}Start reading", (MenuCallbackArgs args) =>
            {
                if (_bookInProgress < 0) return false;
                args.optionLeaveType = GameMenuOption.LeaveType.WaitQuest;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                GameMenu.SwitchToMenu("education_reading_menu");
            }, false, -1, false);


            starter.AddGameMenuOption("education_menu", "select book", "{=LTE00515}Select what to read",
                (MenuCallbackArgs args) => {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    return GetPartyBookAmount(_bookList) > 0;
                    //return true;
                },
            delegate (MenuCallbackArgs args)
            {
                List<InquiryElement> list = new();

                foreach (ItemObject book in GetPartyBooks())
                {
                    string hint = new TextObject("{=LTE00516}This looks like a good book to read", null).ToString();
                    bool activeItem = true;
                    string itemName = book.Name.ToString();
                    int bookProgress = (int)_bookProgress[GetBookIndex(book.StringId)];
                    if (bookProgress > 0)
                    {
                        if (bookProgress < 100)
                        {
                            itemName += " [" + bookProgress + "%]";
                        } else
                        {
                            activeItem = false;
                            hint = new TextObject("{=LTE00517}You have already read this book.").ToString();
                        }
                    }
                    list.Add(new InquiryElement(book, itemName, new ImageIdentifier(book), activeItem, hint));
                }


                MultiSelectionInquiryData data = new(new TextObject("{=LTE00518}From now on, you will read:").ToString(), "",
                list, true, 0, 1, new TextObject("{=LTE00519}Select").ToString(), new TextObject("{=LTE00504}Leave").ToString(), (List<InquiryElement> list) => {
                    // what we do with selected book?
                    foreach (InquiryElement inquiryElement in list)
                    {
                        if (inquiryElement != null && inquiryElement.Identifier != null)
                        {
                            ItemObject? book = inquiryElement.Identifier as ItemObject;
                            if (book != null)
                            {

                                HeroSelectBookToRead(Hero.MainHero, book);

                                //TextObject willReadTO = new("{=LTE00520}Will read ");
                                //Logger.IM(willReadTO.ToString() + book.Name.ToString());
                                //_bookInProgress = GetBookIndex(book.StringId);

                                GameMenu.SwitchToMenu("education_menu");
                            }
                        }
                    }

                }, (List<InquiryElement> list) => { }, "");
                MBInformationManager.ShowMultiSelectionInquiry(data);
            }, false, -1, false);

            starter.AddGameMenuOption("education_menu", "stop_reading", "{=LTE00521}Decide not to read anything", (MenuCallbackArgs args) =>
            {
                if (_bookInProgress < 0) return false;
                args.optionLeaveType = GameMenuOption.LeaveType.Surrender;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                //_bookInProgress = -1;
                HeroStopReadingAndReturnBookToParty(Hero.MainHero);

                GameMenu.SwitchToMenu("education_menu");
            }, false, -1, false);



            starter.AddGameMenuOption("education_menu", "book_stash", "{=LTE00569}Book Stash",
            (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                return true;
            },
            delegate (MenuCallbackArgs args) {
                SoundEvent.PlaySound2D("event:/ui/notification/quest_start");
                LTUIManager.Instance.ShowWindow("BookStash", "education_menu");
            }, false, -1, false);



            starter.AddGameMenuOption("education_menu", "scholars_entry", "{SCHOLARS_MENU_ENTRY_TEXT}", (MenuCallbackArgs args) =>
            {

                int i = GetScholarIndexbySettlement(Settlement.CurrentSettlement);
                if (i > -1)
                {
                    MBTextManager.SetTextVariable("SCHOLARS_MENU_ENTRY_TEXT", "{=LTE00537}Meet the scholar", false);
                } else
                {
                    MBTextManager.SetTextVariable("SCHOLARS_MENU_ENTRY_TEXT", "{=LTE00538}No scholars here...", false);
                    args.Tooltip = new TextObject("{=LTE00539}Propably Tavern Keeper could help...", null);
                    args.IsEnabled = false;                  
                }                             
                args.optionLeaveType = GameMenuOption.LeaveType.ManageGarrison;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                GameMenu.SwitchToMenu("scholar_menu");
            }, false, -1, false);


            starter.AddGameMenuOption("education_menu", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                if (PlayerEncounter.EncounterSettlement.IsCastle)
                {
                    GameMenu.SwitchToMenu("castle");
                    return;
                }
                if (PlayerEncounter.EncounterSettlement.IsTown)
                {
                    GameMenu.SwitchToMenu("town");
                    return;
                }
                GameMenu.SwitchToMenu("village");

            }, true);


            // ----- read book menu --------


            starter.AddWaitGameMenu("education_reading_menu", "{CURRENTLY_READING}",
            delegate (MenuCallbackArgs args)
            {
                MBTextManager.SetTextVariable("READING_DATA", GetBookNameByIndex(_bookInProgress), false);
                MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00525}Reading {READING_DATA}", false);

                _readingInMenu = true;

                // menu sprites:
                // 10-11-12 male castle
                // 13-14-15 male village
                // 16-17-18 male town
                // 19-20-21 female town
                // 22-23-24 female castle
                // 25-26-27 female village
                int menuIndex;
                if (Hero.MainHero.IsFemale || _debug)
                {
                    if (MobileParty.MainParty.CurrentSettlement.IsTown) { menuIndex = 19; }
                    else if (MobileParty.MainParty.CurrentSettlement.IsCastle) { menuIndex = 22; }
                    else { menuIndex = 25; }
                }
                else
                {
                    if (MobileParty.MainParty.CurrentSettlement.IsTown) { menuIndex = 16; }
                    else if (MobileParty.MainParty.CurrentSettlement.IsCastle) { menuIndex = 10; }
                    else { menuIndex = 13; }
                }
                Random rand = new();
                menuIndex += rand.Next(3);

                args.MenuContext.SetBackgroundMeshName("book_menu_sprite"+menuIndex.ToString());

                args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress(100, (float)_bookProgress[_bookInProgress] / 100);
            }, delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                GameMenu.ExitToLast();
            }, delegate (MenuCallbackArgs args, CampaignTime dt) //OnTickDelegate
            {
                float progress = 1;
                if (_bookInProgress > 0 && _bookInProgress < _booksInMod + 1) progress = (float)_bookProgress[_bookInProgress] / 100;
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu(progress);
            },
            GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);



            starter.AddGameMenuOption("education_reading_menu", "stop_reading", "{=LTE00509}Stop for now", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("education_menu"); }, true);





            // ---------------- scholar menu --------------


            starter.AddGameMenu("scholar_menu", "{=LTE00540}{SCHOLAR_NAME}\noffers training in {SKILL_NAME} ({SCHOLAR_LEVEL})\nPrice: {SCHOLAR_PRICE}{GOLD_ICON}/h  Duration: {TRAINING_DURATION}h",
            (MenuCallbackArgs args) => {

                int scholarIndex = GetScholarIndexbySettlement(Settlement.CurrentSettlement);
                MBTextManager.SetTextVariable("SCHOLAR_NAME", GetScholarName(scholarIndex).ToString(), false);               
                MBTextManager.SetTextVariable("SKILL_NAME", GetScholarSkill(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("SCHOLAR_LEVEL", GetScholarLevel(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("SCHOLAR_PRICE", GetScholarPrice(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("TRAINING_DURATION", _trainingDuration.ToString(), false);

                this._trainingScholarIndex = scholarIndex;

                args.MenuContext.SetBackgroundMeshName(GetScholarImage(scholarIndex));

                //Logger.IM(scholarIndex.ToString());

            }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);


            starter.AddGameMenuOption("scholar_menu", "scholar_proceed", "{=LTE00541}Who will participate?",
                (MenuCallbackArgs args) => {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;

                    int scholarIndex = GetScholarIndexbySettlement(Settlement.CurrentSettlement);

                    if (Hero.MainHero.Gold < GetScholarPrice(scholarIndex) * this._trainingDuration)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=LTE01209}Not enough gold...", null);
                    }

                    if (this._startTimeOfTraining.ElapsedHoursUntilNow < this._trainingRest + this._trainingDuration)
                    {
                        args.IsEnabled = false;
                        args.Tooltip = new TextObject("{=LTE00542}Recently trained, need to rest", null);
                    }

                    return true;
                },
            delegate (MenuCallbackArgs args)
            {
                List<InquiryElement> list = FormatHeroInquiryList(true);


                MultiSelectionInquiryData data = new(new TextObject("{=LTE00543}Select who will train").ToString(), "",
                list, true, 0, 1000, new TextObject("{=LTE00519}Select").ToString(), new TextObject("{=LTE00504}Leave").ToString(), (List<InquiryElement> list) => {

                    List<Hero> heroList = new();

                    foreach (InquiryElement inquiryElement in list)
                    {

                        if (inquiryElement != null)
                        {
                            if (inquiryElement.Identifier == null)
                            {
                                //Logger.IMRed("All selected!");
                                heroList = FormatHeroList();
                                break;
                            }
                            else
                            {
                                if (inquiryElement.Identifier is Hero hero)
                                {
                                    //Logger.IMRed(hero.Name.ToString() + " selected!");
                                    heroList.Add(hero);
                                }
                            }
                        }
                    }

                    int scholarIndex = GetScholarIndexbySettlement(Settlement.CurrentSettlement);
                    int pricePerHour = GetScholarPrice(scholarIndex);

                    int totalPrice = pricePerHour * this._trainingDuration * heroList.Count;
                    TextObject totalPriceTO = new(totalPrice.ToString() + "{GOLD_ICON}");

                    if (_debug)
                    {
                        LTLogger.IMGreen("Price/h: " + pricePerHour + "  Total price: " + totalPrice + "  Final heroes [" + heroList.Count + "]: ");
                        foreach (Hero hero in heroList)
                        {
                            LTLogger.IMGreen(hero.Name.ToString());
                        }
                    }

                    this._trainingHeroList = heroList;

                    TextObject scholarName = GetScholarName(scholarIndex);
                    SkillObject scholarSkill = GetScholarSkill(scholarIndex);
                    MBTextManager.SetTextVariable("SCHOLAR_NAME", scholarName, false);
                    MBTextManager.SetTextVariable("SKILL_NAME", scholarSkill.ToString(), false);
                    MBTextManager.SetTextVariable("PERSON_PRICE", (pricePerHour * this._trainingDuration).ToString(), false);
                    MBTextManager.SetTextVariable("TOTAL_PRICE", totalPriceTO.ToString(), false);
                    MBTextManager.SetTextVariable("PARTICIPANT_NUMBER", heroList.Count.ToString(), false);

                    if (totalPrice > Hero.MainHero.Gold)
                    {
                        TextObject title = new("{=LTE01209}Not enough gold...");
                        TextObject text = new("{=LTE00544}Price per person {PERSON_PRICE} with {PARTICIPANT_NUMBER} attendee(s).\n\nTotal price: {TOTAL_PRICE}");
                        TextObject aText = new("{=LTE00545}I'll be back...");
                        InformationManager.ShowInquiry(new InquiryData(title.ToString(), text.ToString(), true, false, aText.ToString(), "", 
                        null, null, "event:/ui/notification/coins_negative"), false);
                    } else
                    {
                        // confirmation popup
                        TextObject title = new("{=LTE00546}Start training?");
                        TextObject text =  new("{=LTE00547}{SCHOLAR_NAME} is ready to start training on {SKILL_NAME}.\n\nTraining will cost {TOTAL_PRICE} for {PARTICIPANT_NUMBER} participant(s).\n\nShould we proceed?");
                        TextObject aText = new("{=LTE00503}Let's do this!");
                        TextObject nText = new("{=LTE00549}I changed my mind...");
                        InformationManager.ShowInquiry(new InquiryData(title.ToString(), text.ToString(), true, true, aText.ToString(), nText.ToString(), delegate ()
                        {
                            //Logger.IMBlue("Training started");
                            this._startTimeOfTraining = CampaignTime.Now;
                            this._inTraining = true;
                            this._trainingInterrupted = 0;
                            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, -1 * totalPrice, false);
                            GameMenu.SwitchToMenu("scholar_training_wait");
                        }, delegate ()
                        {
                            //InformationManager.HideInquiry();
                        }, ""), false);

                    }       


                }, (List<InquiryElement> list) => { }, "");
                
                MBInformationManager.ShowMultiSelectionInquiry(data);

                //GameMenu.SwitchToMenu(returnMenu);

            }, false, -1, false);



            starter.AddGameMenuOption("scholar_menu", "leave", new TextObject("{=LTE00505}Maybe next time...").ToString(), (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("education_menu"); }, true, 100);



            starter.AddWaitGameMenu("scholar_training_wait", "{TRAINING_INFO}", delegate (MenuCallbackArgs args)
            {

                int scholarIndex = GetScholarIndexbySettlement(Settlement.CurrentSettlement);
                MBTextManager.SetTextVariable("SCHOLAR_NAME", GetScholarName(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("SKILL_NAME", GetScholarSkill(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("SCHOLAR_LEVEL", GetScholarLevel(scholarIndex).ToString(), false);
                MBTextManager.SetTextVariable("TRAINING_INFO", new TextObject("{=LTE00548}Training by {SCHOLAR_NAME}, {SCHOLAR_LEVEL} in {SKILL_NAME}"), false);

                args.MenuContext.SetBackgroundMeshName(GetScholarImage(scholarIndex));
                args.MenuContext.GameMenu.SetTargetedWaitingTimeAndInitialProgress((float)this._trainingDuration, 0f);        

            }, delegate (MenuCallbackArgs args)
            {
                //args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                OnTrainingEnd();

            }, delegate (MenuCallbackArgs args, CampaignTime dt)
            {               
                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu((float)this._startTimeOfTraining.ElapsedHoursUntilNow / (float)this._trainingDuration);
            }, GameMenu.MenuAndOptionType.WaitMenuShowOnlyProgressOption, GameOverlays.MenuOverlayType.None, 0f, GameMenu.MenuFlags.None, null);


            starter.AddGameMenuOption("scholar_training_wait", "leave", new TextObject("{=LTE00550}Interrupt").ToString(), delegate (MenuCallbackArgs args)
            {
                //args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, delegate (MenuCallbackArgs args)
            {
                //GameMenu.SwitchToMenu("education_menu");
                this._trainingInterrupted++;
                LTLogger.IMRed(new TextObject("{=LTE00551}The quality of training decreased due to insufficient concentration of the participants...").ToString());
                SoundEvent.PlaySound2D("event:/ui/notification/quest_fail");
            }, false, -1, false);


        }


        private void OnTrainingEnd()
        {
            this._inTraining = false;

            TextObject msg = new("{=LTE00552}Training finished");
            LTLogger.IMGreen(msg.ToString());

            SoundEvent.PlaySound2D("event:/ui/notification/peace_offer");

            int scholarIndex = this._trainingScholarIndex;
            if (scholarIndex < 0) scholarIndex = GetScholarIndexbySettlement(Settlement.CurrentSettlement);
            int trainingPrice = GetScholarPrice(scholarIndex);
            SkillObject scholarSkill = GetScholarSkill(scholarIndex);

            int totalExp = (int)((trainingPrice / 5 * this._trainingDuration) * (1 - 0.2 * this._trainingInterrupted));
            if (totalExp < 0) totalExp = 0;

            if (this._trainingHeroList.Count > 0)
            {
                //Logger.IMGreen("Heroes finished training: " + this._trainingHeroList.Count + "  +Exp each: " + totalExp);

                foreach(Hero hero in this._trainingHeroList)
                {
                    hero.HeroDeveloper.AddSkillXp(scholarSkill, totalExp, false, true);
                }

            }

            GameMenu.SwitchToMenu("education_menu");
        }


        private void OnLearningEnd()
        {
            if (_canRead > 100) _canRead = 100;
            SoundEvent.PlaySound2D("event:/ui/notification/peace");

            string popup;
            if (Hero.MainHero.IsFemale || _debug) { popup = "lt_education_popup2"; }
            else { popup = "lt_education_popup1"; }

            CreatePopupVMLayer("{=LTE00522}You can read!", "", "{=LTE00523}Let it be known throughout the land that you are literate!", "", popup, "{=LTE00530}Continue");
            GameMenu.SwitchToMenu("education_menu");
        }




        private List<Hero> FormatHeroList()
        {
            List<Hero> list = new();
            for (int i = 0; i < Hero.MainHero.PartyBelongedTo.MemberRoster.Count; i++)
            {
                CharacterObject characterAtIndex = Hero.MainHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(i);
                if (characterAtIndex.HeroObject != null)
                {
                    Hero hero = characterAtIndex.HeroObject;
                    if (hero.HitPoints >= hero.MaxHitPoints / 2)     // >= 50% health
                    {
                        list.Add(hero);
                    }
                }
            }
            return list;
        }



        private List<InquiryElement> FormatHeroInquiryList(bool includeAll = false)
        {

            List<InquiryElement> list = new();

            if (includeAll)
            {
                list.Add(new InquiryElement(null, new TextObject("{=LTE00553}All capable party members").ToString(), null, true, ""));
            }

            for (int i = 0; i < Hero.MainHero.PartyBelongedTo.MemberRoster.Count; i++)
            {
                CharacterObject characterAtIndex = Hero.MainHero.PartyBelongedTo.MemberRoster.GetCharacterAtIndex(i);
                if (characterAtIndex.HeroObject != null)
                {
                    Hero hero = characterAtIndex.HeroObject;

                    bool activeItem = true;
                    TextObject hint = new("");

                    if (hero.MaxHitPoints / 2 > hero.HitPoints)     // < 50% health
                    {
                        activeItem = false;
                        hint = new TextObject("{=LTE00554}Can't attend - Wounded", null);
                    }

                    list.Add(new InquiryElement(hero, hero.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(characterAtIndex)), activeItem, hint.ToString()));
                }
            }
            return list;
        }

    }
}
