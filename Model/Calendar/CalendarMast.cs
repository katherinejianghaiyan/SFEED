using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Calendar
{
    public class CalendarMast : Table.TableData
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string BUGuid { get; set; }
        public string BUName { get; set; }
        public string SiteGuid { get; set; }
        public string SiteCode { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }
        public string compNameCn { get; set; }
        public string compNameEn { get; set; }
        public int Working { get; set; }
        public int IsDel { get; set; }
    }
}
