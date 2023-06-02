// This code is used from the Banner Kings mod [https://www.nexusmods.com/mountandblade2bannerlord/mods/3826] with the permission from the mod author βασιλεύςඞ

using SandBox.ViewModelCollection.Input;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace LT.UI
{

    public class LTViewModel : ViewModel
    {

        protected bool selected;

        private InputKeyItemVM _doneInputKey;

        public LTViewModel(bool selected)
        {
            this.selected = selected;
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return this.selected;
            }
            set
            {
                if (value != this.selected)
                {
                    this.selected = value;
                    if (value)
                    {
                        this.RefreshValues();
                    }
                    base.OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        //protected SelectorVM<BKItemVM> GetSelector(BannerKingsPolicy policy, Action<SelectorVM<BKItemVM>> action)
        //{
        //    SelectorVM<BKItemVM> selector = new SelectorVM<BKItemVM>(0, action);
        //    selector.SetOnChangeAction(null);
        //    int i = 0;
        //    foreach (Enum enumValue in policy.GetPolicies())
        //    {
        //        BKItemVM item = new BKItemVM(enumValue, true, policy.GetHint(i), null);
        //        selector.AddItem(item);
        //        i++;
        //    }
        //    return selector;
        //}

        public void ExecuteClose()
        {
            LTUIManager.Instance.CloseUI();
        }




        [DataSourceProperty]
        public InputKeyItemVM DoneInputKey
        {
            get
            {
                return this._doneInputKey;
            }
            set
            {
                if (value != this._doneInputKey)
                {
                    this._doneInputKey = value;
                    base.OnPropertyChangedWithValue<InputKeyItemVM>(value, "DoneInputKey");
                }
            }
        }


        public void SetDoneInputKey(HotKey hotKey)
        {
            this.DoneInputKey = InputKeyItemVM.CreateFromHotKey(hotKey, true);
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            InputKeyItemVM doneInputKey = this.DoneInputKey;
            if (doneInputKey != null)
            {
                doneInputKey.OnFinalize();
            }

        }

    }
}
