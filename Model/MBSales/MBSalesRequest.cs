using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MBSales
{
    public class MBSalesRequest:Common.BaseRequest
    {
        public string StallEntity { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string Title { get; set; }
        public string All { get; set; }

    }
}
