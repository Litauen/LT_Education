using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        private void AddGameMenus(CampaignGameStarter starter)
        {

            if (_bookList == null) return;

            // ------- Learn to read menu town --------

            starter.AddGameMenuOption("town", "education_learn_read_menu_town", "{=LTE00500}Learn to read",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return true;
                return false;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_learn_read_menu_town");
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
                     //MBTextManager.SetTextVariable("MENU_TEXT", "Nobody wants to teach you how to read. They think it's hopeless... (INT < " + _minINTToRead.ToString() + " )", false);
                 }
                 else
                 {
                     MBTextManager.SetTextVariable("READ_PRICE", _readPrice.ToString(), false);
                     MBTextManager.SetTextVariable("MENU_TEXT", "{=LTE00502}You found a scholar who agreed to teach you how to read for {READ_PRICE}{GOLD_ICON} per hour.", false);
                     //MBTextManager.SetTextVariable("MENU_TEXT", "You found a scholar who agreed to teach you how to read for " + _readPrice.ToString() + " {GOLD_ICON} per hour.", false);
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
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("town"); }, true);





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

                    //TextObject msg = new("Paid " + _readPrice.ToString() + "{GOLD_ICON}");
                    Logger.IMGrey(msg.ToString());

                    // learn to read
                    _canRead += LearningToReadPerHourProgress();
                    if (_debug) _canRead += 10;
                }
                _lastHourOfLearning = hour;     // remember last hour the hero was charged

                args.MenuContext.GameMenu.SetProgressOfWaitingInMenu((float)_canRead / 100);

                // control if we can pay for another hour
                if (Hero.MainHero.Gold < _readPrice)
                {
                    Logger.IMRed("{=LTE00508}Not enough gold to continue learning...");
                    args.MenuContext.GameMenu.EndWait();
                    //GameMenu.SwitchToMenu("town");
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
                GameMenu.SwitchToMenu("town");
            }, false, -1, false);




            // -- learn to read village --
            starter.AddGameMenuOption("village", "education_learn_read_menu_village", "{=LTE00500}Learn to read",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return true;
                return false;
            },
            delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("education_learn_read_menu_village"); }, false, 4, false);

            starter.AddGameMenu("education_learn_read_menu_village", "{=LTE00510}Local villagers look confused. After a short talk between themselves, the brightest of them points towards the town...",
             (MenuCallbackArgs args) => {
                 args.MenuContext.SetBackgroundMeshName("book_menu_sprite8");
             }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_learn_read_menu_village", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("village"); }, true);


            // -- learn to read castle --
            starter.AddGameMenuOption("castle", "education_learn_read_menu_castle", "{=LTE00500}Learn to read",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return true;
                return false;
            },
            delegate (MenuCallbackArgs args) { GameMenu.SwitchToMenu("education_learn_read_menu_castle"); }, false, 5, false);

            starter.AddGameMenu("education_learn_read_menu_castle", "{=LTE00511}Locals look puzzled. After scratching their heads they suggest you should go to the town...",
             (MenuCallbackArgs args) => {
                 args.MenuContext.SetBackgroundMeshName("book_menu_sprite9");
             }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_learn_read_menu_castle", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => { GameMenu.SwitchToMenu("castle"); }, true);



            // --------- Manage your education menu -----------

            starter.AddGameMenuOption("town", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return false;
                return GetPlayerBookAmount(_bookList) > 0;
                //return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 9, false);


            // ----------------- test popup menu ------------------
            if (_debug)
            {
                starter.AddGameMenuOption("town", "test_popup", "Test popup",
                (MenuCallbackArgs args) =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Default;
                    if (_canRead < 100) return false;
                    return true;
                },
                delegate (MenuCallbackArgs args) { CreatePopupVMLayer("Howdy!!!!", "", "You are awesome!", "You smart-ass skill increased by 10000!", "lt_education_book17", "{=LTE00530}Continue"); }, false, 9, false);
            }



            // ---------------------------------------------------------------------------------

            starter.AddGameMenuOption("village", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return false;
                return GetPlayerBookAmount(_bookList) > 0;
                //return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 4, false);


            starter.AddGameMenuOption("castle", "education_menu", "{=LTE00512}Manage your education",
            (MenuCallbackArgs args) => {
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                if (_canRead < 100) return false;
                return GetPlayerBookAmount(_bookList) > 0;
                //return true;
            },
            delegate (MenuCallbackArgs args)
            {
                GameMenu.SwitchToMenu("education_menu");
            }, false, 5, false);


            starter.AddGameMenu("education_menu", "{CURRENTLY_READING}",
            (MenuCallbackArgs args) => {

                _readingInMenu = false;

                if (PlayerEncounter.EncounterSettlement.IsCastle)
                {
                    args.MenuContext.SetBackgroundMeshName("book_menu_sprite3");
                }
                else
                if (PlayerEncounter.EncounterSettlement.IsTown)
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
                if (!PlayerHasBook(_bookInProgress)) _bookInProgress = -1;

                if (_bookInProgress != -1)
                {
                    int progress = (int)_bookProgress[_bookInProgress];

                    MBTextManager.SetTextVariable("READING_DATA", GetBookNameByIndex(_bookInProgress) + " [" + progress + "%]", false);
                    MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00513}Currently reading: \n\n{READING_DATA}", false);

                    //MBTextManager.SetTextVariable("CURRENTLY_READING", "Currently reading: \n\n" + GetBookNameByIndex(_bookInProgress) + " [" + progress + "%]", false);
                }
                else
                {
                    MBTextManager.SetTextVariable("CURRENTLY_READING", "{=LTE00514}You are not reading anything currently.", false);
                }

            }, GameOverlays.MenuOverlayType.SettlementWithBoth);

            starter.AddGameMenuOption("education_menu", "start_reading", "{=LTE00532}Start reading", (MenuCallbackArgs args) =>
            {
                if (_bookInProgress < 0) return false;
                args.optionLeaveType = GameMenuOption.LeaveType.WaitQuest;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                //_bookInProgress = -1;
                GameMenu.SwitchToMenu("education_reading_menu");
            }, false, -1, false);


            starter.AddGameMenuOption("education_menu", "select book", "{=LTE00515}Select what to read",
                (MenuCallbackArgs args) => {
                    args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    //return GetPlayerBookAmount() > 0;
                    return true;
                },
            delegate (MenuCallbackArgs args)
            {
                List<InquiryElement> list = new();

                foreach (ItemObject book in GetPlayerBooks(_bookList))
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
                list, true, 1, new TextObject("{=LTE00519}Select").ToString(), new TextObject("{=LTE00504}Leave").ToString(), (List<InquiryElement> list) => {
                    // what we do with selected book?
                    foreach (InquiryElement inquiryElement in list)
                    {
                        if (inquiryElement != null && inquiryElement.Identifier != null)
                        {
                            ItemObject? book = inquiryElement.Identifier as ItemObject;
                            if (book != null)
                            {
                                TextObject willReadTO = new("{=LTE00520}Will read ");
                                Logger.IM(willReadTO.ToString() + book.Name.ToString());
                                _bookInProgress = GetBookIndex(book.StringId);

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
                _bookInProgress = -1;
                GameMenu.SwitchToMenu("education_menu");
            }, false, -1, false);

            starter.AddGameMenuOption("education_menu", "leave", "{=LTE00504}Leave", (MenuCallbackArgs args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) =>
            {
                //GameMenu.SwitchToMenu("town")
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


            //starter.AddGameMenu("education_reading_menu", " === Reading book....",
            // (MenuCallbackArgs args) => {

            //     Random rand = new();
            //     int rndMenu = 10 + rand.Next(18);

            //     args.MenuContext.SetBackgroundMeshName("book_menu_sprite" + rndMenu.ToString());
            // }, TaleWorlds.CampaignSystem.Overlay.GameOverlays.MenuOverlayType.SettlementWithBoth);


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



        }


        private void OnLearningEnd()
        {
            if (_canRead > 100) _canRead = 100;
            SoundEvent.PlaySound2D("event:/ui/notification/peace");

            string popup;
            if (Hero.MainHero.IsFemale || _debug) { popup = "lt_education_popup2"; }
            else { popup = "lt_education_popup1"; }

            CreatePopupVMLayer("{=LTE00522}You can read!", "", "{=LTE00523}Let it be known throughout the land that you are literate!", "", popup, "{=LTE00530}Continue");
            GameMenu.SwitchToMenu("town");
        }


    }
}
