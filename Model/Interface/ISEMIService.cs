using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading.Tasks;

namespace Model.Interface
{
    [ServiceContract(Namespace = "Model.Interface", Name = "ISEMIService")]
    public interface ISEMIService
    {
        /// <summary>
        /// 客户端发起请求,同步Item主数据
        /// </summary>
        /// <param name="timeStamp">时间戳,用户数据加密和解密</param>
        /// <param name="identity">身份验证</param>
        /// <param name="siteGuid">营运点身份GUID</param>
        /// <returns>服务端返回加密后的Item主数据清单序列化字符串</returns>
        [OperationContract]
        string GetItems(string timeStamp, string identity, string siteGuid);

        [OperationContract]
        string GetBOMs(string timeStamp, string identity, string siteGuid);

        [OperationContract]
        string GetUOMs(string timeStamp, string identity, string siteGuid);

        /// <summary>
        /// 客户端发起请求,同步营运点用户主数据
        /// </summary>
        /// <param name="timeStamp">时间戳,用户数据加密和解密</param>
        /// <param name="identity">身份验证</param>
        /// <param name="siteGuid">营运点身份GUID</param>
        /// <returns>服务端返回加密后的用户主数据清单序列化字符串</returns>
        [OperationContract]
        string GetSiteUsers(string timeStamp, string identity, string siteGuid);

        /// <summary>
        /// 客户端发起请求,同步营运点销售订单主数据
        /// </summary>
        /// <param name="timeStamp">时间戳,用户数据加密和解密</param>
        /// <param name="identity">身份验证</param>
        /// <param name="siteGuid">客户端Guid</param>
        /// <returns>服务端返回加密后的销售订单数据清单序列化字符串</returns>
        [OperationContract]
        string GetSalesOrders(string timeStamp, string identity, string siteGuid);

        [OperationContract(IsOneWay=true)]
        void UpdateSiteUsers(string timeStamp, string identity, string siteGuid, string idString);

        [OperationContract(IsOneWay = true)]
        void UpdateSaleOrders(string timeStamp, string identity, string siteGuid, string data);
    }
}
