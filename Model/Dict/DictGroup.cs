using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Dict
{
    public class DictGroup
    {
        public string DictType { get; set; }
        public List<DictDetail> Details { get; set; }
    }
}
