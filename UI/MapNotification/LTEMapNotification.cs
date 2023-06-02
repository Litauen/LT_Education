using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapNotificationTypes;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace LT.UI.MapNotification
{
    public class LTECanReadMapNotificationVM : MapNotificationItemBaseVM
    {
        public LTECanReadMapNotificationVM(LTECanReadMapNotification data) : base(data) 
        {
            base.NotificationIdentifier = "lte_can_read";   // our brush
        }
    }


    public class LTECanReadMapNotification : InformationData
    {
        public LTECanReadMapNotification(TextObject description) : base(description) { }

        public override TextObject TitleText
        {
            get
            {
                return new TextObject("{LTE00581}Can read!"); // not sure where this is used
            }
        }

        public override string SoundEventPath
        {
            get
            {
                return "event:/ui/notification/kingdom_decision";   // play this sound on popup
            }
        }
    }


    [HarmonyPatch(typeof(MapNotificationVM), "PopulateTypeDictionary")]
    internal class PopulateNotificationsPatch
    {
        private static void Postfix(MapNotificationVM __instance)
        {
            Dictionary<Type, Type> dic = (Dictionary<Type, Type>)__instance.GetType().GetField("_itemConstructors", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);
            dic.Add(typeof(LTECanReadMapNotification), typeof(LTECanReadMapNotificationVM));
        }
    }



}
