using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MRP
{
    public class MRPRequest: Common.BaseRequest
    {
        public string RequiredDate { get; set; }
    }
}
