using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Service
{
    public class ServerServiceHelper : Common.BaseDataHelper
    {
        public const string _identity = "adenservices";
        public string key = string.Empty;
        private bool identitied = false;

        private string ReturnString<T>(T obj)
        {
            return Utils.Common.EncyptHelper.Encypt(Utils.Common.JsonHelper.SerializeJsonString(obj), this.key);
        }

        public ServerServiceHelper(string timeStamp, string identity)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(timeStamp) && !string.IsNullOrWhiteSpace(identity))
                {
                    this.key = timeStamp.Substring(timeStamp.Length - 8, 8);
                    identitied = Utils.Common.EncyptHelper.DesEncypt(identity, key).Equals(_identity);
                }
            }
            catch { }
        }

        public string GetItems(string siteGuid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(siteGuid)) return string.Empty;

                string sql = "select GUID,ItemCode,ItemName,Tips from [dbo].[tblItem]";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return string.Empty;
                List<Model.Item.ItemMast> itemList = data.AsEnumerable().Select(dr => new Model.Item.ItemMast()
                {
                    ItemGuid = dr.Field<string>("GUID").GetString(),
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName = dr.Field<string>("ItemName").GetString(),
                    ItemTips = dr.Field<string>("Tips").GetString()
                }).ToList();
                return ReturnString<List<Model.Item.ItemMast>>(itemList);
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("GetItems", "获取Item数据失败", siteGuid, e.Message);
                return "SyncFailed:" + e.Message;
            }
            finally { Log.LogHelper.GetInstance().WriteDBLog("GetItems", "获取Item数据", siteGuid, string.Empty); }
        }

        public string GetBOMs(string siteGuid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(siteGuid)) return string.Empty;

                string sql = "select ProductGUID,ItemGUID,StdQty,ActQty,UOMGUID from [dbo].[tblItemBOM]";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return string.Empty;
                List<Model.BOM.BOMMast> bomList = data.AsEnumerable().GroupBy(dr => dr.Field<string>("ProductGUID").GetString())
                    .Select(dg => new Model.BOM.BOMMast()
                    {
                        ProductGuid = dg.Key,
                        Details = dg.Select(drg => new Model.BOM.BOMDetail()
                        {
                            ItemGuid = drg.Field<string>("ItemGUID").GetString(),
                            StdQty = drg.Field<decimal?>("StdQty").ToDouble(3),
                            ActualQty = drg.Field<decimal?>("ActQty").ToDouble(3),
                            UOMGuid = drg.Field<string>("UOMGUID").GetString()
                        }).ToList()
                    }).ToList();
                return ReturnString<List<Model.BOM.BOMMast>>(bomList);
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("GetBOMs", "获取BOM数据失败", siteGuid, e.Message);
                return "SyncFailed:" + e.Message;
            }
            finally { Log.LogHelper.GetInstance().WriteDBLog("GetBOMs", "获取BOM数据", siteGuid, string.Empty); }
        }

        public string GetUOMs(string siteGuid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(siteGuid)) return string.Empty;

                string sql = " select GUID,NameCn from [dbo].[tblUOM]";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return string.Empty;
                List<Model.UOM.UOMMast> uomList = data.AsEnumerable().Select(dr => new Model.UOM.UOMMast()
                {
                    UOMGuid = dr.Field<string>("GUID").GetString(),
                    UOMName = dr.Field<string>("NameCn").GetString()
                }).ToList();
                return ReturnString<List<Model.UOM.UOMMast>>(uomList);
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("GetUOMs", "获取UOM数据失败", siteGuid, e.Message);
                return "SyncFailed:" + e.Message;
            }
            finally { Log.LogHelper.GetInstance().WriteDBLog("GetUOMs", "获取UOM数据", siteGuid, string.Empty); }
        }

        public string GetSiteUsers(string siteGuid)
        {
            try
            {
                string sql = "select WechatID,FirstName,LastName,Mobile,UserID from tblUser "
                    + "where SiteGUID='" + siteGuid + "'"; // and isnull(IsSync,0)=0";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return string.Empty;
                List<Model.Account.SiteUser> userList = data.AsEnumerable().Select(dr => new Model.Account.SiteUser()
                {
                    UserID = dr.Field<int>("UserID"),
                    FirstName = dr.Field<string>("FirstName").GetString(),
                    LastName = dr.Field<string>("LastName").GetString(),
                    MobileNbr = dr.Field<string>("Mobile").GetString(),
                    WeChatID = dr.Field<string>("WeChatID").GetString()
                }).ToList();
                return ReturnString<List<Model.Account.SiteUser>>(userList);
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("GetSiteUsers", "获取用户数据失败", siteGuid, e.Message);
                return "SyncFailed:" + e.Message;
            }
            finally { Log.LogHelper.GetInstance().WriteDBLog("GetSiteUsers", "获取用户数据", siteGuid, string.Empty); }
        }

        public string GetSaleOrders(string siteGuid)
        {
            try
            {
                string sql = "select a1.OrderID,a1.GUID as HeadGUID,a1.OrderCode,a1.UserID,a1.OrderTime,a1.OrderDate,"
                    + "a1.RequiredDate,a1.TotalAmount,a1.PaymentAmount,a1.PaidTime,a2.GUID as LineGUID,a2.ItemGUID,a2.Qty,"
                    + "a2.Price,a2.IsPrint from tblSaleOrder a1 join tblSaleOrderItem a2 on a1.GUID=a2.SOGUID "
                    + "where a1.IsPaid=1 and a1.Status=20 and a1.SiteGUID='" + siteGuid + "'";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return string.Empty;
                List<Model.Order.SaleOrder> orderList = data.AsEnumerable().GroupBy(dr => new
                {
                    OrderID = dr.Field<int>("OrderID"),
                    HeadGUID = dr.Field<string>("HeadGUID").GetString(),
                    OrderCode = dr.Field<string>("OrderCode").GetString(),
                    UserID = dr.Field<int>("UserID"),
                    OrderDate = dr.Field<int>("OrderDate"),
                    OrderTime = dr.Field<DateTime>("OrderTime").ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    RequiredDate = dr.Field<DateTime>("RequiredDate").ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    TotalAmount = dr.Field<decimal>("TotalAmount"),
                    PaymentAmount = dr.Field<decimal>("PaymentAmount"),
                    PaidTime = dr.Field<DateTime>("PaidTime").ToString("yyyy-MM-dd HH:mm:ss.fff")
                }).Select(dg => new Model.Order.SaleOrder()
                {
                    HeadGUID = dg.Key.HeadGUID,
                    OrderCode = dg.Key.OrderCode,
                    OrderDate = dg.Key.OrderDate,
                    OrderID = dg.Key.OrderID,
                    RequiredDate = dg.Key.RequiredDate,
                    UserID = dg.Key.UserID,
                    OrderTime = dg.Key.OrderTime,
                    PaidTime = dg.Key.PaidTime,
                    PaymentAmount = dg.Key.PaymentAmount,
                    TotalAmount = dg.Key.TotalAmount,
                    Lines = dg.Select(drg => new Model.Order.SaleLine()
                    {
                        LineGUID = drg.Field<string>("LineGUID").GetString(),
                        ItemGUID = drg.Field<string>("ItemGUID").GetString(),
                        Price = drg.Field<decimal>("Price"),
                        Qty = drg.Field<int>("Qty"),
                        IsPrint = drg.Field<bool?>("IsPrint").BoolToInt()
                    }).ToList()
                }).ToList();
                return ReturnString<List<Model.Order.SaleOrder>>(orderList);
            }
            catch (Exception e)
            {
                Log.LogHelper.GetInstance().WriteDBLog("GetSaleOrders", "获取销售订单数据失败", siteGuid, e.Message);
                return "SyncFailed:" + e.Message;
            }
            finally { Log.LogHelper.GetInstance().WriteDBLog("GetSaleOrders", "获取销售订单数据", siteGuid, string.Empty); }

        }

        public void UpdateSiteUsers(string siteGuid, string idString)
        {
            try
            {
                return;
                //List<string> userIds = Utils.Common.JsonHelper.DeSerializerJsonString<List<string>>(
                //    Utils.Common.EncyptHelper.DesEncypt(idString, this.key));
                //if (userIds == null || userIds.Count == 0) return;
                //string sql = "update [dbo].[tblUser] set IsSync=1 where UserID in ("
                //        + string.Join(",", userIds.ToArray()) + ")";
                //int c = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql);
                //if (c <= 0) throw new Exception("更新记录为零");
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("UpdateSiteUsers", "更新用户失败", siteGuid, e.Message); }
        }

        public void UpdateSaleOrders(string siteGuid, string data)
        {
            try
            {
                List<Model.Order.SaleOrder> orderList = Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.Order.SaleOrder>>(
                    Utils.Common.EncyptHelper.DesEncypt(data, this.key));
                if (orderList == null || orderList.Count == 0) return;
                string sql = "insert into [dbo].[tblStockTransaction](UserID,SiteGUID,SODetailGUID,ItemGUID,Cost,Qty,CreateTime,CreateUser) "
                           + "select a1.UserID,a1.SiteGUID,a2.GUID,a2.ItemGUID,a2.Price,a2.Qty,'{1}',{2} from [dbo].[tblSaleOrder] a1,"
                           + "[dbo].[tblSaleOrderItem] a2 where a1.GUID=a2.SOGUID and a1.IsPaid=1 and a1.Status=20 "
                           + "and a1.OrderID={0};update [dbo].[tblSaleOrder] set Status=10,ShippedDate='{1}',ShippedUser='{2}' where OrderID={0};";
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (var order in orderList)
                    sqlBuilder.AppendFormat(sql, order.OrderID, order.ShippedDate, order.ShippedUser);
                int c = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                if(c <= 0) throw new Exception("更新记录为零");
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("UpdateSaleOrders", "更新销售订单失败", siteGuid, e.Message); }
        }
    }
}
