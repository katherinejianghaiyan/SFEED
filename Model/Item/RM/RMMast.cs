using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class RMMast : ItemMast
    {
        public string ItemSpec { get; set; }

        public string ItemUnitGuid { get; set; }

        public string ItemUnit { get; set; }

        public int ItemLoss { get; set; }

        public int ItemSell { get; set; }

        public string ItemClassCode { get; set; }

        public string PurchasePolicy { get; set; }
    }
}
