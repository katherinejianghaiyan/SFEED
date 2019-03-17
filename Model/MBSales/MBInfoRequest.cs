using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MBSales
{
    public class MBInfoRequest : Common.BaseRequest
    {
        public string StallEntity { get; set; }
        public string CardStatus { get; set; }
        public string CardType { get; set; }
        //public string KeyWords { get; set; }
    }
}
