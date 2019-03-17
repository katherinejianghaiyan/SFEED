using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.StallSales
{
    public class StallSalesMast:Table.TableData
    {
        public string CounterNo { get; set; }
        public string CounterName { get; set; }
        public string Date { get; set; }
        public string GrossAmt { get; set; }
        public string DiscountAmt { get; set; }
        public string SalesAmt { get; set; }
        public string TransactionQty { get; set; }
        public string SalesQty { get; set; }
        public string DealQty { get; set; }
        public string RefundQty { get; set; }
        public string RefundAmt { get; set; }
        
    }
}
