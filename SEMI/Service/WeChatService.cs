using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEMI.Service
{
    public class WeChatService : WeChat.Interface.IEntrance
    {
        private static WeChatService instance = new WeChatService();
        private WeChatService() { }
        public static WeChatService GetInstance() { return instance; }
        public string ReturnMessage(System.Xml.XmlDocument xml)
        {
            try
            {
                WeChat.Models.RequestMessage message = WeChat.Service.CorpReplyMessageHelper.GetRequestMessage(xml);
                switch (message.Type)
                {
                    default: return WeChat.Service.CorpReplyMessageHelper.GetTextMessage(message.FromUser, message.ToUser, "回复功能正在开发,谢谢!/::)");
                }
            }
            catch { return string.Empty; }
        }
    }
}
