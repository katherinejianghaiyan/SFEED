using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class ReceiptMast : Table.TableData
    {
        public string LineGuid { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemSpec { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public decimal Qty { get; set; }
        public decimal Amt { get; set; }
        public decimal ReceiptQty { get; set; }
        public decimal ReceiptAmt { get; set; }

        public string ItemName_CN { get; set; }

        public string ItemName_EN { get; set; }
    }
}
