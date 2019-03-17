using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Promotion
{
    public class ItemPromotionHelper: Common.BaseDataHelper
    {
        private static ItemPromotionHelper instance = new ItemPromotionHelper();

        private ItemPromotionHelper() { }

        public static ItemPromotionHelper GetInstance() { return instance; }

        public Model.Table.TableMast<Model.Promotion.PromotionMast> GetTablePromotionDatas(string BUGuid, string language)
        {
            Model.Table.TableMast<Model.Promotion.PromotionMast> tableMast = new Model.Table.TableMast<Model.Promotion.PromotionMast>();
            tableMast.draw = 1;
            tableMast.data = GetPromotionList(BUGuid, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Promotion.PromotionMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Promotion.PromotionMast> GetPromotionList(string BUGuid, string language)
        {
            try
            {
                string sql = "select ID,StartDate,EndDate,MinOrderAmt,MaxQty from [dbo].[tblPromotion] where BUGUID='" + BUGuid + "'";         
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);              
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Promotion.PromotionMast()
                {
                    ID = dr.Field<int>("ID"),
                    StartDate = dr.Field<int?>("StartDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Min"),
                    EndDate = dr.Field<int?>("EndDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Min"),
                    MinOrderAmt = dr.Field<decimal?>("MinOrderAmt").ToDouble(2),
                    MaxQty = dr.Field<int?>("MaxQty").ToInt()               
                }).ToList();
            }
            catch { return null; }
        }

        public Model.Promotion.PromotionMast GetPromotionMast(int ID, string language)
        {
            string sql = "select BUGUID,StartDate,EndDate,MinOrderAmt,MaxQty from [dbo].[tblPromotion] where ID=" + ID;
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count != 1) return null;
            return new Model.Promotion.PromotionMast()
            {
                ID = ID,
                BUGuid = data.Rows[0].Field<string>("BUGUID").GetString(),
                StartDate = data.Rows[0].Field<int?>("StartDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Min"),
                EndDate = data.Rows[0].Field<int?>("EndDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Max"),
                MinOrderAmt = data.Rows[0].Field<decimal?>("MinOrderAmt").ToDouble(2),
                MaxQty = data.Rows[0].Field<int?>("MaxQty").ToInt()
            };
        }

        public Model.Common.BaseResponse ModifyPromotionMast(Model.Promotion.PromotionMast data)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                string BUGuid = data.BUGuid.GetString();
                if(string.IsNullOrWhiteSpace(BUGuid)) throw new Exception("BUGuid not foun.");
                int ID = data.ID.ToInt();
                string sql = string.Empty;
                if (!ID.Equals(0)) //存在记录号,删除或者更改
                {
                    sql = "update [dbo].[tblPromotion] set EndDate=" + (string.IsNullOrWhiteSpace(data.EndDate) ?
                        "22991231" : DateTime.Parse(data.EndDate).ToString("yyyyMMdd")) + " where ID=" + ID;
                }
                else
                {
                    sql = "declare @id int;select @id=ID from [dbo].[tblPromotion] where BUGUID='" + BUGuid + "' and StartDate<="
                        + DateTime.Parse(data.StartDate).ToString("yyyyMMdd") + " and EndDate>="
                        + DateTime.Parse(data.StartDate).ToString("yyyyMMdd") + " and MinOrderAmt=" + data.MinOrderAmt.ToDouble(2)
                        + ";if(isnull(@id,0)=0) begin declare @guid char(36);select top 1 @guid=GUID from [dbo].[tblPromotion] "
                        + "where BUGUID='" + BUGuid + "';if(isnull(@guid,'')<>'') begin insert into [dbo].[tblPromotion]"
                        + "(GUID,BUGUID,StartDate,EndDate,MinOrderAmt,MaxQty) values(@guid,'" + BUGuid + "',"
                        + DateTime.Parse(data.StartDate).ToString("yyyyMMdd") + "," + (string.IsNullOrWhiteSpace(data.EndDate) ?
                        "22991231" : DateTime.Parse(data.EndDate).ToString("yyyyMMdd")) + "," + data.MinOrderAmt.ToDouble(2)
                        + "," + data.MaxQty + ");end else begin insert into [dbo].[tblPromotion]"
                        + "(GUID,BUGUID,StartDate,EndDate,MinOrderAmt,MaxQty) values(newid(),'" + BUGuid + "',"
                        + DateTime.Parse(data.StartDate).ToString("yyyyMMdd") + "," + (string.IsNullOrWhiteSpace(data.EndDate) ?
                        "22991231" : DateTime.Parse(data.EndDate).ToString("yyyyMMdd")) + "," + data.MinOrderAmt.ToDouble(2)
                        + "," + data.MaxQty + ");end end";
                }
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql.ToString());
                if (count > 0) resp.Status = "ok";
                else
                {
                    resp.Status = "error";
                    resp.Msg = "process failed, please check the data";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        public string GetPromotionGuid(string BUGuid)
        {
            string sql = "select top 1 GUID from [dbo].[tblPromotion] where BUGUID='" + BUGuid + "'";
            return new Utils.Database.SqlServer.DBHelper(_conn).GetDataScalar(sql).GetString();
        }

        public List<Model.Promotion.PromotionItem> GetPromotionItemList(string promotionGuid)
        {
            string sql = "select a1.ID,a2.ItemCode,a2.ItemName,a1.ItemGUID,a1.Price from [dbo].[tblPromotedItem] a1 "
                + "join [dbo].[tblItem] a2 on a1.ItemGUID=a2.GUID and a2.ToSell=1 and a2.Status=1 and a1.PromotionGUID='" 
                + promotionGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Promotion.PromotionItem()
            {
                ID = dr.Field<int>("ID"),
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName = dr.Field<string>("ItemName").GetString(),
                Price = dr.Field<decimal?>("Price").ToDouble(2),
                ItemGuid = dr.Field<string>("ItemGUID").GetString()
            }).ToList();
        }

        public Model.Common.BaseResponse ModifyPromotionItems(IList<Model.Promotion.PromotionItem> data)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                string sql = "insert into [dbo].[tblPromotedItem](PromotionGUID,ItemGUID,Price) values('{0}','{1}',{2});";
                string sql1 = "update [dbo].[tblPromotedItem] set Price={1} where ID={0}";
                StringBuilder excuteSql = new StringBuilder();
                foreach (Model.Promotion.PromotionItem item in data)
                {
                    if (!item.ID.Equals(0)) excuteSql.AppendFormat(sql1, item.ID, item.Price.ToDouble(2));
                    else
                    {
                        if (string.IsNullOrWhiteSpace(item.PromotionGuid) || string.IsNullOrWhiteSpace(item.ItemGuid)) continue;
                        excuteSql.AppendFormat(sql, item.PromotionGuid, item.ItemGuid, item.Price.ToDouble(2));
                    }
                }
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(excuteSql.ToString());
                if (count > 0) resp.Status = "ok";
                else
                {
                    resp.Status = "error";
                    resp.Msg = "process failed, please check the data";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }
    }
}
