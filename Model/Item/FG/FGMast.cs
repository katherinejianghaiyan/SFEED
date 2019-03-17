using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Item
{
    public class FGMast: ItemMast
    {
        public int ItemBuy { get; set; }
        public string ItemDishSize { get; set; }
        public string ItemContainer { get; set; }
        public string ItemCooking { get; set; }
        public string ItemNutrition { get; set; }
        public int ItemSales { get; set; }
        public double ItemScore { get; set; }
        public int ItemSort { get; set; }
        public int ItemWeight { get; set; }
        public string Image1 { get; set; }
        public string ImageName1 { get; set; }
        public string Image2 { get; set; }
        public string ImageName2 { get; set; }
        public string Image3 { get; set; }
        public string ImageName3 { get; set; }
        public double OtherCost { get; set; }     
        public List<ItemPropery> ItemProperies { get; set; }
    }
}
