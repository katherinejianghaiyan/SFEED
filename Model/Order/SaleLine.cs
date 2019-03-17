using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class SaleLine : Order.SaleOrder
    {
        public string LineGUID { get; set; }
        public string ItemGUID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public double Amt { get; set; }

        public int Qty { get; set; }

        public decimal Price { get; set; }

        public int IsPrint { get; set; }
        public string UOMName { get; set; }
        public string C { get; set; }

        public string ItemName_EN { get; set; }

        public string ItemName_CN { get; set; }

        public string ClassName { get; set; }
    }
}
