using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using WeChat.Interface;
using Utils.Common;

namespace WeChatEvent
{
    public class Service: IEntrance
    {
        private static Service instance = new Service();
        private Service() { }
        public static Service GetInstance() { return instance; }
        public string ReturnMessage(System.Xml.XmlDocument xml)
        {
            try
            {
                WeChat.Models.RequestMessage message = WeChat.Service.CorpReplyMessageHelper.GetRequestMessage(xml);
                switch (message.Type)
                {
                    case WeChat.Models.MessageType.EVENT:
                    {
                        string result = PersonPicHelper.Signed(message.FromUser, message.EventKey);
                        switch(result)
                        {
                            case "matched": return WeChat.Service.CorpReplyMessageHelper.GetTextMessage(message.FromUser, message.ToUser, "签到成功! /::)");
                            case "ok": return WeChat.Service.CorpReplyMessageHelper.GetTextMessage(message.FromUser, message.ToUser, "签到成功,恭喜您可以参与抽奖! /::)");
                            default: return WeChat.Service.CorpReplyMessageHelper.GetTextMessage(message.FromUser, message.ToUser, "回复任意信息,进行用户绑定! /::)");
                        }
                    }
                    default: 
                    {
                        List<WeChat.Models.News> newsList = new List<WeChat.Models.News>();
                        newsList.Add(new WeChat.Models.News()
                        {
                            title = "ECR Staff Party 2017",
                            description = "ECR年会抽奖绑定",
                            url = ConfigurationManager.AppSettings["LinkedUrl"].GetString() + "/wechat/binding/?key=" 
                                + Utils.Common.EncyptHelper.Encypt(message.FromUser)
                        });
                        return WeChat.Service.CorpReplyMessageHelper.GetNewsMessage(message.FromUser, message.ToUser, newsList);
                    }   
                }
            }
            catch { return string.Empty; }
        }
    }
}
