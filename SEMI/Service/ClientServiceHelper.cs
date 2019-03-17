using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Service
{
    public class ClientServiceHelper : Common.BaseDataHelper
    {
        private static ClientServiceHelper instance = new ClientServiceHelper();
        private ClientServiceHelper() { }
        public static ClientServiceHelper GetInstance() { return instance; }

        public void SyncItems(List<Model.Item.ItemMast> itemList)
        {
            try
            {
                if (itemList == null || itemList.Count == 0) return;
                string sql = "insert into [dbo].[tblItem](GUID,ItemName,ItemCode,Tips,Status) "
                    + "values('{0}','{1}','{2}','{3}',1);";
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (var item in itemList)
                    sqlBuilder.AppendFormat(sql, item.ItemGuid, item.ItemName, item.ItemCode, item.ItemTips);
                if (sqlBuilder.Length > 0)
                {
                    sqlBuilder.Insert(0, "truncate table [dbo].[tblItem];");
                    new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                }
            }
            catch (Exception e) { throw new Exception("[ClientServiceHelper.SyncItems]:" + e.Message); }
        }

        public void SyncBOMs(List<Model.BOM.BOMMast> bomList)
        {
            try
            {
                if (bomList == null || bomList.Count == 0) return;
                string sql = "insert into [dbo].[tblItemBOM](ProductGUID,ItemGUID,StdQty,ActQty,UOMGUID) values('"
                           + "{0}','{1}',{2},{3},'{4}');";
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (var bom in bomList)
                {
                    if (bom.Details == null || bom.Details.Count == 0) continue;
                    foreach(var detail in bom.Details)
                        sqlBuilder.AppendFormat(sql, bom.ProductGuid, detail.ItemGuid, detail.StdQty, detail.ActualQty, detail.UOMGuid);
                }
                    
                if (sqlBuilder.Length > 0)
                {
                    sqlBuilder.Insert(0, "truncate table [dbo].[tblItemBOM];");
                    new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                }
            }
            catch (Exception e) { throw new Exception("[ClientServiceHelper.SyncBOMs]:" + e.Message); }
        }

        public void SyncUOMs(List<Model.UOM.UOMMast> uomList)
        {
            try
            {
                if (uomList == null || uomList.Count == 0) return;
                string sql = "insert into [dbo].[tblUOM](GUID,NameCn) values('{0}','{1}');";
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (var uom in uomList)
                {
                    sqlBuilder.AppendFormat(sql, uom.UOMGuid, uom.UOMName);
                }

                if (sqlBuilder.Length > 0)
                {
                    sqlBuilder.Insert(0, "truncate table [dbo].[tblUOM];");
                    new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                }
            }
            catch (Exception e) { throw new Exception("[ClientServiceHelper.SyncUOMs]:" + e.Message); }
        }

        public void SyncSiteUsers(string siteGuid,List<Model.Account.SiteUser> userList)
        {
            try
            {
                if (userList == null || userList.Count == 0) return;
                string selectSql = "select UserID from [dbo].[tblUser] where UserID in ("
                    + string.Join(",", userList.Select(u => u.UserID.ToString()).ToArray()) + ")";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = helper.GetDataTable(selectSql);
                if (data == null) data = new DataTable();
                var query = (from u in userList
                             join dr in data.AsEnumerable()
                             on u.UserID equals dr.Field<int>("UserID") into ldata
                             from ldr in ldata.DefaultIfEmpty()
                             where ldr == null
                             select u).ToList();
                if (!query.Any()) return;
                else
                {
                    string sql = "insert into [dbo].[tblUser](UserID,WechatID,FirstName,LastName,SiteGUID,Mobile) "
                               + "values({0},'{1}','{2}','{3}','" + siteGuid + "','{4}');";
                    StringBuilder sqlBuilder = new StringBuilder();
                    foreach (var user in query)
                        sqlBuilder.AppendFormat(sql, user.UserID, user.WeChatID, user.FirstName.SqlEscapeString(),
                            user.LastName.SqlEscapeString(), user.MobileNbr.SqlEscapeString());
                    int c = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                    if (c > 0) return;
                    else throw new Exception("用户数据插入失败");
                }
            }
            catch(Exception e) { throw new Exception("[ClientServiceHelper.SyncSiteUsers]:" + e.Message); }
        }

        public List<Model.Order.SaleOrder> SyncSaleOrders(string siteGuid, List<Model.Order.SaleOrder> orderList)
        {
            try
            {
                if (orderList == null || orderList.Count == 0) return null;
                string sql = "select OrderID from [dbo].[tblSaleOrder] where OrderID in ("
                    + string.Join(",", orderList.Select(o => o.OrderID.ToString()).ToArray()) + ")";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = helper.GetDataTable(sql);
                if (data == null) data = new DataTable();
                var query = (from ol in orderList
                             join dr in data.AsEnumerable()
                             on ol.OrderID equals dr.Field<int>("OrderID") into ldata
                             from ldr in ldata.DefaultIfEmpty()
                             where ldr == null
                             select ol).ToList();
                if (query.Any())
                {
                    string sqlHeader = "insert into [dbo].[tblSaleOrder](GUID,OrderCode,UserID,SiteGUID,OrderTime,OrderDate,"
                        + "RequiredDate,TotalAmount,PaymentAmount,IsPaid,PaidTime,Status,OrderID,IsDel) values('{0}','{1}',{2},'"
                        + siteGuid + "','{3}',{4},'{5}',{6},{7},1,'{8}',20,{9},0);";
                    string sqlLine = "insert into [dbo].[tblSaleOrderItem](GUID,UserID,SOGUID,ItemGUID,Qty,Price,CreateTime,"
                        + "ShippingStatus,ShippedDate,IsComment,IsPrint) values('{0}',{1},'{2}','{3}',{4},{5},'{6}',20,0,0,0);";
                    StringBuilder sqlBuilder = new StringBuilder();
                    foreach (var order in query)
                    {
                        sqlBuilder.AppendFormat(sqlHeader, order.HeadGUID, order.OrderCode, order.UserID, order.OrderTime,
                            order.OrderDate, order.RequiredDate, order.TotalAmount, order.PaymentAmount, order.PaidTime, order.OrderID);
                        foreach (var line in order.Lines)
                        {
                            sqlBuilder.AppendFormat(sqlLine, line.LineGUID, order.UserID, order.HeadGUID, line.ItemGUID,
                                line.Qty, line.Price, order.OrderTime);
                        }
                    }
                    int c = helper.Execute(sqlBuilder.ToString());
                    if (c == 0) throw new Exception("插入订单数据失败");
                }
                string sqlOrder = "select OrderID,ShippedDate,ShippedUser from [dbo].[tblSaleOrder] where Status=10 and OrderID in ("
                    + string.Join(",", orderList.Select(ol => ol.OrderID.ToString()).ToArray()) + ")";
                DataTable orderData = helper.GetDataTable(sqlOrder);
                if (orderData == null || orderData.Rows.Count == 0) return null;
                return orderData.AsEnumerable().Select(dr => new Model.Order.SaleOrder()
                {
                    OrderID = dr.Field<int>("OrderID"),
                    ShippedDate = dr.Field<DateTime>("ShippedDate").ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ShippedUser = dr.Field<int?>("ShippedUser").ToInt()
                }).ToList();
            }
            catch (Exception e) { throw new Exception("[ClientServiceHelper.SyncSaleOrders]:" + e.Message); }
        }

        public string GetSiteGuid()
        {
            try
            {
                string sql = "select top 1 GUID from [dbo].[tblSite]";
                return new Utils.Database.SqlServer.DBHelper(_conn).GetDataScalar(sql).GetString();
            }
            catch { return string.Empty; }
        }
        
    }
}
