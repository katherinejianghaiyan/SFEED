using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.WeChatSqlMessage
{
    public class WeChatSqlMessageJobLog
    {
        public int ID { get; set; }
        public string KeyCode { get; set; }
        public int RunDate { get; set; }
    }
}
