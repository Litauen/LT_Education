using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using LT.Logger;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using LT_Education;
using TaleWorlds.Core.ViewModelCollection.Information;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using SandBox.ViewModelCollection.Input;
using TaleWorlds.InputSystem;

namespace LT.UI
{

    public class LTEducationBookStashVM : LTViewModel
    {

        private Hero _hero;
        private List<Hero> _heroList;
        private int _heroIndex;
        private float _heroCanRead;
        private int _heroINT;

        private ImageIdentifierVM _imageIdentifier;
        private ImageIdentifierVM _banner;
        private string _heroName;
        private string _INTText;
        private string _statusLineText;
        private string _statusLineText2;
        private bool _readingBook;

        private bool _noBooks;
        private bool _noScrolls;

        private bool _hasSingleItem;

        private int _bookScrollsCount;

        // Books
        private MBBindingList<LTEducationBookVM> _booksInfo;
        // Scrolls
        private MBBindingList<LTEducationBookVM> _scrollsInfo;


        public string WindowTitle => new TextObject("{=LTE00563}Party's Book Stash").ToString();
        public string ReadingText => new TextObject("{=LTE00557}Reading").ToString() + ":";
        //public string StopReadingText => new TextObject("{=LTE00564}Stop Reading").ToString();

        public string BooksText => new TextObject("{=LTE00558}Books").ToString();
        public string ScrollsText => new TextObject("{=LTE00559}Scrolls").ToString();

        public string PartyHasNoBooksText => new TextObject("{=LTE00565}Party has no books").ToString();
        public string PartyHasNoScrollsText => new TextObject("{=LTE00566}Party has no scrolls").ToString();

        public HintViewModel StopReadingBookHint => new(new TextObject("{=LTE00564}Stop Reading"));
        public HintViewModel NextCharacterHint => new(GameTexts.FindText("str_inventory_next_char", null));
        public HintViewModel PreviousCharacterHint => new(GameTexts.FindText("str_inventory_prev_char", null));

        public LTDecisionElement ToggleAutoReadCheckbox { get; set; }




        public LTEducationBookStashVM(Hero hero) : base(true)
        {

            _imageIdentifier = new ImageIdentifierVM();

            _hero = hero;
            _heroIndex = 0;

            _heroList = (from characterObject in Hero.MainHero.PartyBelongedTo.MemberRoster.GetTroopRoster()
                         where characterObject.Character.HeroObject != null
                         select characterObject.Character.HeroObject).ToList<Hero>();

            _heroCanRead = 0;
            _heroINT = 0;

            _heroName = "";
            _INTText = "";
            _statusLineText = "";
            _statusLineText2 = "";
            _readingBook = false;
            _noBooks = true;
            _noScrolls = true;
            _hasSingleItem= true;

            _bookScrollsCount = 0;

            //ToggleAutoReadCheckbox = new LTDecisionElement();
            ToggleAutoReadCheckbox = new LTDecisionElement();
            ToggleAutoReadCheckbox.SetAsBooleanOption(new TextObject("{=LTE00579}Select the next book automatically").ToString(), true,
                  delegate (bool x) {
                      ToggleAutoReadCheckboxAction(_hero, x);
                  },
                  new TextObject("{=LTE00580}The companion will select the next book to read automatically upon finishing reading the current one"));
            //ToggleAutoReadCheckbox.Show = true;

            //SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));      // does not work :(
        }


        public override void RefreshValues()
        {
            base.RefreshValues();

            _heroCanRead = LT_EducationBehaviour.Instance.HeroCanRead(_hero);
            _heroINT = _hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);

            ImageIdentifier = new ImageIdentifierVM(new ImageIdentifier(CampaignUIHelper.GetCharacterCode(_hero.CharacterObject)));
            Banner = new ImageIdentifierVM(BannerCode.CreateFrom(_hero.Clan.Banner), true);
            HeroName =_hero.Name.ToString();
            INTText = "INT: " + _heroINT.ToString();

            BooksInfo = new MBBindingList<LTEducationBookVM>();
            List<ItemObject> booksList = LT_EducationBehaviour.Instance.GetUniquePartyBooks(1);
            if (booksList.Count == 0) NoBooks = true; else NoBooks = false;
            foreach (ItemObject item in booksList)
            {
                int bookIndex = LT_EducationBehaviour.Instance.GetBookIndex(item.StringId);
                int progress = (int)LT_EducationBehaviour.Instance.GetHeroesBookProgress(_hero, bookIndex);
                SkillObject skill = LT_EducationBehaviour.Instance.GetSkillByBookIndex(bookIndex);

                BooksInfo.Add(new LTEducationBookVM(item, progress, skill.Name.ToString(), bookIndex, _hero, skill, _heroCanRead == 100));
            }

