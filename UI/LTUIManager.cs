// This code is used from the Banner Kings mod [https://www.nexusmods.com/mountandblade2bannerlord/mods/3826] with the permission from the mod author βασιλεύςඞ

using LT.Logger;
using SandBox.View.Map;
using System;
using TaleWorlds.CampaignSystem.GameMenus;

namespace LT.UI
{
    internal class LTUIManager
    {
        private static LTUIManager? _instance;
        private LTMapView? _mapView;

        private string _menuOnClose = "";

        public static LTUIManager Instance
        {
            get
            {
                if (_instance == null) _instance = new LTUIManager();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public void ShowWindow(string id, string menuOnClose = "")
        {
            _menuOnClose = menuOnClose;
            if (_mapView != null) _mapView.Close();
            _mapView = new LTMapView(id);
            _mapView.Refresh();
        }

        public void CloseUI()
        {
            if (_mapView != null)
            {
                _mapView.Close();
                _mapView = null;
            }
            if (_menuOnClose != "") GameMenu.SwitchToMenu(_menuOnClose);
        }

        public void Refresh()
        {
            _mapView?.Refresh();
        }

    }
}
