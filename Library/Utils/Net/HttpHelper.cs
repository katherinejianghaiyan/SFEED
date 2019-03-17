using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace Utils.Net
{
    public class HttpHelper
    {
        private string contentType = string.Empty;
        public HttpHelper() : this("application/x-www-form-urlencoded") { }

        public HttpHelper(string contentType)
        {
            this.contentType = contentType;
        }

        /// <summary>
        /// Post请求获取数据
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="requestData">Post数据</param>
        /// <returns>响应</returns>
        public string Post(string url, string requestData)
        {
            return Request(url, string.Empty, string.Empty, string.Empty, requestData, Encoding.UTF8, HttpRequestType.POST);
        }

        /// <summary>
        /// Post请求获取数据
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="requestData">Post数据</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>响应</returns>
        public string Post(string url, string requestData, Encoding encoding)
        {
            return Request(url, string.Empty, string.Empty, string.Empty, requestData, encoding, HttpRequestType.POST);
        }

        /// <summary>
        /// Get请求获取数据
        /// </summary>
        /// <param name="url">地址</param>
        /// <returns>响应</returns>
        public string Get(string url)
        {
            return Request(url, string.Empty, string.Empty, string.Empty, string.Empty, Encoding.UTF8, HttpRequestType.GET);
        }

        /// <summary>
        /// Get请求获取数据
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="encoding">编码方式</param>
        /// <returns>响应</returns>
        public string Get(string url, Encoding encoding)
        {
            return Request(url, string.Empty, string.Empty, string.Empty, string.Empty, encoding, HttpRequestType.GET);
        }

        /// <summary>
        /// 请求URL,并返回响应数据
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="domain">域名</param>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="requestData">请求数据,请求方式为GET时,此字段无效</param>
        /// <param name="encoding">编码方式</param>
        /// <param name="type">请求方式</param>
        /// <returns>响应数据</returns>
        public string Request(string url, string domain, string userName, string password, string requestData,
            Encoding encoding, HttpRequestType type)
        {
            WebRequest request = HttpWebRequest.Create(HttpUtility.UrlDecode(url));
            if (!userName.Equals(string.Empty) && !password.Equals(string.Empty))
            {
                request.Credentials = new System.Net.NetworkCredential(userName, password, domain);
                request.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            }
            request.ContentType = this.contentType;
            if (!type.Equals(HttpRequestType.GET))
            {
                request.Method = type.ToString();
                byte[] postBytes = encoding.GetBytes(requestData);
                request.ContentLength = postBytes.Length;
                using (Stream outstream = request.GetRequestStream()) { outstream.Write(postBytes, 0, postBytes.Length); }
            }
            else request.Method = HttpRequestType.GET.ToString();
            string result = string.Empty;
            using (WebResponse response = request.GetResponse())
            {
                if (response != null)
                {
                    Stream stream = response.GetResponseStream();
                    StreamReader sr = new StreamReader(stream, encoding);
                    result = sr.ReadToEnd();
                }
            }
            return result;
        }
    }
}