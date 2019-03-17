using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BOM
{
    public class BOMMast
    {
        public string ProductGuid { get; set; }
        public List<BOMDetail> Details { get; set; }
    }
}
