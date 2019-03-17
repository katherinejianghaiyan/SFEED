using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class ItemPrice : ItemMast
    {
        public string BUGuid { get; set; }
        public double Price { get; set; }
        public int IsDel { get; set; }
        public string SiteGUID { get; set; }
    }
}
