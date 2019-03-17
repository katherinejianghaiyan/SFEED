using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class ItemClass: Table.TableData
    {
        public string CLassGUID { get; set; }
        public string ClassName { get; set; }
        public int Sort { get; set; }
    }
}
