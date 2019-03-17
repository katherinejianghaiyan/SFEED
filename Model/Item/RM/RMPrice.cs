using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class RMPrice: ItemPrice
    {
        public int Sort { get; set; }
        public int RecordID { get; set; }
        public string UOMName { get; set; }
        public string SupplierGuid { get; set; }
    }
}
