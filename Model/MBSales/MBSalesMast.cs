using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MBSales
{
    public class MBSalesMast:Table.TableData
    {

        public string CounterNo { get; set; }
        public string CounterName { get; set; }
        public string date { get; set; }
        public string flowno { get; set; }
        public string cardNo { get; set; }
        public string mbName { get; set; }
        public string TypeName { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string operDate { get; set; }
        public string Time { get; set; }
        public string pluno { get; set; }
        public string foodname { get; set; }
        public string salePrice { get; set; }
        public string saleQty { get; set; }
        public string saleAmt { get; set; }
        public string DiscAmt { get; set; }
    }
}
