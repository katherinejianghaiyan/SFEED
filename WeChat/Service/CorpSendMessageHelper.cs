using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Common;
using Utils.Net;

namespace WeChat.Service
{
    public class CorpSendMessageHelper
    {
        private static string textMessageApiUrl = string.Empty;

        private static CorpSendMessageHelper instance = new CorpSendMessageHelper();
        private CorpSendMessageHelper()
        { 
            textMessageApiUrl = "https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={0}";
        }
        public static CorpSendMessageHelper GetInstance() { return instance; }

        public bool CheckTextMessage(string textMessage)
        {
            try
            {
                if (Encoding.UTF8.GetBytes(textMessage).Length >= 2048) return false;
                return true;
            }
            catch { return false; }
        }

        public void SendMessage(WeChat.Models.Message message)
        {
            if (message == null) return;
            if (!string.IsNullOrWhiteSpace(message.TextMessage))
                SendTextMessage(message.UserIds, message.PartyIds, message.TagIds, message.TextMessage, message.AgentId, null);
            if (message.NewsMessage != null && message.NewsMessage.Count > 0)
                SendNewsMessage(message.UserIds, message.PartyIds, message.TagIds, message.NewsMessage, message.AgentId, null);
        }

        /// <summary>
        /// 程序主动调用微信Api发送文本信息
        /// </summary>
        /// <param name="userIds">@all表示所有接收用户，多个用户使用|分隔</param>
        /// <param name="partyIds">@all表示所有接收部门，多个部门使用|分隔,当usrIds为@all时候，字段失效</param>
        /// <param name="tagIds">@all表示所有接收标签，多个标签使用|分隔,当usrIds为@all时候，字段失效</param>
        /// <param name="textMessage">文本信息主体,不能大于2048字节,大约682中文字</param>
        /// <param name="callBack">操作执行成功后的回调函数</param>
        /// <param name="agentId">企业应用Id</param>
        public void SendTextMessage(string userIds, string partyIds, string tagIds, string textMessage, 
            int agentId, Action<WeChat.Models.ResultToken> callBack)
        {
            try
            {
                string jsonString = JsonHelper.SerializeJsonString(SetText(userIds, partyIds, tagIds, textMessage, agentId));
                if (string.IsNullOrWhiteSpace(jsonString)) throw new Exception("发送文本为空,或者转换JSON失败");
                lock(this)
                {
                    WeChat.Models.AccessToken token = TokenHelper.GetInstance().GetAccessToken();
                    if (token == null) throw new Exception("获取AccessToken失败,请检查配置信息");
                    string responseString = new HttpHelper().Post(string.Format(textMessageApiUrl, 
                        token.access_token), jsonString);
                    if (callBack != null) callBack(JsonHelper.DeSerializerJsonString<WeChat.Models.ResultToken>(responseString));
                }            
            }
            catch(Exception e)
            {
                if (callBack != null) callBack(new Models.ResultToken() { errcode = "error", errmsg = e.Message });
            }
        }

        public void SendNewsMessage(string userIds, string partyIds, string tagIds, List<WeChat.Models.News> newsMessage,
            int agentId, Action<WeChat.Models.ResultToken> callBack)
        {
            try
            {
                string jsonString = JsonHelper.SerializeJsonString(SetNews(userIds, partyIds, tagIds, newsMessage, agentId));
                if (string.IsNullOrWhiteSpace(jsonString)) throw new Exception("发送文本为空,或者转换JSON失败");
                lock (this)
                {
                    WeChat.Models.AccessToken token = TokenHelper.GetInstance().GetAccessToken();
                    if (token == null) throw new Exception("获取AccessToken失败,请检查配置信息");
                    string responseString = new HttpHelper().Post(string.Format(textMessageApiUrl,
                        token.access_token), jsonString);
                    if (callBack != null) callBack(JsonHelper.DeSerializerJsonString<WeChat.Models.ResultToken>(responseString));
                }
            }
            catch (Exception e)
            {
                if (callBack != null) callBack(new Models.ResultToken() { errcode = "error", errmsg = e.Message });
            }
        }

        /// <summary>
        /// 生成发送的微信消息,注意当usrIds,partyIds,tagIds都存在的时候,微信将根据交集实现,即三个条件都需满足
        /// </summary>
        /// <param name="usrIds">@all表示所有用户，多个用户使用|分隔</param>
        /// <param name="partyIds">@all表示所有部门，多个部门使用|分隔,当usrIds为@all时候，字段失效</param>
        /// <param name="tagIds">@all表示所有标签，多个标签使用|分隔,当usrIds为@all时候，字段失效</param>
        /// <param name="agentId">企业微信应用id</param>
        /// <param name="content">发送的内容,不能超过2048字节</param>
        private WeChat.Models.CorpTextContext SetText(string userIds, string partyIds, 
            string tagIds, string content, int agentId)
        {
            WeChat.Models.CorpTextContext context = new Models.CorpTextContext();
            context.msgtype = "text";
            context.text = new Models.CorpText();
            context.text.content = content;
            context.agentid = agentId;
            context.touser = userIds;
            context.toparty = partyIds;
            context.totag = tagIds;
            return context;
        }

        private WeChat.Models.CorpNewsContext SetNews(string userIds, string partyIds,
            string tagIds, List<WeChat.Models.News> newsList, int agentId)
        {
            WeChat.Models.CorpNewsContext context = new Models.CorpNewsContext();
            context.msgtype = "news";
            context.agentid = agentId;
            context.touser = userIds;
            context.toparty = partyIds;
            context.totag = tagIds;
            context.news = new Models.CorpNews();
            context.news.articles = newsList;
            return context;
        }

    }
}
