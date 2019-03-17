using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.CustomerData
{
    public class CustomerDataMast: Table.TableData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Instruction { get; set; }
        public string SQL { get; set; }
        public int RunWeek { get; set; }
        public string Remark { get; set; }
        public int Status { get; set; }
        public int IsDel { get; set; }
    }
}
