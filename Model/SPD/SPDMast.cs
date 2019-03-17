using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.SPD
{
    public class SPDMast : Table.TableData
    {
        public string code { get; set; }
        public string Year { get; set; }
        public string Month { get; set; }
        public string Date { get; set; }
        public string Supplier { get; set; }
        public string Consumer { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Qty { get; set; }
        public string Turnover { get; set; }
        public string NetTurnover { get; set; }
        public string Cost { get; set; }
        public string GM { get; set; }
        public string NetGM { get; set; }
        public string Sql { get; set; }
    }
}
