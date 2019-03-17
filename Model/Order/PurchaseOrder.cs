using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class PurchaseOrder: Table.TableData
    {
        public string OrderGuid { get; set; }
        public string SiteGuid { get; set; }
        public string OrderCode { get; set; }
        public string OrderDate { get; set; }
        public string SupplierGuid { get; set; }
        public string SupplierName { get; set; }
        public string RequiredDate { get; set; }
        public double OrderAmt { get; set; }
        public int Status { get; set; }
        public List<PurchaseOrderLine> Lines { get; set; }
    }
}
