using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class SaleOrder: Table.TableData
    {
        public int OrderID { get; set; }
        public string HeadGUID { get; set; }
        public string OrderCode { get; set; }
        public int UserID { get; set; }
        public string OrderTime { get; set; }
        public int OrderDate { get; set; }
        public string RequiredDate { get; set; }
        public string DeliveryEndTime { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaymentAmount { get; set; }
        public string PaidTime { get; set; }
        public string ShippedDate { get; set; }
        public string WorkedDate { get; set; }
        public int ShippedUser { get; set; }
        public string PaymentMethod { get; set; }
        public string UserName { get; set; }
        public string WechatID { get; set; }
        public string Status { get; set; }
        public string checkbox { get; set; }
        public string RequiredDinnerType { get; set; }
        public List<SaleLine> Lines { get; set; }
        public string comments { get; set; }
        public string mobile { get; set; }
        public string department { get; set; }
        public string section { get; set; }
        public string shipToAddr { get; set; }
        public int dataLength { get; set; }
        public string orderGuid { get; set; }
    }
}
