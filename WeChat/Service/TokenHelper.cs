using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Utils.Common;
using Utils.Net;


namespace WeChat.Service
{
    public class TokenHelper
    {
        private static WeChat.Models.AccessToken _accessToken = null;

        private static TokenHelper instance = new TokenHelper();
        private TokenHelper()
        {
            SetAccessToken();
        }
        public static TokenHelper GetInstance() { return instance; }

        private void SetAccessToken()
        {
            string url = "https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid="
                           + ConfigurationManager.AppSettings["CorpId"].GetString() + "&corpsecret="
                           + ConfigurationManager.AppSettings["CorpSecret"].GetString();
            string token = new HttpHelper().Get(url);
            if (string.IsNullOrWhiteSpace(token)) throw new Exception("Request access token failed.");
            _accessToken = JsonHelper.DeSerializerJsonString<WeChat.Models.AccessToken>(token);
            _accessToken.date = DateTime.Now;
        }

        public WeChat.Models.AccessToken GetAccessToken()
        {
            try
            {
                if (_accessToken.date.AddSeconds(-60).AddSeconds(int.Parse(_accessToken.expires_in)) < DateTime.Now) SetAccessToken();
                return _accessToken;
            }
            catch { return null; }
        }
    }
}
