using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;

namespace Model.WeChatSqlMessage
{
    public class WeChatReportDisplayData
    {
        public List<string> TitleList { get; set; }
        public DataTable Data { get; set; }
        public string LinkedField { get; set; }
        public string Type { get; set; }
        public string ChildGuid { get; set; }
    }
}
