using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BU
{
    public class BUMast: Table.TableData
    {
        public string BUGuid { get; set; }
        public string BUCode { get; set; }
        public string BUName { get; set; }
        public string EndTime { get; set; }
        public int TimeOut { get; set; }
        public string ParentName { get; set; }
        public string ParentGuid { get; set; }
    }
}
