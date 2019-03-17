using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class ItemRequest: Common.BaseRequest
    {
        public string Guid { get; set; }
        public string Type { get; set; }
        public string startDate { get; set; }
        public string weekDay { get; set; }
    }
}
