using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Diagnostics;
using Utils.Common;

namespace SEMI.MRP
{
    public class MRPHelper: Common.BaseDataHelper
    {
        private static MRPHelper instance = new MRPHelper();

        private bool processStatus = false;
        private MRPHelper() { }
        public static MRPHelper GetInstance() { return instance; }

        //steve.weng 2017-6-12 按site确定开始时间
        public void Run()
        {
            
            if (processStatus) return;
            processStatus = true;
            try
            {
                DateTime date = DateTime.Now;

                string stopTime = ConfigurationManager.AppSettings["MRPStopTime"].GetString();

                //超过定义的结束时间,则不再运行程序
                if (!string.IsNullOrWhiteSpace(stopTime) && date > DateTime.Parse(date.ToString("yyyy-MM-dd") + " " + stopTime)) return;

                string sql = "select b1.* from "
                    + "(select a2.GUID,a2.code, isnull(a2.EndHour, a1.EndHour) EndHour, a1.Timeout, convert(varchar(8), getdate() + "
                    + "case a2.DeliveryDays when - 1 then a1.DeliveryDays else a2.DeliveryDays end, 112) RequiredDate "
                    + "from tblbu a1, tblsite a2 "
                    + "where a1.BUGUID = a2.BUGUID ) b1 left join tblMRPOrder b2 on b1.GUID = b2.SiteGUID and b1.RequiredDate = b2.RequiredDate "                    
                    + "where b2.ID is null";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable SiteData = helper.GetDataTable(sql);
                if (SiteData == null || SiteData.Rows.Count == 0) return;
               // DateTime StopTime = SiteData.AsEnumerable().Max(q => DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + q.Field<string>("EndHour")));
                string endHour = string.Empty;
                int timeOut = 0;
                //按Site生成MRP

                StringBuilder sbinfosite = new StringBuilder();
                foreach (DataRow row in SiteData.Rows)
                {
                    sbinfosite.AppendFormat("\n{0} {1}", row.Field<string>("code"), row.Field<string>("EndHour"));
                    endHour = row.Field<string>("EndHour").GetString();
                    timeOut = row.Field<int?>("TimeOut").ToInt();// + 5;
                    if (timeOut > 60) timeOut = 60; //超时时间最多1小时
                    if (string.IsNullOrWhiteSpace(endHour)) endHour = "11:30";
                    if (DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + endHour).AddMinutes(timeOut) <= DateTime.Now)
                    {
                        try
                        {
                            sbinfosite.Append("  Run");
                            Process(row.Field<string>("RequiredDate").ToInt(), row.Field<string>("GUID").GetString(), helper);
                        }
                        catch (Exception ex)
                        {
                            WriteErrorLog("运行营运点MRP错误", row.Field<string>("GUID").GetString(), ex.Message);
                        }
                        
                    }
                }

                EventLog.WriteEntry(System.Diagnostics.Process.GetCurrentProcess().ProcessName, 
                    string.Format("{0} - {1}\n{2}", date.ToShortTimeString(), DateTime.Now.ToShortTimeString(),sbinfosite));
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("MRP", "执行MRP错误", "SEMI.MRP.MRPHelper.Run()", e.Message); }
            finally { processStatus = false; }
        }

