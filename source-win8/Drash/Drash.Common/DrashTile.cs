using System;
using System.Linq;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Drash.Common
{
    public static class DrashTile
    {
        public static void Update(int chance, string location)
        {
            try {
                var xml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText01);
                var tileTextAttributes = xml.GetElementsByTagName("text");
                tileTextAttributes[0].AppendChild(xml.CreateTextNode(string.Format("{0}%", chance)));
                var parts = location.Split(new[] { ',' }, 2).Select(x => x.Trim()).ToArray();
                if (parts.Length == 2) {
                    tileTextAttributes[1].InnerText = parts[0];
                    tileTextAttributes[2].InnerText = parts[1];
                }
                else {
                    tileTextAttributes[1].InnerText = location;
                }
                var notification = new TileNotification(xml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            }
            catch (Exception) {
            }

            try {
                var xml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideText01);
                var tileTextAttributes = xml.GetElementsByTagName("text");
                tileTextAttributes[0].AppendChild(xml.CreateTextNode(string.Format("{0}%", chance)));
                tileTextAttributes[1].InnerText = location;
                var notification = new TileNotification(xml);
                TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            }
            catch (Exception) {
            }

            //try {
            //    var xml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            //    var badge = (XmlElement) xml.SelectSingleNode("/badge");
            //    badge.SetAttribute("value", string.Format("{0}", chance));
            //    var notification = new BadgeNotification(xml);
            //    BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(notification);
            //}
            //catch (Exception) {
            //}
        }
    }
}