            ScrollsInfo = new MBBindingList<LTEducationBookVM>();
            List<ItemObject> scrollList = LT_EducationBehaviour.Instance.GetUniquePartyBooks(2);
            if (scrollList.Count == 0) NoScrolls = true; else NoScrolls = false;
            foreach (ItemObject item in scrollList)
            {
                int bookIndex = LT_EducationBehaviour.Instance.GetBookIndex(item.StringId);
                int progress = (int)LT_EducationBehaviour.Instance.GetHeroesBookProgress(_hero, bookIndex);
                SkillObject skill = LT_EducationBehaviour.Instance.GetSkillByBookIndex(bookIndex);

                ScrollsInfo.Add(new LTEducationBookVM(item, progress, skill.Name.ToString(), bookIndex, _hero, skill, _heroCanRead == 100));
            }

            _bookScrollsCount = booksList.Count + scrollList.Count;

            SetStatusLinesTexts();

            ReadingBook = _readingBook;
            NotReadingBook = !_readingBook;

            List<Hero> heroList = LHelpers.GetPartyCompanionsList();
            if (heroList.Count > 0) HasSingleItem = false; else HasSingleItem = true;

            if (_hero == Hero.MainHero)
            {
                ToggleAutoReadCheckbox.Show = false;
            }
            else
            {
                ToggleAutoReadCheckbox.Show = _readingBook;
                ToggleAutoReadCheckbox.OptionValueAsBoolean = LT_EducationBehaviour.Instance._LTECompanions.GetCompanionAutoRead(_hero) > 0;
            }

            //LTLogger.IM("_bookScrollsCount: " + _bookScrollsCount.ToString());
        }


        private void ToggleAutoReadCheckboxAction(Hero hero, bool enable)
        {
            //LTLogger.IMRed("ToggleAutoReadCheckboxAction: " + hero.Name.ToString() + " - " + enable.ToString());
            if (hero == Hero.MainHero) return;

            int val = 0;
            if (enable) val = 1;
            LT_EducationBehaviour.Instance._LTECompanions.SetCompanionAutoRead(_hero, val);
        }

        private void SetStatusLinesTexts()
        {
            float canRead = _heroCanRead;

            _readingBook = false;

            string statusLineText;

            TextObject statusLineText2TO = new("");

            if (canRead == 0)
            {
                statusLineText = new TextObject("{=LTE00567}Can't read").ToString();

                int minINTTORead = LT_EducationBehaviour.Instance.GetMinINTToRead();
                
                //minINTTORead = 2; // debug

                if (_heroINT < minINTTORead)
                {
                    statusLineText2TO = new TextObject("{=LTE00570}Need INT {MIN_INT} to learn to read.");
                    statusLineText2TO.SetTextVariable("MIN_INT", minINTTORead.ToString());
                } else
                {
                    statusLineText2TO = new TextObject("{=LTE00571}Will start to learn to read soon.");
                }
                
            }
            else if (canRead < 100)
            {
                statusLineText = new TextObject("{=LTE00568}Learning to read").ToString() + " [" + ((int)canRead).ToString() + "%]";

                if (_hero == Hero.MainHero) statusLineText2TO = new TextObject("{=LTE00572}Seek scholars in the town to learn to read.");
                                       else statusLineText2TO = new TextObject("{=LTE00573}It's not easy and takes time...");

            }
            else    // can read
            {
                int bookInProgress = LT_EducationBehaviour.Instance.GetHeroesBookInProgress(_hero);

                if (bookInProgress > -1)
                {
                    int progress = (int)LT_EducationBehaviour.Instance.GetHeroesBookProgress(_hero, bookInProgress);
                    statusLineText = LT_EducationBehaviour.Instance.GetBookNameByIndex(bookInProgress) + " [" + progress.ToString() + "%]";
                    _readingBook = true;
                }
                else
                {
                    statusLineText = new TextObject("{=LTE00562}Currently not reading anything.").ToString();

                    if (_bookScrollsCount > 0) statusLineText2TO = new TextObject("{=LTE00574}Select book or scroll to read.");
                                          else statusLineText2TO = new TextObject("{=LTE00575}Buy Books or Scrolls from vendors to read.");
                }

            }

            StatusLineText = statusLineText;
            StatusLineText2 = statusLineText2TO.ToString();

        }


