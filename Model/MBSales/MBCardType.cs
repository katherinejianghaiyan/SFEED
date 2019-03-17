using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MBSales
{
    public class MBCardType:Table.TableData
    {
        public string Code { get; set; }

        public string TypeName { get; set; }
    }
}
