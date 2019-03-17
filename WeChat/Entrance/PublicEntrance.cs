using System;
using System.Web;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using Utils.Common;

namespace WeChat.Entrance
{
    public static class PublicEntrance
    {
        private static string _appId = ConfigurationManager.AppSettings["WXAppId"].GetString();

        private static string _appToken = ConfigurationManager.AppSettings["WXAppToken"].GetString();

        private static string _appAESKey = ConfigurationManager.AppSettings["WXAppAESKey"].GetString();

        public static void CallBackResponse(HttpContextBase context, WeChat.Interface.IEntrance service)
        {
            try
            {
                string signature = HttpUtility.UrlDecode(context.Request.QueryString["signature"]);

                string timeStamp = HttpUtility.UrlDecode(context.Request.QueryString["timestamp"]);

                string nonce = HttpUtility.UrlDecode(context.Request.QueryString["nonce"]);

                if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(nonce) || string.IsNullOrWhiteSpace(timeStamp)) return;

                Tencent.WXBizMsgCrypt wxcpt = new Tencent.WXBizMsgCrypt(_appToken, _appAESKey, _appId, false);

                if (context.Request.HttpMethod.ToUpper().Equals("GET")) //微信服务号请求回调地址时候响应
                {
                    if (wxcpt.CheckSignature(signature, _appToken, timeStamp, nonce))
                        context.Response.Output.Write(HttpUtility.UrlDecode(context.Request.QueryString["echostr"]));
                }
                if (context.Request.HttpMethod.ToUpper().Equals("POST")) //用户在微信中请求事件时响应
                {
                    string postData = string.Empty;
                    using (Stream stream = context.Request.InputStream)
                    {
                        Byte[] postBytes = new Byte[stream.Length];
                        stream.Read(postBytes, 0, (Int32)stream.Length);
                        postData = Encoding.UTF8.GetString(postBytes);
                    }
                    string msg = string.Empty;
                    if(wxcpt.DecryptMsg(signature, timeStamp, nonce, postData, ref msg) == 0)
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(msg);
                        string returnString = service.ReturnMessage(xmlDoc);
                        if (!string.IsNullOrWhiteSpace(returnString))
                        {
                            string encryptString = string.Empty;
                            if(wxcpt.EncryptMsg(returnString, timeStamp, nonce, ref encryptString) == 0)
                                context.Response.Output.Write(encryptString);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
