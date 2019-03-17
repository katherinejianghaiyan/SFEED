using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class ReceiptRequest
    {
        public string SiteGuid { get; set; }
        public string SupplierGuid { get; set; }
        public DateTime ReceiptDate { get; set; }
    }
}
