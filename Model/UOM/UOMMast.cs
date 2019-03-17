using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.UOM
{
    public class UOMMast
    {
        public string UOMGuid { get; set; }
        public string UOMName { get; set; }
        public string BaseUOMGuid { get; set; }
        public string BaseUOMName { get; set; }
        public double BaseQty { get; set; }
    }
}
