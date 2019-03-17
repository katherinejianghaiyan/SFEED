using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEMI.Service
{
    public class SEMIService : Model.Interface.ISEMIService
    {
        public string GetItems(string timeStamp, string identity, string siteGuid)
        {
            return new ServerServiceHelper(timeStamp, identity).GetItems(siteGuid);
        }

        public string GetSiteUsers(string timeStamp, string identity, string siteGuid)
        {
            return new ServerServiceHelper(timeStamp, identity).GetSiteUsers(siteGuid);
        }

        public string GetSalesOrders(string timeStamp, string identity, string siteGuid)
        {
            return new ServerServiceHelper(timeStamp, identity).GetSaleOrders(siteGuid);
        }


        public void UpdateSiteUsers(string timeStamp, string identity, string siteGuid, string idString)
        {
            new ServerServiceHelper(timeStamp, identity).UpdateSiteUsers(siteGuid, idString);
        }


        public void UpdateSaleOrders(string timeStamp, string identity, string siteGuid, string data)
        {
            new ServerServiceHelper(timeStamp, identity).UpdateSaleOrders(siteGuid, data);
        }


        public string GetBOMs(string timeStamp, string identity, string siteGuid)
        {
            return new ServerServiceHelper(timeStamp, identity).GetBOMs(siteGuid);
        }


        public string GetUOMs(string timeStamp, string identity, string siteGuid)
        {
            return new ServerServiceHelper(timeStamp, identity).GetUOMs(siteGuid);
        }
    }
}
