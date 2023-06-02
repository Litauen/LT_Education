using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using LT.Logger;
using LT_Education;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace UI.Extensions
{
    [ViewModelMixin("RefreshBindValues")]
    internal class SettlementNameplateVMMixin : BaseViewModelMixin<SettlementNameplateVM>
    {

        private readonly SettlementNameplateVM settlementNameplateVM;

        private bool _hasScholar;
        private int _bookIconYOffset;

        private CampaignTime _lastUpdateTime;

        public SettlementNameplateVMMixin(SettlementNameplateVM vm) : base(vm)
        {
            settlementNameplateVM = vm;

            HasScholar = false;
            _lastUpdateTime = CampaignTime.HoursFromNow(-25); // test on new game start

            var settlement = settlementNameplateVM.Settlement as Settlement;
            if (settlement == null) return;

            if (settlement.IsTown) BookIconYOffset = 40;
            else if (settlement.IsCastle) BookIconYOffset = 35;
            else BookIconYOffset = 30;
        }


        [DataSourceProperty]
        public int BookIconYOffset
        {
            get => _bookIconYOffset;
            set
            {
                if (value != _bookIconYOffset)
                {
                    _bookIconYOffset = value;
                    ViewModel!.OnPropertyChangedWithValue(value);
                }
            }
        }


        [DataSourceProperty]
        public bool HasScholar
        {
            get => _hasScholar;
            set
            {
                if (value != _hasScholar)
                {
                    _hasScholar = value;
                    ViewModel!.OnPropertyChangedWithValue(value);
                }
            }
        }

        public override void OnRefresh()
        {
            bool isInRange = settlementNameplateVM.IsInRange;
            if (!isInRange) return; // no need to update if not visible

            int hour = (int)CampaignTime.Now.CurrentHourInDay;
            if (hour == 0) return; // no update on the hour of scholar migration
            int elapsedHours = (int)_lastUpdateTime.ElapsedHoursUntilNow;
            if (elapsedHours == 0) return; // updated this hour
            if (elapsedHours < hour) return; // today already updated

            if (settlementNameplateVM.Settlement is not Settlement settlement) return;

            if (LHelpers.GetPartyScoutingLevel(MobileParty.MainParty) < LT_EducationBehaviour.Instance.ScoutingLevelToSeeScholarIcons) return;

            HasScholar = false;

            if (LT_EducationBehaviour.Instance.GetScholarIndexbySettlement(settlement) > 0) HasScholar = true;  // scholars
         
            if (LT_EducationBehaviour.Instance.IsAnyVendorInTown(settlement)) HasScholar = true;    // vendors

            //LTLogger.IMRed(settlement.Name.ToString() + " h:" + hour.ToString() + " elapsed: " + elapsedHours.ToString());

            // save time when updated
            _lastUpdateTime = CampaignTime.Now;
        }
    }
}