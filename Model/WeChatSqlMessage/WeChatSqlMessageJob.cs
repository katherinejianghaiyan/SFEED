using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.WeChatSqlMessage
{
    public class WeChatSqlMessageJob: Table.TableData
    {
        public int ID { get; set; }
        public string JobName { get; set; }
        public string SQLGUID { get; set; }
        public string SQLName { get; set; }
        public string DataSQL { get; set; }
        public string KeyFields { get; set; }
        public string ParameterFields { get; set; }
        public string EmployeeIDField { get; set; }
        public string DailyStartTime { get; set; }
        public int RunWeek { get; set; }
        public int Status { get; set; }
        public int IsDel { get; set; }
    }
}
