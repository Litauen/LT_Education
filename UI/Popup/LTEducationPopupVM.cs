using TaleWorlds.Library;

namespace LT_Education
{

    public class EducationPopupVM : ViewModel
    {
        private string _title;
        private string _smallText;
        private string _bigText;
        private string _textOverImage;
        private string _spriteName;
        private string _closeButtonText;

        public EducationPopupVM(string title, string smallText, string bigText, string textOverImage, string spriteName, string closeButtonText)
        {
            _title = title;
            _smallText = smallText;
            _bigText = bigText;
            _textOverImage = textOverImage;
            _spriteName = spriteName;
            _closeButtonText = closeButtonText;
        }

        public void Close()
        {
            LT_EducationBehaviour.DeletePopupVMLayer();
        }

        public void Refresh()
        {
            Title = _title;
            SmallText = _smallText;
            BigText = _bigText;
            TextOverImage = _textOverImage;
            SpriteName = _spriteName;
            CloseButtonText = _closeButtonText;
        }


        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChangedWithValue(value, "PopupTitle");
            }
        }

        public string SmallText
        {
            get
            {
                return _smallText;
            }
            set
            {
                _smallText = value;
                OnPropertyChangedWithValue(value, "PopupSmallText");
            }
        }

        public string BigText
        {
            get
            {
                return _bigText;
            }
            set
            {
                _bigText = value;
                OnPropertyChangedWithValue(value, "PopupBigText");
            }
        }

        public string TextOverImage
        {
            get
            {
                return _textOverImage;
            }
            set
            {
                _textOverImage = value;
                OnPropertyChangedWithValue(value, "PopupTextOverImage");
            }
        }

        public string SpriteName
        {
            get
            {
                return _spriteName;
            }
            set
            {
                _spriteName = value;
                OnPropertyChangedWithValue(value, "SpriteName");
            }
        }

        public string CloseButtonText
        {
            get
            {
                return _closeButtonText;
            }
            set
            {
                _closeButtonText = value;
                OnPropertyChangedWithValue(value, "CloseButtonText");
            }
        }

    }
}