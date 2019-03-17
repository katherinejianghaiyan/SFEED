using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Promotion
{
    public class PromotionMast : Table.TableData
    {
        public int ID { get; set; }

        public string BUGuid { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public double MinOrderAmt { get; set; }

        public int MaxQty { get; set; }
    }
}