        //steve.weng 2017-6-12 按BU确定开始时间
        public void Run2()
        {
            if (processStatus) return;
            processStatus = true;
            try
            {
                DateTime date = DateTime.Now;

                string stopTime = ConfigurationManager.AppSettings["MRPStopTime"].GetString();

                //超过定义的结束时间,则不再运行程序
                if (!string.IsNullOrWhiteSpace(stopTime) && date > DateTime.Parse(date.ToString("yyyy-MM-dd") + " " + stopTime)) return;

                string BUSql = "select BUGUID, Endhour,TimeOut from [dbo].[tblBU]";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable BUData = helper.GetDataTable(BUSql);
                if (BUData == null || BUData.Rows.Count == 0) return;
                string endHour = string.Empty;
                int timeOut = 0;
                //按BU生成MRP
                foreach (DataRow row in BUData.Rows)
                {
                    endHour = row.Field<string>("EndHour").GetString();
                    timeOut = row.Field<int?>("TimeOut").ToInt() + 5;
                    if (timeOut > 60) timeOut = 60; //超时时间最多1小时
                    if (string.IsNullOrWhiteSpace(endHour)) endHour = "11:30";
                    if (DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + endHour).AddMinutes(timeOut) <= DateTime.Now)
                        GenerateMRP(date.ToString("yyyyMMdd").ToInt(), row.Field<string>("BUGUID").GetString(), helper);
                }
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("MRP", "执行MRP错误", "SEMI.MRP.MRPHelper.Run()", e.Message); }
            finally { processStatus = false; }
        }

        public Model.Common.BaseResponse GenerateMRP(string siteGUID, DateTime date)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                int dateInt = date.ToString("yyyyMMdd").ToInt();
                string sql = "select ID from [dbo].[tblMRPOrder] where SiteGUID='" + siteGUID + "' and RequiredDate=" + dateInt;
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                string val = helper.GetDataScalar(sql).GetString();
                if (!string.IsNullOrWhiteSpace(val)) throw new Exception("MRP has been created.");
                Process(dateInt, siteGUID, helper);
                resp.Status = "ok";
                resp.Msg = "MRP finished.";
            }
            catch(Exception e) 
            {
                resp.Status = "error";
                resp.Msg = e.Message;
            }
            return resp;
        }

        private void GenerateMRP(int dateInt, string BUGUID, Utils.Database.SqlServer.DBHelper helper)
        {
            try
            {
                string siteSql = "select GUID from [dbo].[tblSite] where BUGUID='" + BUGUID + "' and GUID not in ("
                        + "select SiteGUID from [dbo].[tblMRPOrder] where RequiredDate=" + dateInt + ")";
                DataTable data = helper.GetDataTable(siteSql);
                if (data == null || data.Rows.Count == 0) return;
                foreach(DataRow dr in data.Rows)
                {
                    try { Process(dateInt, dr.Field<string>("GUID").GetString(), helper); }
                    catch (Exception ex) { WriteErrorLog("运行营运点MRP错误", dr.Field<string>("GUID").GetString(), ex.Message); }
                } 
            }
            catch (Exception e) { WriteErrorLog("运行公司MRP错误", BUGUID, e.Message); }
        }

        private void Process(int dateInt, string SiteGUID, Utils.Database.SqlServer.DBHelper helper)
        {
            Model.MRP.MRPOrder mrpOrder = new Model.MRP.MRPOrder();
            mrpOrder.GUID = Guid.NewGuid().ToString();
            mrpOrder.SiteGUID = SiteGUID;
            mrpOrder.RequiredDate = dateInt.ToString();
            string dataSql = "select a1.GUID as SODetailGUID,a1.ItemGUID as ProductGUID,a1.Qty as SaleQty,a3.ItemGUID,"
                + "a3.ActQty,a3.UOMGUID,a4.PurUOMGUID,a4.ItemType,a5.ToQty,a6.DataType from [dbo].[tblSaleOrderItem] a1 join [dbo].[tblSaleOrder] a2 "
                + "on a1.SOGUID=a2.GUID join [dbo].[tblItemBOM] a3 on a1.ItemGUID=a3.ProductGUID join [dbo].[tblItem] a4 "
                + "on a3.ItemGUID=a4.GUID and a4.ToBuy=1 and a4.Status=1 and a4.PurchasePolicy='OnDemand' left join ("
                + "select distinct a1.ItemGUID,a1.UOMGUID,a1.ToQty from tblItemUOMConv a1 join tblUOM a2 on a1.ToUOMGUID=a2.GUID "
                + "and isnull(a2.ToUOMGUID,'')='' where a1.Active=1) a5 on a3.ItemGUID=a5.ItemGUID and a4.PurUOMGUID=a5.UOMGUID "
                + "join tblUOM a6 on a4.PurUOMGUID=a6.GUID and a6.Active=1 where a2.IsPaid=1 and a2.isdel=0 and convert(varchar(8),a2.requireddate,112)='"
                + dateInt + "' and a2.SiteGUID='" + SiteGUID + "'";
            DataTable data = helper.GetDataTable(dataSql);
            if (data != null && data.Rows.Count > 0)
            {
                StringBuilder priceSql = new StringBuilder();
                priceSql.AppendFormat("select a1.ItemGUID,a1.SupplierGUID,a1.Price from [dbo].[tblItemPrice] a1 "
                    + "join [dbo].[tblSupplierSite] a2 on a1.SupplierGUID=a2.SupplierGUID join [dbo].[tblItem] a3 "
                    + "on a1.ItemGUID=a3.GUID and a3.ToBuy=1 and a3.Status=1 and a3.PurchasePolicy='OnDemand' "
                    + "where a1.PriceType='Buy' and a2.SiteGUID='" + SiteGUID + "' and isnull(a1.StartDate,19000101)<=" + dateInt
                    + " and isnull(a1.EndDate,20991231)>=" + dateInt + " and a1.ItemGUID in ({0})", string.Join(",",
                    data.AsEnumerable().Select(dr => "'" + dr.Field<string>("ItemGUID").GetString() + "'").Distinct().ToArray()));
                DataTable priceData = helper.GetDataTable(priceSql.ToString());
                if (priceData == null || priceData.Rows.Count == 0) throw new Exception("没有找到价目表数据");
                var checkQry = priceData.AsEnumerable().GroupBy(dr => dr.Field<string>("ItemGUID").GetString())
                    .Select(drg => new { key = drg.Key, count = drg.Count() }).Where(dg => dg.count > 1);
                if (checkQry.Any())//同一个Item是否存在多个供应商或者价格
                {
                    StringBuilder err = new StringBuilder();
                    err.Append("Item存在多个供应商或者价格:").Append(string.Join(",", checkQry.Select(c => c.key).ToArray()));
                    throw new Exception(err.ToString());
                }
                List<Model.UOM.UOMMast> uomList = SEMI.MastData.MastDataHelper.GetInstance().GetUOMList("zh");
                var query = (from d in data.AsEnumerable()
                             join p in priceData.AsEnumerable()
                             on d.Field<string>("ItemGUID").GetString() equals p.Field<string>("ItemGUID") into pdata
                             join u1 in uomList
                             on d.Field<string>("UOMGUID").GetString() equals u1.UOMGuid into bomUOM
                             join u2 in uomList
                             on d.Field<string>("PurUOMGUID").GetString() equals u2.UOMGuid into purUOM
                             from pd in pdata.DefaultIfEmpty()
                             from buom in bomUOM.DefaultIfEmpty()
                             from puom in purUOM.DefaultIfEmpty()
                             let iQty = d.Field<decimal?>("ToQty").ToDouble(3)
                             select new
                             {
                                 SODetailGUID = d.Field<string>("SODetailGUID").GetString(),
                                 ProductGUID = d.Field<string>("ProductGUID").GetString(),
                                 SaleQty = d.Field<int>("SaleQty"),
                                 ItemGUID = d.Field<string>("ItemGUID").GetString(),
                                 UOMGUID = d.Field<string>("PurUOMGUID").GetString(),
                                 RoundUp = d.Field<string>("DataType").GetString().Equals("RoundUpToINT"),
                                 ActQty = (puom == null ? 0 : (d.Field<decimal?>("ActQty").ToDouble(3)
                                    * (buom == null ? 0 : buom.BaseQty) / (iQty.Equals(0) ? puom.BaseQty : iQty))).ToDouble(3),
                                 SupplierGUID = pd == null ? "" : pd.Field<string>("SupplierGUID").GetString(),
                                 Price = pd == null ? 0 : pd.Field<decimal?>("Price").ToDouble(4),
                                 ItemType = d.Field<string>("ItemType").ToUpper()
                             }).ToList();
                var cq1 = query.Where(q => string.IsNullOrWhiteSpace(q.SupplierGUID) && q.ItemType != "EXPENSE");
                if (cq1.Any())
                {
                    StringBuilder err = new StringBuilder();
                    err.Append("Item没有价目表:").Append(string.Join(",", cq1.Select(c => c.ItemGUID).ToArray()));
                    
                    throw new Exception(err.ToString()); 
                }
                var cq2 = query.Where(q => q.ActQty.Equals(0));
                if (cq2.Any())
                {
                    StringBuilder err = new StringBuilder();
                    err.Append("BOMItem数量为0,请检查BOM,BOM单位,采购单位:").Append(string.Join(",",
                        cq2.Select(c => c.ItemGUID).ToArray()));
                    throw new Exception(err.ToString());
                }
                mrpOrder.Lines = query.Where(q=>q.Price != 0).GroupBy(q => new
                {
                    Supplier = q.SupplierGUID,
                    Item = q.ItemGUID,
                    UOM = q.UOMGUID,
                    RoundUp = q.RoundUp,
                    Price = q.Price
                }).Select(g => new Model.MRP.MRPOrderItem()
                {
                    ItemGUID = g.Key.Item,
                    SupplierGUID = g.Key.Supplier,
                    LineGUID = Guid.NewGuid().ToString(),
                    UOMGUID = g.Key.UOM,
                    Qty = (g.Key.RoundUp ? Math.Ceiling(g.Sum(gg => gg.ActQty * gg.SaleQty).ToDouble(3)) :
                        g.Sum(gg => gg.ActQty * gg.SaleQty).ToDouble(3)),
                    Price = g.Key.Price,
                    SODetails = g.Select(gr => new Model.MRP.MRPSODetail()
                    {
                        ProductGUID = gr.ProductGUID,
                        SODetailGUID = gr.SODetailGUID,
                        Qty = gr.SaleQty,
                        UsedQty = (gr.ActQty * gr.SaleQty).ToDouble(3)
                    }).ToList()
                }).ToList();
            }
            WriteMRPOrder(mrpOrder, helper);
        }

        private void WriteMRPOrder(Model.MRP.MRPOrder order, Utils.Database.SqlServer.DBHelper helper)
        {
            try
            {
                if(order == null) return;
                string insertMrpSql = "insert into [dbo].[tblMRPOrder](GUID,SiteGUID,RequiredDate) values('{0}','{1}',{2});";
                string insertMrpLineSql = "insert into [dbo].[tblMRPOrderItem](GUID,MRPGUID,ItemGUID,UOMGUID,Qty,Price,SupplierGUID) "
                                        + "values('{0}','{1}','{2}','{3}',{4},{5},'{6}');";
                string insertMrpSOSql = "insert into [dbo].[tblMRPSODetail](MRPLineGUID,SODetailGUID,ProductGUID,Qty,UsedQty) "
                                      + "values('{0}','{1}','{2}',{3},{4});";
                string insertPOSql = "insert into tblPurchaseOrder(GUID,SiteGUID,OrderCode,OrderDate,SupplierGUID,"
                                   + "RequiredDate,IsClosed) values('{0}','{1}','{2}',{3},'{4}',{5},0);";
                string insertPOLineSql = "insert into [dbo].[tblPurchaseOrderItem](GUID,POGUID,ItemGUID,Price,Qty,"
                                       + "UOMGUID,RequiredDate,CreateTime) values('{0}','{1}','{2}',{3},{4},'{5}',{6},'"
                                       + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "');";

                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.AppendFormat(insertMrpSql, order.GUID, order.SiteGUID, order.RequiredDate);
                if (order.Lines != null && order.Lines.Count > 0)
                {
                    string guid = string.Empty;
                    var gQry = order.Lines.GroupBy(g => g.SupplierGUID).Select(gr => new
                    {
                        Key = gr.Key,
                        Data = gr
                    }).ToList();
                    foreach(var grp in gQry)
                    {
                        guid = Guid.NewGuid().ToString();
                        sqlBuilder.AppendFormat(insertPOSql, guid, order.SiteGUID, DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                            order.RequiredDate, grp.Key, order.RequiredDate);
                        foreach(var d in grp.Data)
                        {
                            sqlBuilder.AppendFormat(insertPOLineSql, d.LineGUID, guid, d.ItemGUID, d.Price, d.Qty,
                                d.UOMGUID, order.RequiredDate);
                            sqlBuilder.AppendFormat(insertMrpLineSql, d.LineGUID, order.GUID, d.ItemGUID, d.UOMGUID, d.Qty,
                                d.Price, d.SupplierGUID);
                            foreach (var soDetail in d.SODetails)
                                sqlBuilder.AppendFormat(insertMrpSOSql, d.LineGUID, soDetail.SODetailGUID, 
                                    soDetail.ProductGUID, soDetail.Qty, soDetail.UsedQty);
                        }
                    }
                }
                if (sqlBuilder.Length > 0) helper.Execute(sqlBuilder.ToString());

            }
            catch (Exception e) { WriteErrorLog("营运点写入数据库失败", order.SiteGUID, e.Message); }
        }

        private void WriteErrorLog(string title, string GUID, string errorMessage)
        {
            Log.LogHelper.GetInstance().WriteDBLog("MRP", title, GUID, errorMessage);
        }

        public Model.Table.TableMast<Model.MRP.MRPOrderItem> GetMRPDatas(string siteGuid, string requiredDate, string language)
        {
            Model.Table.TableMast<Model.MRP.MRPOrderItem> tableMast = new Model.Table.TableMast<Model.MRP.MRPOrderItem>();
            tableMast.draw = 1;
            tableMast.data = GetMRPItemList(siteGuid, requiredDate, language);
            if (tableMast.data == null) tableMast.data = new List<Model.MRP.MRPOrderItem>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public List<Model.MRP.MRPOrderItem> GetMRPItemList(string siteGuid, string requiredDate, string language)
        {
            string sql = "select a1.GUID,a2.ItemCode,a2.ItemName,a1.Qty,a1.Price,case when 'zh'='" + language
                + "' then a3.NameCn else a3.NameEn end as UnitName,a4.SupplierCode + '/' + case when 'zh'='" + language
                + "' then a4.CompNameCn else a4.CompNameEn end as SupplierName from [dbo].[tblMRPOrderItem] a1 "
                + "join [dbo].[tblItem] a2 on a1.ItemGUID=a2.GUID join [dbo].[tblUOM] a3 on a1.UOMGUID=a3.GUID "
                + "join [dbo].[tblSupplier] a4 on a1.SupplierGUID=a4.SupplierGUID join [dbo].[tblMRPOrder] a5 on a1.MRPGUID=a5.GUID "
                + "and a5.SiteGUID = '" + siteGuid + "' and a5.RequiredDate=" + DateTime.Parse(requiredDate).ToString("yyyyMMdd")
                + " order by a4.SupplierCode";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.MRP.MRPOrderItem()
            {
                LineGUID = dr.Field<string>("GUID").GetString(),
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName = dr.Field<string>("ItemName").GetString(),
                Qty = dr.Field<decimal?>("Qty").ToDouble(3),
                Price = dr.Field<decimal?>("Price").ToDouble(4),
                UOMName = dr.Field<string>("UnitName").GetString(),
                SupplierName = dr.Field<string>("SupplierName").GetString()
            }).ToList();
        }

        public List<Model.MRP.MRPSODetail> GetMRPLineDetails(string lineGuid)
        {
            string sql = "select a3.OrderCode,a4.ItemCode,a4.ItemName,a1.Qty,a2.Price from [dbo].[tblMRPSODetail] a1 "
                + "join [dbo].[tblSaleOrderItem] a2 on a1.SODetailGUID=a2.GUID join [dbo].[tblSaleOrder] a3 "
                + "on a2.SOGUID=a3.GUID join [dbo].[tblItem] a4 on a1.ProductGUID=a4.GUID where MRPLineGUID='" + lineGuid + "' "
                + "order by a1.ID";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.MRP.MRPSODetail()
            {
                OrderCode = dr.Field<string>("OrderCode").GetString(),
                ProductCode = dr.Field<string>("ItemCode").GetString(),
                ProductName = dr.Field<string>("ItemName").GetString(),
                Qty = dr.Field<int?>("Qty").ToInt(),
                Price = dr.Field<decimal?>("Price").ToDouble(4)
            }).ToList();
        }

    }
}
