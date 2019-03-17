using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class FGPrice: ItemPrice
    {
        public int Sort { get; set; }
        public int RecordID { get; set; }
        public string Container { get; set; }
    }
}
