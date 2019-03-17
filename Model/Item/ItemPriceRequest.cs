using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class ItemPriceRequest: ItemRequest
    {
        public string SupplierGuid { get; set; }
    }
}
