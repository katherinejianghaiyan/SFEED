using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class FGCostPrice: FGMast
    {
        public double ItemActCost { get; set; }
        public double ItemPreviousActCost { get; set; }
        public double ItemActGMRate { get; set; }
        public double ItemPreviousActGMRate { get; set; }
        public double ItemPrice { get; set; }
        public double ItemPromotionPrice { get; set; }
    }
}
