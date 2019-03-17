using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Common
{
    public class BaseRequest
    {
        public string UserGuid { get; set; }
        public string SiteGuid { get; set; }
        public string BUGuid { get; set; }
        public string KeyWords { get; set; }
        public int Status { get; set; }
        public string itemClass { get; set; }
    }
}
