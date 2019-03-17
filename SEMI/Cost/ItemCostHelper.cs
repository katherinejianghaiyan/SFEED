using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Cost
{
    public class ItemCostHelper : Common.BaseDataHelper
    {
        private static ItemCostHelper instance = new ItemCostHelper();
        private ItemCostHelper() { }
        public static ItemCostHelper GetInstance() { return instance; }

        public Model.Table.TableMast<Model.Item.FGCostPrice> GetTableFGCostPriceList(string BUGuid, string keyWords, string language)
        {
            Model.Table.TableMast<Model.Item.FGCostPrice> tableMast = new Model.Table.TableMast<Model.Item.FGCostPrice>();
            tableMast.draw = 1;
            tableMast.data = GetFGCostPriceList(BUGuid, keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.FGCostPrice>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Item.FGCostPrice> GetFGCostPriceList(string BUGuid, string keyWords, string language)
        {
            try
            {
                DateTime date = DateTime.Now;
                string sql = "select a1.ItemGUID,a1.Price,a4.PromotedPrice,a3.ItemCode,a3.ItemName,a3.ItemNameZHCN as ItemName_CN,a3.ItemNameENUS as ItemName_EN,a3.ItemType,a3.Sort,a3.OtherCost from "
                    + "[dbo].[tblItemPrice] a1 join (select ItemGUID,min(" + date.ToString("yyyyMMdd")
                    + "-StartDate) MinDays from [dbo].[tblItemPrice] where PriceType='Sales' and BUGUID='" + BUGuid
                    + "' and StartDate<=" + date.ToString("yyyyMMdd") + "and isnull(EndDate,22991231)>=" + date.ToString("yyyyMMdd")
                    + " group by ItemGUID) a2 on a1.ItemGUID=a2.ItemGUID and a2.MinDays=(" + date.ToString("yyyyMMdd")
                    + "-a1.StartDate) join [dbo].[tblItem] a3 on a1.ItemGUID=a3.GUID and a3.ToSell=1 and a3.Status=1 left join ( "
                    + "select b1.ItemGUID,min(b1.Price) as PromotedPrice from [dbo].[tblPromotedItem] b1 join "
                    + "(select distinct p1.GUID from [dbo].[tblPromotion] p1 join (select BUGUID,min(" + date.ToString("yyyyMMdd")
                    + "-StartDate) MinDays from [dbo].[tblPromotion] where BUGUID='" + BUGuid + "' and StartDate<="
                    + date.ToString("yyyyMMdd") + "and isnull(EndDate,22991231)>=" + date.ToString("yyyyMMdd") + "group by BUGUID) "
                    + "p2 on p1.BUGUID=p2.BUGUID and p2.MinDays=(" + date.ToString("yyyyMMdd") + "-p1.StartDate)) b2 "
                    + "on b1.PromotionGUID=b2.GUID group by b1.ItemGUID) a4 on a1.ItemGUID=a4.ItemGUID where a1.PriceType='Sales' "
                    + "and a1.BUGUID='" + BUGuid + "' and a1.StartDate<=" + date.ToString("yyyyMMdd")
                    + " and isnull(a1.EndDate,22991231)>=" + date.ToString("yyyyMMdd")
                    + " and (a3.ItemCode like @search or a3.ItemName like @search) order by a3.ItemCode";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%"
                    + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "FGMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                List<Model.BOM.BOMMast> bomList = GetBOMCostList((string.IsNullOrWhiteSpace(keyWords) ? null :
                    data.AsEnumerable().Select(dr => dr.Field<string>("ItemGUID").GetString()).ToList()), BUGuid, date);
                if (bomList == null) bomList = new List<Model.BOM.BOMMast>();
                int i = 1;
                return (from dr in data.AsEnumerable()
                        join bom in bomList
                        on dr.Field<string>("ItemGUID").GetString() equals bom.ProductGuid into ldata
                        from lr in ldata.DefaultIfEmpty()
                        let PreviousActCost = lr == null ? 0 : lr.Details.Sum(l => l.PreviousActCost).ToDouble(4)
                        let ActCost = lr == null ? 0 : lr.Details.Sum(l => l.ActCost).ToDouble(4)
                        let OtherCost = dr.Field<decimal?>("OtherCost").ToDouble(2)
                        select new Model.Item.FGCostPrice()
                        {
                            ItemGuid = dr.Field<string>("ItemGUID").GetString(),
                            ItemCode = dr.Field<string>("ItemCode").GetString(),
                            ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                            ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                            ItemType = dr.Field<string>("ItemType").GetString(),
                            ItemPrice = dr.Field<decimal?>("Price").ToDouble(4),
                            OtherCost = OtherCost,
                            ItemPromotionPrice = dr.Field<decimal?>("PromotedPrice").ToDouble(4),
                            ItemSort = dr.Field<int?>("Sort").ToInt().Equals(0) ? i++ : dr.Field<int?>("Sort").ToInt(),
                            ItemActCost = ActCost,
                            ItemPreviousActCost = PreviousActCost,
                            ItemActGMRate = dr.Field<decimal?>("Price").ToDouble(4).Equals(0) ? 0
                            : ((dr.Field<decimal?>("Price").ToDouble(4) - ActCost - OtherCost)
                            / dr.Field<decimal?>("Price").ToDouble(4) * 100).ToDouble(2),
                            ItemPreviousActGMRate = dr.Field<decimal?>("Price").ToDouble(4).Equals(0) ? 0
                            : ((dr.Field<decimal?>("Price").ToDouble(4) - PreviousActCost - OtherCost)
                            / dr.Field<decimal?>("Price").ToDouble(4) * 100).ToDouble(2)
                        }).ToList();
            }
            catch { return null; }
        }

        /// <summary>
        /// 计算公司有效成品成本
        /// </summary>
        /// <param name="BUGuid">公司Guid</param>
        /// <param name="calDate">计算日期</param>
        public List<Model.BOM.BOMMast> GetBOMCostList(List<string> productGuids, string BUGuid, DateTime calDate)
        {
            try
            {
                StringBuilder itemSql = new StringBuilder("select b1.ItemGUID as ProductGUID,b2.ItemGUID,b3.ItemType,b2.StdQty,b2.ActQty,"
                    + "b2.UOMGUID,b3.ItemCode,b3.ItemName,b3.ItemNameZHCN as ItemName_CN,b3.ItemNameENUS as ItemName_EN,b3.PurUOMGUID,b5.ToQty,b3.ParentGUID,b4.PurUOMGUID as PPurUOMGUID "
                    + "from (select distinct a1.ItemGUID from [dbo].[tblItemPrice] a1,[dbo].[tblItem] a2 "
                    + "where a1.ItemGUID=a2.GUID and a2.ToSell=1 and a2.Status=1) b1 "
                    + "left join [dbo].[tblItemBOM] b2 on b1.ItemGUID=b2.ProductGUID join [dbo].[tblItem] b3 "
                    + "on b2.ItemGUID=b3.GUID and b3.ToBuy=1 and b3.Status=1 left join [dbo].[tblItem] b4 "
                    + "on b3.ParentGUID=b4.GUID and b4.ToBuy=1 and b4.Status=1 left join (select distinct a1.ItemGUID,a1.UOMGUID,"
                    + "a1.ToQty from tblItemUOMConv a1 join tblUOM a2 on a1.ToUOMGUID=a2.GUID and isnull(a2.ToUOMGUID,'')='' "
                    + "where a1.Active=1) b5 on b2.ItemGUID=b5.ItemGUID and b3.PurUOMGUID=b5.UOMGUID");
                StringBuilder priceSql = new StringBuilder("select c1.ItemGUID,c1.Price,c1.PriceStatus "
                    + "from (select b1.ItemGUID,max(b1.Price) as Price,'Previous' as PriceStatus from [dbo].[tblItemPrice] b1 join "
                    + "(select a1.SupplierGUID,a1.ItemGUID,max(a1.StartDate) StartDate from [dbo].[tblItemPrice] a1 "
                    + "join [dbo].[tblSupplierSite] a2 on a1.SupplierGUID=a2.SupplierGUID left join [dbo].[tblSite] a3 "
                    + "on a2.SiteGUID=a3.GUID where a1.PriceType='Buy' and (isnull(a3.BUGUID,'')='" + BUGuid + "' "
                    + "or isnull(a3.BUGUID,'')='" + BUGuid + "') and a1.StartDate<=" + calDate.ToString("yyyyMMdd")
                    + "and isnull(a1.EndDate,22991231)>=" + calDate.ToString("yyyyMMdd") + " group by a1.SupplierGUID,a1.ItemGUID) b2 "
                    + "on b1.SupplierGUID=b2.SupplierGUID and b1.ItemGUID=b2.ItemGUID and b1.StartDate=b2.StartDate "
                    + "group by b1.ItemGUID union select a1.ItemGUID,max(a1.Price) as Price,"
                    + "'Next' as PriceStatus from [dbo].[tblItemPrice] a1 join [dbo].[tblSupplierSite] a2 "
                    + "on a1.SupplierGUID=a2.SupplierGUID left join [dbo].[tblSite] a3 on a2.SiteGUID=a3.GUID "
                    + "where a1.PriceType='Buy' and (isnull(a3.BUGUID,'')='" + BUGuid + "' or isnull(a3.BUGUID,'')='"
                    + BUGuid + "') and a1.StartDate>" + calDate.ToString("yyyyMMdd")
                    + " group by a1.ItemGUID) c1,(select distinct a1.ItemGUID from [dbo].[tblItemBOM] a1,[dbo].[tblItem] a2 "
                    + "where a1.ItemGUID=a2.GUID");

                if (productGuids != null && productGuids.Count > 0)
                {
                    string pGuids = string.Join(",", productGuids.Select(p => "'" + p + "'").ToArray());
                    itemSql.Append(" where b1.ItemGUID in (").Append(pGuids).Append(")");
                    priceSql.Append(" and a1.ProductGUID in (").Append(pGuids).Append(")");
                }
                priceSql.Append(" union select distinct ParentGUID from [dbo].[tblItem] where isnull(ParentGUID,'')<>'') c2 "
                    + "where c1.ItemGUID=c2.ItemGUID");

                List<Model.BOM.BOMMast> bomList = GetCostList(itemSql.ToString(), priceSql.ToString());

                return bomList;
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("CalItemCost", "计算成品成本失败", BUGuid, e.Message);
                return null;
            }
        }

        public Model.BOM.BOMMast GetBOMCost(string BUGuid, string productGuid, DateTime calDate)
        {
            string itemSql = "select a1.ProductGUID,a1.ItemGUID,a2.ItemCode,a2.ItemName,a2.ItemNameZHCN as ItemName_CN,a2.ItemNameENUS as ItemName_EN,a2.ItemType,a2.PurUOMGUID,a1.StdQty,a1.ActQty,a1.UOMGUID,"
                + "a2.ParentGUID,a3.PurUOMGUID as PPurUOMGUID,a4.ToQty from [dbo].[tblItemBOM] a1 join [dbo].[tblItem] a2 "
                + "on a1.ItemGUID=a2.GUID and a2.status=1 left join [dbo].[tblItem] a3 on a2.ParentGUID=a3.GUID "
                + "and a3.ToBuy=1 and a3.status=1 left join (select distinct a1.ItemGUID,a1.UOMGUID,a1.ToQty from tblItemUOMConv a1 "
                + "join tblUOM a2 on a1.ToUOMGUID=a2.GUID and isnull(a2.ToUOMGUID,'')='' where a1.Active=1) a4 on a1.ItemGUID=a4.ItemGUID "
                + "and a2.PurUOMGUID=a4.UOMGUID where a1.ProductGUID='" + productGuid + "' order by a1.ID";

            string priceSql = "select c1.ItemGUID,c1.Price,c1.PriceStatus "
                   + "from (select b1.ItemGUID,max(b1.Price) as Price,'Previous' as PriceStatus from [dbo].[tblItemPrice] b1 join "
                   + "(select a1.SupplierGUID,a1.ItemGUID,max(a1.StartDate) StartDate from [dbo].[tblItemPrice] a1 "
                   + "join [dbo].[tblSupplierSite] a2 on a1.SupplierGUID=a2.SupplierGUID left join [dbo].[tblSite] a3 "
                   + "on a2.SiteGUID=a3.GUID where a1.PriceType='Buy' and (isnull(a3.BUGUID,'')='" + BUGuid + "' "
                   + "or isnull(a3.BUGUID,'')='" + BUGuid + "') and a1.StartDate<=" + calDate.ToString("yyyyMMdd")
                   + " and isnull(a1.EndDate,22991231)>=" + calDate.ToString("yyyyMMdd") + " group by a1.SupplierGUID,a1.ItemGUID) b2 "
                   + "on b1.SupplierGUID=b2.SupplierGUID and b1.ItemGUID=b2.ItemGUID and b1.StartDate=b2.StartDate "
                   + "group by b1.ItemGUID union select a1.ItemGUID,max(a1.Price) as Price,"
                   + "'Next' as PriceStatus from [dbo].[tblItemPrice] a1 join [dbo].[tblSupplierSite] a2 "
                   + "on a1.SupplierGUID=a2.SupplierGUID left join [dbo].[tblSite] a3 on a2.SiteGUID=a3.GUID "
                   + "where a1.PriceType='Buy' and (isnull(a3.BUGUID,'')='" + BUGuid + "' or isnull(a3.BUGUID,'')='"
                   + BUGuid + "') and a1.StartDate>" + calDate.ToString("yyyyMMdd")
                   + " group by a1.ItemGUID) c1,(select distinct a1.ItemGUID from [dbo].[tblItemBOM] a1,[dbo].[tblItem] a2 "
                   + "where a1.ItemGUID=a2.GUID and a1.ProductGUID='" + productGuid + "' union select distinct ParentGUID "
                   + "from [dbo].[tblItem] where isnull(ParentGUID,'')<>'') c2 where c1.ItemGUID=c2.ItemGUID";

            return GetCostList(itemSql, priceSql).First();
        }

        /// <summary>
        /// 计算成品数据中BOM每个原料换算单位后的成本价格,并返回List<BOM>对象
        /// </summary>

        /// <param name="calDate"></param>
        /// <returns></returns>
        private List<Model.BOM.BOMMast> GetCostList(string itemSql, string priceSql)
        {
            List<Utils.Database.SqlServer.DBQueryDic> sqlDics = new List<Utils.Database.SqlServer.DBQueryDic>();
            List<Model.UOM.UOMMast> uomList = null;
            Task t = new Task(() => { uomList = SEMI.MastData.MastDataHelper.GetInstance().GetUOMList("zh"); });
            t.Start();
            sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
            {
                Sql = itemSql,
                TableName = "ItemMast"
            });
            sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
            {
                Sql = priceSql,
                TableName = "PriceMast"
            });
            DataSet ds = new Utils.Database.SqlServer.DBHelper(_conn).GetDataSet(sqlDics);
            t.Wait();
            if (ds == null) throw new Exception("数据集获取失败,没有数据");
            DataTable data = ds.Tables["ItemMast"];
            if (data == null || data.Rows.Count == 0) throw new Exception("成品数据获取失败,没有数据");
            DataTable price = ds.Tables["PriceMast"];
            if (price == null || price.Rows.Count == 0) throw new Exception("原料数据获取失败,没有数据");
            if (uomList == null || uomList.Count == 0) throw new Exception("单位主数据获取失败,没有数据");
            var previousPriceData = price.AsEnumerable().Where(dr => dr.Field<string>("PriceStatus").Equals("Previous")).ToList();
            var priceData = price.AsEnumerable().Where(dr => dr.Field<string>("PriceStatus").Equals("Next")).ToList();
            return ProcessDatas(data, previousPriceData, priceData, uomList);
        }

        /// <summary>
        /// 计算成品数据中BOM每个原料换算单位后的成本价格,并返回List<BOM>对象
        /// </summary>
        /// <param name="itemData"></param>
        /// <param name="previousPriceData"></param>
        /// <param name="priceData"></param>
        /// <param name="uomList"></param>
        /// <returns></returns>
        private List<Model.BOM.BOMMast> ProcessDatas(DataTable itemData, List<DataRow> previousPriceData, List<DataRow> priceData,
            List<Model.UOM.UOMMast> uomList)
        {
            return (from dr in itemData.AsEnumerable()
                    join drp1 in previousPriceData
                    on dr.Field<string>("ItemGUID").GetString() equals drp1.Field<string>("ItemGUID").GetString() into pprice
                    join drpp1 in previousPriceData
                    on dr.Field<string>("ParentGUID").GetString() equals drpp1.Field<string>("ItemGUID").GetString() into parentpprice
                    join drp2 in priceData
                    on dr.Field<string>("ItemGUID").GetString() equals drp2.Field<string>("ItemGUID").GetString() into price
                    join drpp2 in priceData
                    on dr.Field<string>("ParentGUID").GetString() equals drpp2.Field<string>("ItemGUID").GetString() into parentprice
                    join dru in uomList
                    on dr.Field<string>("UOMGUID").GetString() equals dru.UOMGuid into dataUOM
                    join drpu in uomList
                    on dr.Field<string>("PurUOMGUID").GetString() equals drpu.UOMGuid into dataPUOM
                    join drppu in uomList
                    on dr.Field<string>("PPurUOMGUID").GetString() equals drppu.UOMGuid into dataPPUOM
                    from drpp in pprice.DefaultIfEmpty()
                    from drpp1 in parentpprice.DefaultIfEmpty()
                    from drp in price.DefaultIfEmpty()
                    from drp1 in parentprice.DefaultIfEmpty()
                    from dru in dataUOM.DefaultIfEmpty()
                    from drpu in dataPUOM.DefaultIfEmpty()
                    from drppu in dataPPUOM.DefaultIfEmpty()
                    let PrePrice = drpp == null ? 0 : CalBOMItemPrice(drpp.Field<decimal?>("Price").ToDouble(4),
                           dr.Field<decimal?>("ToQty").ToDouble(3), (drpu == null ? 1 : drpu.BaseQty), (dru == null ? 1 : dru.BaseQty))
                    let ParentPrePrice = drpp1 == null ? 0 : CalBOMItemPrice(drpp1.Field<decimal?>("Price").ToDouble(4),
                           dr.Field<decimal?>("ToQty").ToDouble(3), (drppu == null ? 1 : drppu.BaseQty), (dru == null ? 1 : dru.BaseQty))
                    let Price = drp == null ? 0 : CalBOMItemPrice(drp.Field<decimal?>("Price").ToDouble(4),
                           dr.Field<decimal?>("ToQty").ToDouble(3), (drpu == null ? 1 : drpu.BaseQty), (dru == null ? 1 : dru.BaseQty))
                    let ParentPrice = drp1 == null ? 0 : CalBOMItemPrice(drp1.Field<decimal?>("Price").ToDouble(4),
                           dr.Field<decimal?>("ToQty").ToDouble(3), (drppu == null ? 1 : drppu.BaseQty), (dru == null ? 1 : dru.BaseQty))
                    select new
                    {
                        ProductGUID = dr.Field<string>("ProductGUID").GetString(),
                        ItemCode = dr.Field<string>("ItemCode").GetString(),
                        ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                        ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                        ItemType = dr.Field<string>("ItemType").GetString(),
                        StdQty = dr.Field<decimal?>("StdQty").ToDouble(3),
                        ActQty = dr.Field<decimal?>("ActQty").ToDouble(3),
                        UOMName = dru == null ? string.Empty : dru.UOMName,
                        PreviousPrice = drpp == null ? ParentPrePrice : PrePrice,
                        Price = drp == null ? (drp1 == null ? PrePrice : ParentPrice) : Price
                    }).GroupBy(dg => dg.ProductGUID).Select(drg => new Model.BOM.BOMMast()
                    {
                        ProductGuid = drg.Key,
                        Details = drg.Select(drgg => new Model.BOM.BOMDetail()
                        {
                            ItemCode = drgg.ItemCode,
                            ItemName_CN = drgg.ItemName_CN,
                            ItemName_EN = drgg.ItemName_EN,
                            ItemType = drgg.ItemType,
                            UOMName = drgg.UOMName,
                            PreviousPrice = drgg.PreviousPrice,
                            Price = drgg.Price,
                            ActualQty = drgg.ActQty,
                            StdQty = drgg.StdQty,
                            StdCost = (drgg.StdQty * drgg.Price).ToDouble(4),
                            ActCost = (drgg.ActQty * drgg.Price).ToDouble(4),
                            PreviousStdCost = (drgg.StdQty * drgg.PreviousPrice).ToDouble(4),
                            PreviousActCost = (drgg.ActQty * drgg.PreviousPrice).ToDouble(4)
                        }).ToList()
                    }).ToList();
        }

        /// <summary>
        /// 计算BOM中Item价格
        /// </summary>
        /// <param name="price">价目表Item价格</param>
        /// <param name="iQty">item定义的转换数量系数</param>
        /// <param name="purUOMQty">采购单位转换到基本单位的系数</param>
        /// <param name="bomUOMQty">BOM单位转换到基本单位的系数</param>
        /// <returns></returns>
        private double CalBOMItemPrice(double price, double iQty, double purUOMQty, double bomUOMQty)
        {
            return ((price / (iQty.Equals(0) ? purUOMQty : iQty)) * bomUOMQty).ToDouble(4);
        }
    }
}
