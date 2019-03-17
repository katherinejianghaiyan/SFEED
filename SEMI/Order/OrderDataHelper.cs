using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Order
{
    public class OrderDataHelper: Common.BaseDataHelper
    {
        private static OrderDataHelper instance = new OrderDataHelper();
        private OrderDataHelper() { }
        public static OrderDataHelper GetInstance() { return instance; }

        #region Sales Order
        public Model.Table.TableMast<Model.Order.SaleOrder> GetSODatas(string maxOrderId,string siteGuid, string orderDate, string orderstatus,string orderCode,string language)
        {
            Model.Table.TableMast<Model.Order.SaleOrder> tableMast = new Model.Table.TableMast<Model.Order.SaleOrder>();
            tableMast.draw = 1;
            tableMast.data = GetSOList(maxOrderId,siteGuid, orderDate,orderstatus,orderCode,language);
            if (tableMast.data == null) tableMast.data = new List<Model.Order.SaleOrder>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Order.SaleOrder> GetSOList(string maxOrderId,string siteGuid, string orderDate, string orderstatus, string orderCode, string language)
        {
            string filterDate = "";
            string filterSiteOC = string.Format(" a1.SiteGuid = '{0}' and a1.orderCode = '{1}' and ",siteGuid,orderCode);
            if (string.IsNullOrWhiteSpace(orderDate)) orderDate = DateTime.Now.ToString("yyyy-MM-dd");

            if (string.IsNullOrWhiteSpace(orderCode)) {
                filterDate = string.Format(" convert(varchar(10),a1.RequiredDate,120)='{0}' and ", DateTime.Parse(orderDate).ToString("yyyy-MM-dd"));
                filterSiteOC = string.Format(" a1.SiteGuid = '{0}' and ", siteGuid);
            }
            

            string sql = "select a1.orderid, "
                + "a3.needWork,a1.WorkedDate,a1.WorkedUser,a1.guid orderguid,a1.ordercode,isnull(a2.UserName,a2.FirstName) UserName,a2.WechatID,a1.RequiredDate,a1.PaymentAmount,"
                + "case a1.paymentid when 10 then '支付宝' when 20 then '微信' when 30 then '员工卡' else '' end paymentmethod,a1.shippeddate,a1.comments,case when a1.ShippedDate is null then '<input type=checkbox class=minimal >' else '' end as status, "
                + "isnull(a1.PhoneNo,a2.Mobile) Mobile,isnull(a2.Department,'') Department,isnull(a2.Section,'') Section,a1.ShipToAddr," +
                "convert(varchar,dateadd(mi,timestep,endtime),120) DeliveryEndTime "
                //"dateadd(mi, timestep, endtime) DeliveryEndTime "
                  + "from tblSaleOrder a1 join tbluser a2 on  a1.userid=a2.userid " +
                "join tblSite a3 on a3.GUID=a1.SiteGuid " +
                "left join  tblDeliveryTimes a4 on a1.siteguid=a4.siteguid and " +
                "convert(time(0),a1.requireddate,8) >= convert(time(0),a4.starttime,8) and " +
                "convert(time(0),a1.requireddate,8) <= convert(time(0),dateadd(mi,timestep,endtime),8) "
                + "where "+ filterSiteOC + " {0}" +
                "a1.IsDel=0 and a1.ispaid=1 {1} {2} "
                + "order by a1.ordercode";
            //sql = string.Format(sql, siteGuid, DateTime.Parse(orderDate).ToString("yyyy-MM-dd"), orderstatus,"");
            string sqlstatus = "";
            switch(orderstatus)
            {
                case "ToBeWorked":
                    {
                        sqlstatus = " and a1.WorkedDate is null and a1.ShippedDate is null";
                        break;
                    }
                case "ToBeShipped":
                    {
                        sqlstatus = " and (a3.needWork=0 or a1.WorkedDate is not null) and a1.shippeddate is null";
                        break;
                    }
                case "Shipped":
                    {
                        sqlstatus = " and a1.shippeddate is not null";
                        break;
                    }
            }
            sql = string.Format(sql, filterDate, sqlstatus,
                string.IsNullOrWhiteSpace(maxOrderId) ? "" : " and a1.orderid " + maxOrderId);

            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);

            if (data == null || data.Rows.Count == 0) return null;

            int dataLength = data.Rows[data.Rows.Count-1].Field<int>("OrderID");


            var list = data.AsEnumerable()
                .Select(dr => 
                    new Model.Order.SaleOrder()
                    {
                        OrderID = dr.Field<int>("orderid"),
                        orderGuid = dr.Field<string>("orderGuid").GetString(),
                        RequiredDate = dr.Field<DateTime>("Requireddate").ToString("yyyy-M-d HH:mm").Trim(),
                        DeliveryEndTime = dr.Field<string>("DeliveryEndTime"),
                        HeadGUID = dr.Field<string>("OrderGuid").GetString().Trim(),
                        OrderCode = dr.Field<string>("OrderCode").GetString().Trim(),
                        WechatID = dr.Field<string>("WechatID").GetString().Trim(),
                        PaymentAmount = dr.Field<decimal>("paymentamount"),
                        PaymentMethod = dr.Field<string>("paymentmethod").GetString().Trim(),
                        UserName = dr.Field<string>("UserName").GetString().Trim(),
                        ShippedDate = dr.Field<DateTime?>("ShippedDate") == null ? "" : dr.Field<DateTime>("ShippedDate").ToString("yyyy-M-d H:m"),
                        WorkedDate = dr.Field<DateTime?>("WorkedDate") == null ? "" : dr.Field<DateTime>("WorkedDate").ToString("yyyy-M-d H:m"),
                        comments = dr.Field<string>("comments") == null ? "" : dr.Field<string>("comments").ToString(),
                        Status = dr.Field<string>("status").GetString(),
                        mobile = dr.Field<string>("Mobile").GetString(),
                        department = dr.Field<string>("Department").GetString(),
                        section = dr.Field<string>("Section").GetString(),
                        shipToAddr = dr.Field<string>("ShipToAddr").GetString(),
                        dataLength = dataLength
                }).ToList();

            List<Model.Order.SaleLine> getSO = GetSO("", language).Lines.ToList();


            list = list.GroupJoin(getSO, a => a.orderGuid, b => b.HeadGUID, (a, b) => new { a, b }).Select(q =>
                       {
                           return new Model.Order.SaleOrder()
                           {
                               WechatID=q.a.WechatID,
                               HeadGUID=q.a.HeadGUID,
                               OrderID=q.a.OrderID,
                               OrderCode = q.a.OrderCode,
                               UserName = q.a.UserName,
                               mobile=q.a.mobile,
                               department=q.a.department,
                               section=q.a.section,
                               RequiredDate=q.a.RequiredDate,
                               DeliveryEndTime = q.a.DeliveryEndTime,
                               PaymentAmount =q.a.PaymentAmount,
                               PaymentMethod=q.a.PaymentMethod,
                               shipToAddr=q.a.shipToAddr,
                               ShippedDate=q.a.ShippedDate,
                               comments=q.a.comments,
                               dataLength=q.a.dataLength,
                               Lines=q.b.ToList()==null?null: q.b.ToList()
                           };
                       }).ToList();

            return list;
        }

        

        public Model.Order.SaleOrder GetSO(string orderGuid, string language)
        {
            string filter = "";
            if (!string.IsNullOrWhiteSpace(orderGuid)) filter = string.Format(" a1.SOGUID = '{0}' and ",orderGuid);
            string sql = "select a1.SOGUID, a2.ItemCode,a2.ItemNameZHCN as ItemName_CN,a2.ItemNameENUS as ItemName_EN,a4.ClassName,a1.price,a1.qty,case '{0}' when 'zh' then isnull(a3.NameCn,'pc') else isnull(a3.nameen,'pc') end unitname "
                + "from tblsaleorderitem a1,tblitem a2,tbluom a3,tblItemClass a4 "
                + "where {1} a1.itemguid=a2.guid and a2.SaleUOMGUID=a3.GUID and a4.GUID=a2.ClassGUID order by a1.id";
            sql = string.Format(sql, language, filter);
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            Model.Order.SaleOrder order = new Model.Order.SaleOrder();
            order.HeadGUID = orderGuid;
            order.Lines = data.AsEnumerable().Select(dr => new Model.Order.SaleLine()
            {
                HeadGUID=dr.Field<string>("SOGUID").GetString(),
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                ClassName = dr.Field<string>("ClassName").GetString(),
                Price = (decimal)dr.Field<decimal?>("Price"),
                Qty = dr.Field<int>("Qty"),
                Amt = (dr.Field<decimal?>("Price").ToDouble(4) * dr.Field<int>("Qty")).ToDouble(2),
                UOMName = dr.Field<string>("UnitName").GetString()
            }).ToList();
            return order;
        }
        public List<Model.Order.SaleLine> PrintSO(string siteguid, string orderDate, string language)
        {
            string sql = "select Row_Number() over (Partition by a1.OrderCode Order by a2.FirstName) as No, "
                        + "case when Row_Number() over(Partition by a1.OrderCode Order by a2.FirstName) > 1 then '' else 'C' end as C, "
                        + "case when Row_Number() over(Partition by a1.OrderCode Order by a2.FirstName) > 1 then '' else a1.OrderCode end as OrderCode, "
                        + "case when Row_Number() over(Partition by a1.OrderCode Order by a2.FirstName) > 1 then '' else a2.FirstName end as UserName, "
                        + "case when Row_Number() over (Partition by a1.OrderCode Order by a2.FirstName)>1 then '' else convert(varchar(10),a1.RequiredDate,120) end as RequiredDate, "
                        + "a4.ItemCode,replace(a4.ItemNameZHCN, '<br>', '\n') as ItemName_CN,replace(a4.ItemNameENUS, '<br>', '\n') as ItemName_EN,a6.ClassName,a3.Qty, "
                        + "a5.NameCn as UOMName from tblSaleOrder a1, tbluser a2, tblsaleorderitem a3,tblItem a4, tbluom a5,tblItemClass a6 "
                        + "where a1.SiteGuid = '{0}' and a3.SOGUID = a1.GUID and a4.guid = a3.ItemGUID and a1.IsDel=0 and "
                        + "a5.GUID = a4.SaleUOMGUID and convert(varchar(10), a1.RequiredDate, 120) = '{1}' and a1.userid = a2.userid and a1.ispaid = 1 and a6.GUID=a4.ClassGUID "
                        + "and a1.ShippedDate is null order by a2.FirstName";
            sql = string.Format(sql, siteguid, DateTime.Parse(orderDate).ToString("yyyy-MM-dd"));
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            Model.Order.SaleLine lines = new Model.Order.SaleLine();
            return data.AsEnumerable().Select(dr => new Model.Order.SaleLine()
            {
                C = dr.Field<string>("C").GetString(),
                OrderCode = dr.Field<string>("OrderCode").GetString(),
                UserName = dr.Field<string>("UserName").GetString(),
                RequiredDate = dr.Field<string>("RequiredDate").GetString(),
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                ClassName = dr.Field<string>("ClassName").GetString(),
                Qty = dr.Field<int>("Qty"),
                UOMName = dr.Field<string>("UOMName").GetString()
            }).ToList();
        }
        #endregion
        #region SO delivery
        public Model.Common.BaseResponse UpdateSOStatus(string headGuid, string orderstatus, string userID, string language)
        {
            return UpdateSOStatus(new List<Model.Order.SaleOrder>() { new Model.Order.SaleOrder() { HeadGUID = headGuid } },
                orderstatus, userID, language);
        }
        public Model.Common.BaseResponse UpdateSOStatus(IList<Model.Order.SaleOrder> headGuids, string orderstatus, string userID, string language)
        {
            Model.Common.BaseResponse response = new Model.Common.BaseResponse();
            try
            {
                if (headGuids == null || headGuids.Count == 0) throw new Exception("No Data to be processed");
                headGuids = headGuids.Where(q => !string.IsNullOrWhiteSpace(q.HeadGUID)).ToList();
                if(!headGuids.Any()) throw new Exception("No Data to be processed");

                string guids = string.Join("','",headGuids.Select(q=>q.HeadGUID).ToArray());
                string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                
                string sql = "update [dbo].[tblsaleorder] set {0}='" + userID + "',{1}='" + createTime + "' {2} "
                    + "where guid in ('" + guids + "') and {0} is null and {1} is null";

                switch (orderstatus)
                {
                    case "ToBeWorked":  // "Worked":
                        {
                            sql = string.Format(sql,"WorkedUser", "WorkedDate","");
                            break;
                        }
                    case "ToBeShipped": //"Shipped":
                        {
                            sql = string.Format(sql, "ShippedUser", "ShippedDate", ",Status='10'");
                            break;
                        }
                }

                //sql = string.Format(sql, userID, createTime, guids);
   
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                int r = dbHelper.Execute(sql);
                if (r < headGuids.Count)
                    throw new Exception("Update Error");
                response.Status = "ok";
            }
            catch (Exception e) { response.Msg = e.Message; }
            return response;
        }
        #endregion

        #region PurchaseOrder
        public Model.Table.TableMast<Model.Order.PurchaseOrder> GetPODatas(string siteGuid, string orderDate, string language)
        {
            Model.Table.TableMast<Model.Order.PurchaseOrder> tableMast = new Model.Table.TableMast<Model.Order.PurchaseOrder>();
            tableMast.draw = 1;
            tableMast.data = GetPOList(siteGuid, orderDate, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Order.PurchaseOrder>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Order.PurchaseOrder> GetPOList(string siteGuid, string orderDate, string language)
        {
            string sql = "select b1.OrderGuid,b1.OrderCode,b1.OrderDate,b1.IsClosed,b1.OrderAmt,"
                + "b2.SupplierCode + '/' + case when 'zh'='" + language + "' then b2.CompNameCn else b2.CompNameEn end as SupplierName "
                + "from (select a1.GUID as OrderGuid,a1.OrderCode,a1.OrderDate,a1.IsClosed,sum(round(a2.Price*Qty,2)) as OrderAmt,"
                + "a1.SupplierGUID from [dbo].[tblPurchaseOrder] a1 join [dbo].[tblPurchaseOrderItem] a2 on a1.GUID=a2.POGUID "
                + "where a1.SiteGuid='" + siteGuid + "' and a1.OrderDate=" + DateTime.Parse(orderDate).ToString("yyyyMMdd") 
                + " group by a1.GUID,a1.OrderCode,a1.OrderDate,a1.IsClosed,a1.SupplierGUID) "
                + "b1 join [dbo].[tblSupplier] b2 on b1.SupplierGUID=b2.SupplierGUID and b2.Active=1 order by b1.OrderCode";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Order.PurchaseOrder()
            {
                OrderDate = dr.Field<int?>("OrderDate").ToDateString("yyyy-MM-dd", string.Empty),
                OrderCode = dr.Field<string>("OrderCode").GetString(),
                OrderGuid = dr.Field<string>("OrderGuid").GetString(),
                OrderAmt = dr.Field<decimal?>("OrderAmt").ToDouble(2),
                Status = dr.Field<bool?>("IsClosed").BoolToInt(),
                SupplierName = dr.Field<string>("SupplierName").GetString()
            }).ToList();
        }

        public Model.Order.PurchaseOrder GetPO(string orderGuid, string language)
        {
            string sql = "select a2.ItemCode,a2.ItemName,a1.Price,a1.Qty,a1.ReceiptQty,case when 'zh'='" + language
                + "' then a3.NameCn else a3.NameEn end as UnitName from [dbo].[tblPurchaseOrderItem] a1 join "
                + "[dbo].[tblItem] a2 on a1.ItemGUID=a2.GUID join [dbo].[tblUOM] a3 on a1.UOMGUID=a3.GUID "
                + "where a1.POGUID='" + orderGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            Model.Order.PurchaseOrder order = new Model.Order.PurchaseOrder();
            order.OrderGuid= orderGuid;
            order.Lines = data.AsEnumerable().Select(dr => new Model.Order.PurchaseOrderLine()
            {
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName = dr.Field<string>("ItemName").GetString(),
                Price = dr.Field<decimal?>("Price").ToDouble(4),
                Qty = dr.Field<decimal?>("Qty").ToDouble(3),
                Amt = (dr.Field<decimal?>("Price").ToDouble(4) * dr.Field<decimal?>("Qty").ToDouble(3)).ToDouble(2),
                ReceiptQty = dr.Field<decimal?>("ReceiptQty").ToDouble(3),
                ReceiptAmt = (dr.Field<decimal?>("Price").ToDouble(4) * dr.Field<decimal?>("ReceiptQty").ToDouble(3)).ToDouble(2),
                UOMName = dr.Field<string>("UnitName").GetString()
            }).ToList();
            return order;
        }


        /// <summary>
        /// 根据SiteGuid和BUGuid获取指定日期的未收货采购订单上的供应商信息
        /// </summary>
        /// <param name="language"></param>
        /// <param name="siteGuid"></param>
        /// <param name="BUGuid"></param>
        /// <param name="receiptDate"></param>
        /// <returns></returns>
        public List<Model.Supplier.SupplierMast> GetReceiptSupplierList(string language, DateTime receiptDate, string siteGuid)
        {
            if (string.IsNullOrWhiteSpace(siteGuid)) return null;
            string sql = "select p2.SupplierGUID,p2.SupplierCode,case when 'zh'='" + language
                + "' then isnull(p2.CompNameCn,p2.CompNameEn) else isnull(p2.CompNameEn,p2.CompNameCn) end SupplierName "
                + "from [dbo].[tblPurchaseOrder] p1,[dbo].[tblSupplier] p2 where p1.SupplierGUID=p2.SupplierGUID "
                + "and p1.IsClosed=0 and p2.Active=1 and p1.SiteGUID='" + siteGuid + "' and p1.OrderDate=" 
                + receiptDate.ToString("yyyyMMdd");
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Supplier.SupplierMast()
            {
                SupplierCode = dr.Field<string>("SupplierCode").GetString(),
                SupplierGuid = dr.Field<string>("SupplierGUID").GetString(),
                SupplierName = dr.Field<string>("SupplierName").GetString()
            }).ToList();
        }

        public Model.Table.TableMast<Model.Order.ReceiptMast> GetReceiptMastDatas(string language, string siteGuid, string supplierGuid, DateTime receiptDate)
        {
            Model.Table.TableMast<Model.Order.ReceiptMast> tableMast = new Model.Table.TableMast<Model.Order.ReceiptMast>();
            tableMast.draw = 1;
            tableMast.data = GetReceiptMastLines(language, siteGuid, supplierGuid, receiptDate);
            if (tableMast.data == null) tableMast.data = new List<Model.Order.ReceiptMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        private IList<Model.Order.ReceiptMast> GetReceiptMastLines(string language, string siteGuid, string supplierGuid, DateTime receiptDate)
        {
            try
            {
                string sql = "select a1.GUID,a3.ItemCode,a3.ItemNameZHCN as ItemName_CN,a3.ItemNameENUS as ItemName_EN,a3.ItemSpec,a1.Price,"
                    + "case when 'zh'='" + language + "' then a4.NameCn else a4.NameEn end ItemUnit,a1.Qty from "
                    + "[dbo].[tblPurchaseOrderItem] a1,[dbo].[tblPurchaseOrder] a2,[dbo].[tblItem] a3,[dbo].[tblUOM] a4 "
                    + "where a1.POGUID=a2.GUID and a1.ItemGUID=a3.GUID and a1.UOMGUID=a4.GUID and a2.IsClosed=0 "
                    + "and a2.SiteGUID='" + siteGuid + "' and a2.OrderDate=" + receiptDate.ToString("yyyyMMdd")
                    + " and a2.SupplierGUID='" + supplierGuid + "' order by a3.ItemCode";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Order.ReceiptMast()
                {
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemSpec = dr.Field<string>("ItemSpec").GetString(),
                    LineGuid = dr.Field<string>("GUID").GetString(),
                    Unit = dr.Field<string>("ItemUnit").GetString(),
                    Price = dr.Field<decimal>("Price"),
                    Qty = dr.Field<decimal>("Qty"),
                    ReceiptQty = dr.Field<decimal>("Qty"),
                    Amt = Math.Round(dr.Field<decimal>("Price") * dr.Field<decimal>("Qty"), 2),
                    ReceiptAmt = Math.Round(dr.Field<decimal>("Price") * dr.Field<decimal>("Qty"), 2)
                }).ToList();
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse ProcessReceiptMastDatas(IList<Model.Order.ReceiptMast> receiptMastDatas, string userID, string language)
        {
            Model.Common.BaseResponse response = new Model.Common.BaseResponse();
            try
            {
                if (receiptMastDatas == null || receiptMastDatas.Count == 0) throw new Exception("No Data to be processed");
                string guid = Guid.NewGuid().ToString();
                string createTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                StringBuilder sqls = new StringBuilder();
                string sql = "update [dbo].[tblPurchaseOrderItem] set [dbo].[tblPurchaseOrderItem].ReceiptQty={1},"
                    + "[dbo].[tblPurchaseOrderItem].ProcessGUID='" + guid + "' from [dbo].[tblPurchaseOrder] "
                    + "where [dbo].[tblPurchaseOrder].GUID=[dbo].[tblPurchaseOrderItem].POGUID and [dbo].[tblPurchaseOrder].IsClosed=0 "
                    + "and [dbo].[tblPurchaseOrderItem].GUID='{0}' and isnull([dbo].[tblPurchaseOrderItem].ProcessGUID,'')='';"
                    + "insert into [dbo].[tblStockTransaction](SiteGUID,PODetailGUID,ItemGUID,Cost,UOMGUID,Qty,CreateTime,CreateUser) "
                    + "select top 1 a2.SiteGUID,a1.GUID,a1.ItemGUID,a1.Price,a1.UOMGUID,{1},'" + createTime + "',{2}"
                    + " from [dbo].[tblPurchaseOrderItem] a1,[dbo].[tblPurchaseOrder] a2 where a1.POGUID=a2.GUID "
                    + "and a2.IsClosed=0 and a1.GUID='{0}' and a1.ProcessGUID='" + guid + "';";
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (Model.Order.ReceiptMast data in receiptMastDatas)
                    sqlBuilder.AppendFormat(sql, data.LineGuid, data.ReceiptQty, userID);
                sqlBuilder.Append("update [dbo].[tblPurchaseOrder] set IsClosed=1 where IsClosed=0 and GUID "
                    + "in (select distinct POGUID from [dbo].[tblPurchaseOrderItem] where ProcessGUID='" + guid + "');");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                int r = dbHelper.Execute(sqlBuilder.ToString());
                if (r > 0) response.Status = "ok";
                else
                {
                    response.Status = "error";
                    response.Msg = "Receipt Error: Data being processed.";
                }
            }
            catch (Exception e) { response.Msg = e.Message; }
            return response;
        }

        #endregion
    }
}