        [DataSourceProperty]
        public ImageIdentifierVM Banner
        {
            get => _banner;
            set
            {
                if (value != _banner)
                {
                    _banner = value;
                    //OnPropertyChangedWithValue(value);
                    //OnPropertyChanged("Banner");
                    OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Banner");
                }
            }
        }


        [DataSourceProperty]
        public ImageIdentifierVM ImageIdentifier
        {
            get => _imageIdentifier;
            set
            {
                _imageIdentifier = value;
                OnPropertyChanged("ImageIdentifier");
            }
        }


        [DataSourceProperty]
        public string HeroName
        {
            get => _heroName;
            set
            {
                _heroName = value;
                OnPropertyChangedWithValue(value, "HeroName");
            }
        }


        [DataSourceProperty]
        public string INTText
        {
            get => _INTText;
            set
            {
                _INTText = value;
                OnPropertyChangedWithValue(value, "INTText");
            }
        }

        [DataSourceProperty]
        public string StatusLineText
        {
            get => _statusLineText;
            set
            {
                _statusLineText = value;
                OnPropertyChangedWithValue(value, "StatusLineText");
            }
        }

        [DataSourceProperty]
        public string StatusLineText2
        {
            get => _statusLineText2;
            set
            {
                _statusLineText2 = value;
                OnPropertyChangedWithValue(value, "StatusLineText2");
            }
        }


        [DataSourceProperty]
        public bool ReadingBook
        {
            get => _readingBook;
            set
            {
                _readingBook = value;
                OnPropertyChangedWithValue(value, "ReadingBook");
            }
        }

        [DataSourceProperty]
        public bool NotReadingBook
        {
            get => !_readingBook;
            set
            {
                _readingBook = !value;
                OnPropertyChangedWithValue(value, "NotReadingBook");
            }
        }

        [DataSourceProperty]
        public bool NoBooks
        {
            get => _noBooks;
            set
            {
                _noBooks = value;
                OnPropertyChangedWithValue(value, "NoBooks");
            }
        }

        [DataSourceProperty]
        public bool NoScrolls
        {
            get => _noScrolls;
            set
            {
                _noScrolls = value;
                OnPropertyChangedWithValue(value, "NoScrolls");
            }
        }

        [DataSourceProperty]
        public bool HasSingleItem
        {
            get => _hasSingleItem;
            set
            {
                _hasSingleItem = value;
                OnPropertyChangedWithValue(value, "HasSingleItem");
            }
        }

        //private string FormatPercentString(int percent)
        //{
        //    string res = "";

        //    //if (percent == 0 || percent == 100) { return ""; }

        //    res = percent.ToString() + "%";

        //    return res;
        //}



        [DataSourceProperty]
        public MBBindingList<LTEducationBookVM> BooksInfo
        {
            get
            {
                return this._booksInfo;
            }
            set
            {
                if (value != this._booksInfo)
                {
                    this._booksInfo = value;
                    base.OnPropertyChangedWithValue<MBBindingList<LTEducationBookVM>>(value, "BooksInfo");
                }
            }
        }


        [DataSourceProperty]
        public MBBindingList<LTEducationBookVM> ScrollsInfo
        {
            get
            {
                return this._scrollsInfo;
            }
            set
            {
                if (value != this._scrollsInfo)
                {
                    this._scrollsInfo = value;
                    base.OnPropertyChangedWithValue<MBBindingList<LTEducationBookVM>>(value, "ScrollsInfo");
                }
            }
        }


        public void ExecuteStopReadingBook()
        {
            //LTLogger.IM("ExecuteStopReadingBook");
            _readingBook = false;
            LT_EducationBehaviour.Instance.HeroStopReadingAndReturnBookToParty(_hero);
            RefreshValues();
        }


        public void ExecuteSelectNextHero()
        {
            //LTLogger.IM("ExecuteSelectNextHero");
            if (_heroList.Count < 2) return;
            _heroIndex = (_heroIndex + 1) % _heroList.Count;
            _hero = _heroList[_heroIndex];
            RefreshValues();

        }

        public void ExecuteSelectPreviousHero()
        {
            //LTLogger.IM("ExecuteSelectPreviousHero");
            if (_heroList.Count < 2) return;
            _heroIndex = (_heroIndex == 0) ? _heroList.Count - 1 : _heroIndex - 1;
            _hero = _heroList[_heroIndex];
            RefreshValues();

        }

    }
}