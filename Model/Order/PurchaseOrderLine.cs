using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class PurchaseOrderLine
    {
        public string LineGUID { get; set; }
        public string ItemGUID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public double Qty { get; set; }
        public double Amt { get; set; }
        public double ReceiptQty { get; set; }
        public double ReceiptAmt { get; set; }
        public string UOMGUID { get; set; }
        public string UOMName { get; set; }
    }
}
