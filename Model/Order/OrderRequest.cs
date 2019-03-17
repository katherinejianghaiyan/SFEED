using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Order
{
    public class OrderRequest: Common.BaseRequest
    {
        public string headGuid { get; set; }
        public string maxOrderId { get; set; }
        public string orderDate { get; set; }
        public string orderStatus { get; set; }
        public string user { get; set; }
        public string orderCode { get; set; }
    }
}
