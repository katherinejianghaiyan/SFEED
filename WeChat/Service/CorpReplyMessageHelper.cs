using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using Utils.Common;

namespace WeChat.Service
{
    public static class CorpReplyMessageHelper
    {
        public static WeChat.Models.RequestMessage GetRequestMessage(XmlDocument xml)
        {
            try
            {
                WeChat.Models.RequestMessage message = new Models.RequestMessage();
                message.ToUser = xml.DocumentElement.SelectSingleNode("ToUserName").InnerText.GetString();
                message.FromUser = xml.DocumentElement.SelectSingleNode("FromUserName").InnerText.GetString();
                message.Type = (WeChat.Models.MessageType)Enum.Parse(typeof(WeChat.Models.MessageType),
                    xml.DocumentElement.SelectSingleNode("MsgType").InnerText.GetString().ToUpper());
                switch (message.Type)
                {
                    case Models.MessageType.TEXT: message.Content = xml.DocumentElement
                        .SelectSingleNode("Content").InnerText.GetString();break;
                    case Models.MessageType.EVENT:
                        {
                            message.Event = xml.DocumentElement.SelectSingleNode("Event").InnerText.GetString();
                            message.EventKey = xml.DocumentElement.SelectSingleNode("EventKey").InnerText.GetString();
                            message.EventName = xml.DocumentElement.SelectSingleNode("Event").InnerText.GetString();
                            break;
                        }
                    case Models.MessageType.IMAGE:
                    case Models.MessageType.LINK:
                    case Models.MessageType.LOCATION: break;
                }
                return message;
            }
            catch { return null; }
        }
        public static string GetTextMessage(string toUser, string fromUser, string content)
        {
            StringBuilder returnStrings = new StringBuilder();
            returnStrings.Append("<xml><ToUserName><![CDATA[").Append(toUser).Append("]]></ToUserName>")
                .Append("<FromUserName><![CDATA[").Append(fromUser).Append("]]></FromUserName>")
                .Append("<CreateTime>").Append(DateTime.Now.Ticks).Append("</CreateTime>")
                .Append("<MsgType><![CDATA[text]]></MsgType><Content><![CDATA[").Append(content)
                .Append("]]></Content></xml>");
            return returnStrings.ToString();
        }
        public static string GetNewsMessage(string toUser, string fromUser, List<WeChat.Models.News> newsList)
        {
            if(newsList == null || newsList.Count == 0) return string.Empty;
            StringBuilder returnStrings = new StringBuilder();
            returnStrings.Append("<xml><ToUserName><![CDATA[").Append(toUser).Append("]]></ToUserName>")
                .Append("<FromUserName><![CDATA[").Append(fromUser).Append("]]></FromUserName>")
                .Append("<CreateTime>").Append(DateTime.Now.Ticks).Append("</CreateTime>")
                .Append("<MsgType><![CDATA[news]]></MsgType>").Append("<ArticleCount>")
                .Append(newsList.Count).Append("</ArticleCount><Articles>");
            foreach (WeChat.Models.News news in newsList)
            {
                returnStrings.Append("<item><Title><![CDATA[").Append(news.title).Append("]]></Title>")
                    .Append("<Description><![CDATA[").Append(news.description).Append("]]></Description>")
                    .Append("<PicUrl><![CDATA[").Append(news.picurl).Append("]]></PicUrl>")
                    .Append("<Url><![CDATA[").Append(news.url).Append("]]></Url></item>");
            }
            return returnStrings.Append("</Articles></xml>").ToString();
        }
    }
}
