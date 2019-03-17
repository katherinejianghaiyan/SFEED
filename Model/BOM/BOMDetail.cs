using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.BOM
{
    public class BOMDetail
    {
        public int BOMID { get; set; }
        public string ItemGuid { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public string UOMGuid { get; set; }
        public string UOMName { get; set; }
        public double StdQty { get; set; }
        public double ActualQty { get; set; }
        public int Sort { get; set; }
        public double Price { get; set; }
        public double StdCost { get; set; }
        public double ActCost { get; set; }
        public double PreviousPrice { get; set; }
        public double PreviousStdCost { get; set; }
        public double PreviousActCost { get; set; }
        public string ItemName_CN { get; set; }
        public string ItemName_EN { get; set; }
    }
}
