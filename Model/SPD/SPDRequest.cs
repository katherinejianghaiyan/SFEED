using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.SPD
{
    public class SPDRequest
    {
        public string empCode { get; set; }
        public string startdate { get; set; }
        public string enddate { get; set; }
        public string all { get; set; }
        public string group { get; set; }

        public string getsite { get; set; }

    }
}
