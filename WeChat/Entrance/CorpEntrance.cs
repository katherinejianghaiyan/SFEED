using System;
using System.Web;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Utils.Common;

namespace WeChat.Entrance
{
    public class CorpEntrance
    {
        private static string _corpId = ConfigurationManager.AppSettings["CorpId"].GetString();

        private static string _secretKey = ConfigurationManager.AppSettings["CorpSecret"].GetString();

        private static CorpEntrance instance = new CorpEntrance();
        private CorpEntrance() { }
        public static CorpEntrance GetInstance() { return instance; }

        /// <summary>
        /// 接收微信传递的回调
        /// </summary>
        public void CallBackResponse(HttpContextBase context, string token, string encodingAESKey, WeChat.Interface.IEntrance service)
        {
            try
            {
                string signature = HttpUtility.UrlDecode(context.Request.QueryString["msg_signature"]);
                string timeStamp = HttpUtility.UrlDecode(context.Request.QueryString["timestamp"]);
                string nonce = HttpUtility.UrlDecode(context.Request.QueryString["nonce"]);
                if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(nonce) || string.IsNullOrWhiteSpace(timeStamp)) return;
                int ret = 0; //返回状态
                Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(token, encodingAESKey, _corpId, true);
                if (context.Request.HttpMethod.ToUpper().Equals("GET")) //企业号应用绑定,微信请求回调地址时候响应
                {
                    string echoStr = HttpUtility.UrlDecode(context.Request.QueryString["echostr"]);
                    string retStr = string.Empty;
                    ret = wxcpt.VerifyURL(signature, timeStamp, nonce, echoStr, ref retStr);
                    if (ret == 0) context.Response.Output.Write(retStr);
                }
                if (context.Request.HttpMethod.ToUpper().Equals("POST")) //企业号用户在微信中请求事件时响应
                {
                    string postData = string.Empty;
                    using (Stream stream = context.Request.InputStream)
                    {
                        Byte[] postBytes = new Byte[stream.Length];
                        stream.Read(postBytes, 0, (Int32)stream.Length);
                        postData = Encoding.UTF8.GetString(postBytes);
                    }
                    string msg = string.Empty;
                    ret = wxcpt.DecryptMsg(signature, timeStamp, nonce, postData, ref msg);
                    if (ret == 0) //微信发送的xml字符串信息解密正确
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(msg);
                        string returnString = service.ReturnMessage(xmlDoc);
                        if (!string.IsNullOrWhiteSpace(returnString))
                        {
                            string encryptString = string.Empty;
                            ret = wxcpt.EncryptMsg(returnString, timeStamp, nonce, ref encryptString);
                            if (ret == 0) context.Response.Output.Write(encryptString);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
