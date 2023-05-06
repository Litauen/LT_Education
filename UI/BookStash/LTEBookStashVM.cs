using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using LT.Logger;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using LT_Education;
using TaleWorlds.Core.ViewModelCollection.Information;
using System.Collections.Generic;

namespace LT.UI
{

    public class LTEducationBookStashVM : LTViewModel
    {

        private Hero _hero;

        private ImageIdentifierVM _imageIdentifier;
        private ImageIdentifierVM _banner;
        private string _heroName;
        private string _INTText;
        private string _statusLineText;
        private bool _readingBook;

        private bool _noBooks;
        private bool _noScrolls;

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

        //[DataSourceProperty]
        public HintViewModel StopReadingBookHint => new(new TextObject("{=LTE00564}Stop Reading"));


        public LTEducationBookStashVM(Hero hero) : base(true)
        {

            //_imageIdentifier = new ImageIdentifierVM(new ImageIdentifier(CampaignUIHelper.GetCharacterCode(hero.CharacterObject)));   // to get rid of warning

            _hero = hero;

            _heroName = hero.Name.ToString();          
            _INTText = "INT: " + hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence).ToString();
            _statusLineText = SetStatusLineText();
            _readingBook = false;
            _noBooks = true;
            _noScrolls = true;
        }


        public override void RefreshValues()
        {
            base.RefreshValues();

            ImageIdentifier = new ImageIdentifierVM(new ImageIdentifier(CampaignUIHelper.GetCharacterCode(_hero.CharacterObject)));
            Banner = new ImageIdentifierVM(BannerCode.CreateFrom(_hero.Clan.Banner), true);
            HeroName =_hero.Name.ToString();
            INTText = "INT: " + _hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence).ToString();
            StatusLineText = SetStatusLineText();
            ReadingBook = _readingBook;
            NotReadingBook = !_readingBook;

            BooksInfo = new MBBindingList<LTEducationBookVM>();
            //List<ItemObject> booksList = LT_EducationBehaviour.Instance.GetAllBooks();
            List<ItemObject> booksList = LT_EducationBehaviour.Instance.GetUniquePartyBooks(1);
            if (booksList.Count == 0) NoBooks = true; else NoBooks = false;
            foreach (ItemObject item in booksList)
            {
                int bookIndex = LT_EducationBehaviour.Instance.GetBookIndex(item.StringId);
                int progress = (int)LT_EducationBehaviour.Instance.GetHeroesBookProgress(_hero, bookIndex);
                SkillObject skill = LT_EducationBehaviour.Instance.GetSkillByBookIndex(bookIndex);

                BooksInfo.Add(new LTEducationBookVM(item, progress, skill.Name.ToString(), bookIndex, _hero, skill));
            }

            ScrollsInfo = new MBBindingList<LTEducationBookVM>();
            //List<ItemObject> scrollList = LT_EducationBehaviour.Instance.GetAllScrolls();
            List<ItemObject> scrollList = LT_EducationBehaviour.Instance.GetUniquePartyBooks(2);
            if (scrollList.Count == 0) NoScrolls = true; else NoScrolls = false;
            foreach (ItemObject item in scrollList)
            {
                int bookIndex = LT_EducationBehaviour.Instance.GetBookIndex(item.StringId);
                int progress = (int)LT_EducationBehaviour.Instance.GetHeroesBookProgress(_hero, bookIndex);
                SkillObject skill = LT_EducationBehaviour.Instance.GetSkillByBookIndex(bookIndex);

                ScrollsInfo.Add(new LTEducationBookVM(item, progress, skill.Name.ToString(), bookIndex, _hero, skill));
            }

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


        private string SetStatusLineText()
        {
            float canRead = LT_EducationBehaviour.Instance.HeroCanRead(_hero);

            string statusLineText;
            if (canRead == 0)
            {
                statusLineText = new TextObject("{=LTE00567}Can't read").ToString();
            }
            else if (canRead < 100)
            {
                statusLineText = new TextObject("{=LTE00568}Learning to read").ToString() + " [" + ((int)canRead).ToString() + "%]";
            }
            else
            {
                //statusLineText = new TextObject("===Can read").ToString();

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
                }

            }
            return statusLineText;
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


        public void StopReadingBook()
        {
            //LTLogger.IM("StopReadingBook");
            _readingBook = false;
            LT_EducationBehaviour.Instance.HeroStopReadingAndReturnBookToParty(_hero);
            RefreshValues();
        }



        //public void StartReadingBook(int parameter)
        //{
        //    LTLogger.IM("StartReadingBook: " + parameter.ToString());
        //    //_readingBook = false;
        //    //LT_EducationBehaviour.Instance.HeroStopReadingAndReturnBookToParty(_hero);
        //    //RefreshValues();
        //}

    }
}