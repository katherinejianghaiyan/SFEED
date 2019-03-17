using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MRP
{
    public class MRPOrderItem: Table.TableData
    {
        public string LineGUID { get; set; }

        public string ItemGUID { get; set; }

        public string ItemCode { get; set; }

        public string ItemName { get; set; }

        public double Qty { get; set; }

        public double Price { get; set; }

        public string UOMGUID { get; set; }

        public string UOMName { get; set; }

        public string SupplierGUID { get; set; }

        public string SupplierName { get; set; }

        public List<MRPSODetail> SODetails { get; set; }
    }
}
