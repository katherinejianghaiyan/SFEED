using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MRP
{
    public class MRPSODetail
    {
        public string SODetailGUID { get; set; }
        public string OrderCode { get; set; }
        public string ProductGUID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Qty { get; set; }
        public double UsedQty { get; set; }
        public double Price { get; set; }
    }
}
