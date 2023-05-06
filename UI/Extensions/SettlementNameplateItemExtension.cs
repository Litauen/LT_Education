using System.Xml;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
//using LT.Logger;

namespace UI.Extensions
{
    [PrefabExtension("SettlementNameplateItem", "descendant::ListPanel[@Id='EventsListPanel']")]
    internal class SettlementNameplateItemExtension : PrefabExtensionInsertPatch
    {

        public override InsertType Type => InsertType.Append;
        
        private XmlDocument document;

        public SettlementNameplateItemExtension()
        {
            document = new XmlDocument();
            document.LoadXml("<Widget IsVisible=\"@IsInRange\"> <Children> <Widget WidthSizePolicy=\"Fixed\" HeightSizePolicy=\"Fixed\" SuggestedWidth=\"14\" SuggestedHeight=\"9\" PositionXOffset=\"-20\" PositionYOffset=\"@BookIconYOffset\" HorizontalAlignment=\"Left\" VerticalAlignment=\"Center\" Sprite=\"lt_book_map_icon\" AlphaFactor=\"0.9\" IsEnabled=\"false\" IsVisible=\"@HasScholar\"/> </Children> </Widget>");

            //LTLogger.IMRed("UIExtenderEx active");
        }

        [PrefabExtensionXmlDocument]
        public XmlDocument GetPrefabExtension() => document;
    }
}