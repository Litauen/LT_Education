// This code is used from the Banner Kings mod [https://www.nexusmods.com/mountandblade2bannerlord/mods/3826] with the permission from the mod author βασιλεύςඞ

using System;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using LT.Logger;
using TaleWorlds.GauntletUI.Data;

namespace LT.UI
{

    public class LTMapView : MapView
    {

        public string id;
        private SpriteCategory? _categoryDeveloper;
        private SpriteCategory? _categoryEncyclopedia;

        private new GauntletLayer? Layer { get; set; }

        private LTViewModel? VM { get; set; }

        private GauntletMovie? _gauntletMovie;

        public LTMapView(string id)
        {
            this.id = id;
            this.CreateLayout();
        }


        protected override void CreateLayout()
        {
            base.CreateLayout();
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot resourceDepot = UIResourceManager.UIResourceDepot;
            _categoryDeveloper = spriteData.SpriteCategories["ui_characterdeveloper"];
            _categoryDeveloper.Load(resourceContext, resourceDepot);
            _categoryEncyclopedia = spriteData.SpriteCategories["ui_encyclopedia"];
            _categoryEncyclopedia.Load(resourceContext, resourceDepot);
            ValueTuple<LTViewModel, string> tuple = GetVM(this.id);
            this.Layer = new GauntletLayer(1000, "GauntletLayer", false);
            this.VM = tuple.Item1;
            _gauntletMovie = (GauntletMovie)this.Layer.LoadMovie(tuple.Item2, tuple.Item1);
            this.Layer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
            MapScreen.Instance.AddLayer(this.Layer);
            ScreenManager.TrySetFocus(this.Layer);
        }

        private (LTViewModel, string) GetVM(string id)
        {

            string movieXML = "";

            if (id == "BookStash") movieXML = "LTEBookStash";

            return new ValueTuple<LTViewModel, string>(new LTEducationBookStashVM(Hero.MainHero), movieXML);

        }

        public void Close()
        {
            if (this.Layer == null) return;
            this.Layer.InputRestrictions.ResetInputRestrictions();
            this.Layer.IsFocusLayer = false;
            if (_gauntletMovie != null) this.Layer.ReleaseMovie(_gauntletMovie);
            
            MapScreen.Instance.RemoveLayer(this.Layer);
            
            //_categoryDeveloper?.Unload();
            //_categoryEncyclopedia?.Unload();
            this.Layer = null;
            _gauntletMovie = null;
            this.VM = null;
        }


        public void Refresh()
        {
            this.VM?.RefreshValues();
        }

    }
}
