using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MRP
{
    public class MRPOrder
    {
        public string GUID { get; set; }
        public string SiteGUID { get; set; }
        public string RequiredDate { get; set; }
        public List<MRPOrderItem> Lines { get; set; }
    }
}
