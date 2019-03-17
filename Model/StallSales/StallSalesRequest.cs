using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.StallSales
{
    public class StallSalesRequest:Common.BaseRequest
    {
        public string StallEntity { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }

    }
}
