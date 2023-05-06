using LT_Education;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace LT.UI
{
    public class LTEducationBookVM : ViewModel
    {
        private ImageIdentifierVM _visual;
        private string _name;
        private string _readProgress;
        private string _skillSprite;
        private string _skillValue;
        private HintViewModel _skillHint;
        private HintViewModel _bookHint;
        private LTDecisionElement _readButton;

        public LTEducationBookVM(ItemObject book, int readProgress, string skillHint, int bookIndex, Hero hero, SkillObject skill)
        {

            if (skill == null) return;

            int skillValue = hero.GetSkillValue(skill);
            string skillSprite= "SPGeneral\\Skills\\gui_skills_icon_" + skill.StringId.ToString().ToLower() + "_tiny";

            this.Visual = new ImageIdentifierVM(new ImageIdentifier(book));
            this.Name = book.Name.ToString();
            this.ReadProgress = readProgress.ToString()+"%";
            this.SkillSprite = skillSprite;
            this.SkillValue = skillValue.ToString();
            this.SkillHint = new HintViewModel(new TextObject(skillHint), null);

            TextObject hint = new("{=LTE00561}Reading {BOOK_NAME}, will increase {SKILL_NAME} skill.");
            hint.SetTextVariable("BOOK_NAME", this.Name);
            hint.SetTextVariable("SKILL_NAME", skill.Name);

            this.BookHint = new HintViewModel(new TextObject(hint.ToString()), null);

            var _readButton = new LTDecisionElement().SetAsButtonOption(new TextObject("{=LTE00560}Read").ToString(),
                () => LT_EducationBehaviour.Instance.HeroStartReadBookFromUI(hero, bookIndex),
                new TextObject(""));

            _readButton.Enabled = true;

            if (readProgress < 100) _readButton.Show = true; else _readButton.Show = false;

            ReadButton = _readButton;

        }


        [DataSourceProperty]
        public LTDecisionElement ReadButton
        {
            get => _readButton;
            set
            {
                if (value != _readButton)
                {
                    _readButton = value;
                    OnPropertyChangedWithValue(value);
                }
            }
        }


        [DataSourceProperty]
        public ImageIdentifierVM Visual
        {
            get => this._visual;
            set
            {
                if (value != this._visual)
                {
                    this._visual = value;
                    base.OnPropertyChangedWithValue<ImageIdentifierVM>(value, "Visual");
                }
            }
        }


        [DataSourceProperty]
        public string Name
        {
            get => this._name;
            set
            {
                if (value != this._name)
                {
                    this._name = value;
                    base.OnPropertyChangedWithValue(value, "Name");
                }
            }
        }


        [DataSourceProperty]
        public string ReadProgress
        {
            get => this._readProgress;
            set
            {
                if (value != this._readProgress)
                {
                    this._readProgress = value;
                    base.OnPropertyChangedWithValue(value, "ReadProgress");
                }
            }
        }

        [DataSourceProperty]
        public string SkillSprite
        {
            get => this._skillSprite;
            set
            {
                if (value != this._skillSprite)
                {
                    this._skillSprite = value;
                    base.OnPropertyChangedWithValue(value, "SkillSprite");
                }
            }
        }

        [DataSourceProperty]
        public string SkillValue
        {
            get => this._skillValue;
            set
            {
                if (value != this._skillValue)
                {
                    this._skillValue = value;
                    base.OnPropertyChangedWithValue(value, "SkillValue");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel SkillHint
        {
            get
            {
                return this._skillHint;
            }
            set
            {
                if (value != this._skillHint)
                {
                    this._skillHint = value;
                    base.OnPropertyChangedWithValue<HintViewModel>(value, "SkillHint");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel BookHint
        {
            get
            {
                return this._bookHint;
            }
            set
            {
                if (value != this._bookHint)
                {
                    this._bookHint = value;
                    base.OnPropertyChangedWithValue<HintViewModel>(value, "BookHint");
                }
            }
        }

    }

}
