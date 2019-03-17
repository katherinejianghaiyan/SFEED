using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Supplier
{
    public class SupplierRequest: Common.BaseRequest
    {
        public DateTime ReceiptDate { get; set; }
    }
}
