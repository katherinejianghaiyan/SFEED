using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Table
{
    public class TableMast<T> where T: TableData
    {
        public string errMsg { get; set; }
        public string user { get; set; }
        public int draw { get; set; }
        public int recordsTotal { get; set; }
        public int recordsFiltered { get; set; }
        public IList<T> data { get; set; }
        public string orderStatus { get; set; }
    }
}
