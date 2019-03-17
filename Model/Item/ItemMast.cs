using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class ItemMast : Table.TableData
    {
        public string ItemGuid { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public int ItemStatus { get; set; }
        public string ItemCreateTime { get; set; }
        public string ItemClassGuid { get; set; }
        public string ItemClassName { get; set; }
        public string ItemTips { get; set; }
        public string ItemName_CN { get; set; }
        public string ItemName_EN { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string ingredientTips_CN { get; set; }
        public string ingredientTips_EN { get; set; }
        public string weekday { get; set; }
        public string ingredientTips { get; set; }
        public string comments { get; set; }
        public string langCode { get; set; }
    }
}
