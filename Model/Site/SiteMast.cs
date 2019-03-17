using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Site
{
    public class SiteMast: Table.TableData
    {
        public string SiteGuid { get; set; }
        public string SiteCode { get; set; }
        public string SiteName { get; set; }
        public string NameCn { get; set; }
        public string NameEn { get; set; }
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string TelNbr { get; set; }
        public string BUGuid { get; set; }
        public bool needWork { get; set; }
    }
}
