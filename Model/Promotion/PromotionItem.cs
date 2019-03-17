using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Promotion
{
    public class PromotionItem : Table.TableData
    {
        public int ID { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemGuid { get; set; }
        public double Price { get; set; }
        public string PromotionGuid { get; set; }
    }
}
