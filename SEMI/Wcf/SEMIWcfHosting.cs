using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.ServiceModel;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Wcf
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public class SEMIWcfHosting 
    {
        private ServiceHost host = null; //主机
        private Uri uri = null; //主机IP地址与端口

        public SEMIWcfHosting()
        {
            string uriString = ConfigurationManager.AppSettings["HostUri"].GetString();
            if (!string.IsNullOrWhiteSpace(uriString)) uri = new Uri(uriString);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uriString">uri参数中必须包含IP和Port</param>
        public SEMIWcfHosting(string uriString)
        {
            if (!string.IsNullOrWhiteSpace(uriString)) uri = new Uri(uriString);
        }

        public void SetUri(string uriString)
        {
            if (!string.IsNullOrWhiteSpace(uriString)) uri = new Uri(uriString);
        }

        /// <summary>
        /// 启动WCF服务监听
        /// </summary>
        /// <param name="serviceType">服务对象类型</param>
        public void Start(Type serviceType)
        {
            host = new ServiceHost(serviceType, uri);
            host.Open();
        }

        public void Stop()
        {
            if(host != null)
            {
                try { host.Close(); }
                catch { }
                host = null;
            }
        }

        public string GetIp()
        {
            if (uri != null) return uri.Host;
            else return string.Empty;
        }

        public string GetPort()
        {
            if (uri != null) return uri.Port.ToString();
            else return string.Empty;
        }

    }
}
