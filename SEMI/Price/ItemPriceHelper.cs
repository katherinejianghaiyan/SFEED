using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Price
{
    public class ItemPriceHelper : Common.BaseDataHelper
    {
        private static ItemPriceHelper instance = new ItemPriceHelper();
        private ItemPriceHelper() { }
        public static ItemPriceHelper GetInstance() { return instance; }

        #region 采购价目表
        public Model.Table.TableMast<Model.Item.RMPrice> GetTablePurchasePriceList(string supplierGuid, string searchKey, string language)
        {
            Model.Table.TableMast<Model.Item.RMPrice> tableMast = new Model.Table.TableMast<Model.Item.RMPrice>();
            tableMast.draw = 1;
            tableMast.data = GetPurchasePriceList(supplierGuid, searchKey, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.RMPrice>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Item.RMPrice> GetPurchasePriceList(string supplierGuid, string searchKey, string language)
        {
            try
            {
                string sql = "select a1.ID,a3.ItemCode,replace(a3.ItemNameZHCN,'<br>','\n') as ItemName_CN,replace(a3.ItemNameENUS,'<br>','\n') as ItemName_EN,a3.ItemType,a1.StartDate,a1.EndDate,a1.Price,"
                    + "case when 'zh'='zh' then a4.NameCn else a4.NameEn end UnitName from [dbo].[tblItemPrice] a1,"
                    + "(select ItemGUID,SupplierGUID,max(StartDate) maxDate from [dbo].[tblItemPrice] "
                    + "where PriceType='Buy' and SupplierGUID='" + supplierGuid + "' "
                    + "group by SupplierGUID,ItemGUID) a2,[dbo].[tblItem] a3,[dbo].[tblUOM] a4 "
                    + "where a1.ItemGUID=a2.ItemGUID and a1.SupplierGUID=a2.SupplierGUID "
                    + "and a1.StartDate=a2.maxDate and a1.ItemGUID=a3.GUID and a3.Status=1 and a3.ToBuy=1 "
                    + "and a3.PurUOMGUID=a4.GUID and a4.Active=1 and (a3.ItemName like @search or a3.ItemCode like @search) "
                    + "order by a3.ItemCode";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + searchKey.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "PPL", CommandType.Text,
                   new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                int i = 1;
                return data.AsEnumerable().Select(dr => new Model.Item.RMPrice()
                {
                    Sort = i++,
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemType = dr.Field<string>("ItemType").GetString(),
                    startDate = dr.Field<int?>("StartDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Min"),
                    Price = dr.Field<decimal?>("Price").ToDouble(4),
                    UOMName = dr.Field<string>("UnitName").GetString(),
                    RecordID = dr.Field<int>("ID")
                }).ToList();
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse EditPurchasePrice(Model.Item.RMPrice itemPrice, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                int recordID = itemPrice.RecordID.ToInt();
                StringBuilder sql = new StringBuilder();
                DateTime date = DateTime.Now;
                if (!recordID.Equals(0)) //存在记录号,删除或者更改
                {
                    if (itemPrice.IsDel.ToInt().Equals(1))
                    {
                        sql.Append("declare @sdate int;").Append("declare @maxdate int;").Append("declare @item char(36);")
                            .Append("declare @sup char(36);").Append("select @sdate=StartDate,@item=ItemGUID,@sup=SupplierGUID "
                            + "from [dbo].[tblItemPrice] where ID=" + recordID
                            + ";if(@sdate>=" + date.AddDays(1).ToString("yyyyMMdd") + ") begin "
                            + "if(not exists(select top 1 ID from [dbo].[tblItemPrice] where ItemGUID=@item "
                            + "and SupplierGUID=@sup and StartDate>=@sdate and ID<>" + recordID + ")) begin "
                            + "select @maxdate=max(StartDate) from [dbo].[tblItemPrice] where ItemGUID=@item "
                            + "and SupplierGUID=@sup and StartDate<@sdate;update [dbo].[tblItemPrice] set EndDate=null "
                            + "where StartDate=@maxdate and ItemGUID=@item and SupplierGUID=@sup;"
                            + "delete [dbo].[tblItemPrice] where ID=" + recordID + "; end end");
                    }
                    else
                    {
                        sql.Append("declare @sdate int;").Append("declare @item char(36);").Append("declare @sup char(36);")
                            .Append("select @sdate=StartDate,@item=ItemGUID,@sup=SupplierGUID from [dbo].[tblItemPrice] "
                            + "where ID=" + recordID + ";").Append("if(not exists(select top 1 ID from [dbo].[tblItemPrice] "
                            + "where ItemGUID=@item and SupplierGUID=@sup and StartDate>=@sdate and ID<>" + recordID + ")) "
                            + "begin if(@sdate>=" + date.AddDays(1).ToString("yyyyMMdd") + ") begin "
                            + "update [dbo].[tblItemPrice] set Price=" + itemPrice.Price.ToDouble(4) + " where ID=" + recordID
                            + "; end else begin update [dbo].[tblItemPrice] set EndDate=" + date.ToString("yyyyMMdd")
                            + "where ID=" + recordID + ";"
                            + "insert into [dbo].[tblItemPrice](ItemGUID,SupplierGUID,StartDate,Price,PriceType) "
                            + "select ItemGUID,SupplierGUID," + date.AddDays(1).ToString("yyyyMMdd") + "," + itemPrice.Price.ToDouble(4)
                            + ",PriceType from [dbo].[tblItemPrice] where ID=" + recordID + "; end end");
                    }
                }
                else
                {
                    DateTime sdate = DateTime.Parse(itemPrice.startDate);
                    if (sdate < date.AddDays(1)) sdate = date.AddDays(1);
                    sql.Append("declare @sdate int;").Append("select @sdate=max(StartDate) from [dbo].[tblItemPrice] "
                        + "where ItemGUID='" + itemPrice.ItemGuid + "' and SupplierGUID='" + itemPrice.SupplierGuid
                        + "';if(isnull(@sdate,0)<" + sdate.ToString("yyyyMMdd") + ") begin "
                        + "update [dbo].[tblItemPrice] set EndDate=" + sdate.AddDays(-1).ToString("yyyyMMdd")
                        + " where isnull(EndDate,0)=0 and StartDate=@sdate and ItemGUID='" + itemPrice.ItemGuid
                        + "' and SupplierGUID='" + itemPrice.SupplierGuid + "';"
                        + "insert into [dbo].[tblItemPrice](ItemGUID,SupplierGUID,StartDate,Price,PriceType) "
                        + "values('" + itemPrice.ItemGuid + "','" + itemPrice.SupplierGuid + "'," + sdate.ToString("yyyyMMdd")
                        + "," + itemPrice.Price.ToDouble(4) + ",'Buy'); end");
                }
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql.ToString());
                if (count > 0) resp.Status = "ok";
                else
                {
                    resp.Status = "error";
                    resp.Msg = "process failed, please check the start date";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        #endregion

        #region 销售价目表
        public Model.Table.TableMast<Model.Item.FGPrice> GetTableSalesPriceList(string BUGuid, string searchKey, string language)
        {
            Model.Table.TableMast<Model.Item.FGPrice> tableMast = new Model.Table.TableMast<Model.Item.FGPrice>();
            tableMast.draw = 1;
            tableMast.data = GetSalesPriceList(BUGuid, searchKey, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.FGPrice>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Item.FGPrice> GetSalesPriceList(string BUGuid, string searchKey, string language)
        {
            try
            {
                string sql = "select a1.ID,a3.ItemCode,a3.ItemNameZHCN as ItemName_CN,a3.ItemNameENUS as ItemName_EN,a4.ClassName,a1.StartDate,a1.EndDate,a1.Price,a3.Container "
                    + "from [dbo].[tblItemPrice] a1,(select ItemGUID,BUGUID,SiteGUID,max(StartDate) maxDate from [dbo].[tblItemPrice] "
                    + "where PriceType='Sales' and (BUGUID='" + BUGuid + "' or SiteGUID='" + BUGuid + "') group by BUGUID,ItemGUID,SiteGUID) a2,[dbo].[tblItem] a3, tblItemClass a4 "
                    + "where a1.ItemGUID=a2.ItemGUID and (a1.BUGUID=a2.BUGUID or a1.SiteGUID=a2.SiteGUID) and a1.StartDate=a2.maxDate and a1.ItemGUID=a3.GUID and a4.GUID=isnull(a3.ClassGUID,'E6A44E42-A108-4B75-B5A8-41DFEF6F8518') "
                    + "and a3.Status=1 and a3.ToSell=1 and (a3.ItemName like @search or a3.ItemCode like @search) "
                    + "order by a3.ItemCode";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + searchKey.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "SPL", CommandType.Text,
                   new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                int i = 1;
                return data.AsEnumerable().Select(dr => new Model.Item.FGPrice()
                {
                    Sort = i++,
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemClassName = dr.Field<string>("ClassName").GetString(),
                    startDate = dr.Field<int?>("StartDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Min"),
                    endDate=dr.Field<int?>("EndDate").ToDateString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"), "Max"),
                    Price = dr.Field<decimal?>("Price").ToDouble(4),
                    Container = dr.Field<string>("Container").GetString(),
                    RecordID = dr.Field<int>("ID")
                }).ToList();
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse ModifySalesPrice(Model.Item.FGPrice itemPrice, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                int recordID = itemPrice.RecordID.ToInt();
                StringBuilder sql = new StringBuilder();
                DateTime date = DateTime.Now;
                // 更新BU的销售价目表
                if (string.IsNullOrWhiteSpace(itemPrice.SiteGUID))
                {
                    if (!recordID.Equals(0)) //存在记录号,删除或者更改
                    {
                        if (itemPrice.IsDel.ToInt().Equals(1))
                        {
                            sql.Append("declare @sdate int;").Append("declare @maxdate int;").Append("declare @item char(36);")
                                .Append("declare @bu char(36);").Append("select @sdate=StartDate,@item=ItemGUID,@bu=BUGUID "
                                + "from [dbo].[tblItemPrice] where ID=" + recordID + ";if(@sdate>=" + date.AddDays(1).ToString("yyyyMMdd")
                                + ") begin if(not exists(select top 1 ID from [dbo].[tblItemPrice] where ItemGUID=@item "
                                + "and BUGUID=@bu and StartDate>=@sdate and ID<>" + recordID + ")) begin "
                                + "select @maxdate=max(StartDate) from [dbo].[tblItemPrice] where ItemGUID=@item "
                                + "and BUGUID=@bu and StartDate<@sdate;update [dbo].[tblItemPrice] set EndDate=null "
                                + "where StartDate=@maxdate and ItemGUID=@item and BUGUID=@bu;"
                                + "delete [dbo].[tblItemPrice] where ID=" + recordID + "; end end");
                        }
                        else
                        {
                            sql.Append("declare @sdate int;").Append("declare @item char(36);").Append("declare @bu char(36);")
                                .Append("select @sdate=StartDate,@item=ItemGUID,@bu=BUGUID from [dbo].[tblItemPrice] "
                                + "where ID=" + recordID + ";").Append("if(not exists(select top 1 ID from [dbo].[tblItemPrice] "
                                + "where ItemGUID=@item and BUGUID=@bu and StartDate>=@sdate and ID<>" + recordID + ")) "
                                + "begin if(@sdate>=" + date.AddDays(1).ToString("yyyyMMdd") + ") begin "
                                + "update [dbo].[tblItemPrice] set Price=" + itemPrice.Price.ToDouble(4) + " where ID=" + recordID
                                + "; end else begin update [dbo].[tblItemPrice] set EndDate=" + date.ToString("yyyyMMdd")
                                + "where ID=" + recordID + ";"
                                + "insert into [dbo].[tblItemPrice](ItemGUID,BUGUID,StartDate,Price,PriceType) "
                                + "select ItemGUID,BUGUID," + date.AddDays(1).ToString("yyyyMMdd") + "," + itemPrice.Price.ToDouble(4)
                                + ",PriceType from [dbo].[tblItemPrice] where ID=" + recordID + "; end end");
                        }
                    }
                    else
                    {
                        DateTime sdate = DateTime.Parse(itemPrice.startDate);
                        if (sdate < date.AddDays(1)) sdate = date.AddDays(1);
                        sql.Append("declare @sdate int;").Append("select @sdate=max(StartDate) from [dbo].[tblItemPrice] "
                            + "where ItemGUID='" + itemPrice.ItemGuid + "' and BUGUID='" + itemPrice.BUGuid
                            + "';if(isnull(@sdate,0)<" + sdate.ToString("yyyyMMdd") + ") begin "
                            + "update [dbo].[tblItemPrice] set EndDate=" + sdate.AddDays(-1).ToString("yyyyMMdd")
                            + " where isnull(EndDate,0)=0 and StartDate=@sdate and ItemGUID='" + itemPrice.ItemGuid
                            + "' and BUGUID='" + itemPrice.BUGuid + "';"
                            + "insert into [dbo].[tblItemPrice](ItemGUID,BUGUID,StartDate,Price,PriceType) "
                            + "values('" + itemPrice.ItemGuid + "','" + itemPrice.BUGuid + "'," + sdate.ToString("yyyyMMdd")
                            + "," + itemPrice.Price.ToDouble(4) + ",'Sales'); end");
                    }
                    int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql.ToString());
                    if (count > 0) resp.Status = "ok";
                    else
                    {
                        resp.Status = "error";
                        resp.Msg = "process failed, please check the start date";
                    }
                }
                // 跟新英语点销售价目表
                else if (!string.IsNullOrWhiteSpace(itemPrice.SiteGUID))
                {
                    DateTime edate = DateTime.Parse(itemPrice.endDate);
                    DateTime sdate = DateTime.Parse(itemPrice.startDate);
                    // 处理旧产品销售价格
                    if (!recordID.Equals(0))
                    {

                        sql.Append("declare @sdate int; ").Append("declare @eDate int; ").Append("declare @price decimal; ")
                            .Append(" select @sdate=StartDate,@eDate=EndDate,@price=Price from [dbo].[tblItemPrice] where ID=" + recordID + ";")
                            .Append(" if(@price-"+itemPrice.Price+"<>0)")
                            .Append("begin if(@sdate>=" + date.AddDays(1).ToString("yyyyMMdd") + ") begin "
                                    + "update [dbo].[tblItemPrice] set EndDate='" + edate.ToString("yyyyMMdd") + "',Price='" + itemPrice.Price.ToDouble(4) + "' "
                                    + "where ID=" + recordID + "; end else begin update [dbo].[tblItemPrice] set EndDate='"+date.ToString("yyyyMMdd")+ "' where ID=" + recordID + ";" 
                                    + " insert into [dbo].[tblItemPrice] (ItemGUID,SiteGUID,StartDate,Price,PriceType,CreateTime) "
                                    + "select ItemGUID,SiteGUID,"+date.AddDays(1).ToString("yyyyMMdd")+","+itemPrice.Price+",PriceType, "
                                    + " '"+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")+ "' from [dbo].[tblItemPrice] where ID=" + recordID + "; end end;")
                            .Append("if(@price-" + itemPrice.Price + "=0)")
                            .Append("begin if(@eDate<>"+edate.ToString("yyyyMMdd")+") begin "
                                    + "update [dbo].[tblItemPrice] set EndDate=" + date.ToString("yyyyMMdd")
                                + "where ID=" + recordID + ";end;end;");
                            
                    }
                    // 处理新建产品销售价格
                    else if (recordID.Equals(0)) //无记录号,新建
                    {
                        sql.Append(string.Format(" insert into [dbo].[tblItemPrice] (ItemGUID,SiteGUID,StartDate,EndDate,Price,PriceType,CreateTime) "
                            + "select top 1 '{0}','{1}','{2}','{3}','{4}','{5}','{6}' from [dbo].[tblItemPrice] where not exists (select * from [dbo].[tblItemPrice] where ItemGuid='{7}' and (SiteGuid='{1}' or BUGuid='{1}'))", 
                            itemPrice.ItemGuid,itemPrice.SiteGUID,sdate.ToString("yyyyMMdd"),edate.ToString("yyyyMMdd"),itemPrice.Price,
                            "Sales",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),itemPrice.ItemGuid));
                    }
                    int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql.ToString());
                    if (count > 0) resp.Status = "ok";
                    else
                    {
                       resp.Status = "error";
                       resp.Msg = "process failed, please check the start date and check if the item is already set";
                    }
                   
                }
                
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        

        #endregion

        #region 批量导入采购价目表
        public string UploadPurchasePriceList(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) throw new Exception("上传文件未找到.文件名为空,请联系管理员");
            DataSet ds = Utils.Excel.ExcelHelper.GetInstance().GetDataSet(file, new List<string>() { "instruction" }, 0, 1, 0);
            if (ds == null || ds.Tables.Count == 0) throw new Exception("Excel转换失败,请检查Excel文件是否符合要求");
            List<string> supCodeList = new List<string>(ds.Tables.Count);
            foreach (DataTable dt in ds.Tables)
            {
                if (!dt.Columns.Contains("ItemCode") || !dt.Columns.Contains("Price") || !dt.Columns.Contains("StartDate"))
                    throw new Exception("工作表标题错误,工作表:" + dt.TableName + ",标题必须包含(ItemCode,Price,StartDate)");
                if (dt.AsEnumerable().Select(dr => dr.Field<string>("ItemCode").GetString()).Distinct().ToList().Count < dt.Rows.Count)
                    throw new Exception("工作表中存在重复的原料代码,工作表:" + dt.TableName);
                supCodeList.Add(dt.TableName.GetString().SqlEscapeString());
            }
            string supSql = "select SupplierGUID,SupplierCode from [dbo].[tblSupplier] where Active=1 "
                + " and SupplierCode in (" + string.Join(",", supCodeList.Select(sup => "'" + sup + "'").ToArray()) + ")";
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable supData = helper.GetDataTable(supSql);
            if (supData == null || supData.Rows.Count == 0) throw new Exception("供应商代码错误,请检查所有工作表名称");
            var query = (from s1 in supCodeList
                         join s2 in supData.AsEnumerable()
                         on s1 equals s2.Field<string>("SupplierCode").GetString() into ls
                         from lrs in ls.DefaultIfEmpty()
                         where lrs == null
                         select s1);
            if (query.Any()) throw new Exception("供应商代码错误,工作表:" + string.Join(",", query));
            DataSet retDs = null;
            DataTable retData = null;
            string processSql = "declare @guid char(36);select @guid=GUID from [dbo].[tblItem] where ItemCode='{0}' and ToBuy=1 "
                + "and Status=1;if(isnull(@guid,'')<>'') begin declare @id int;declare @sdate int; select @id=ID,@sdate=StartDate "
                + "from [dbo].[tblItemPrice] where ItemGUID=@guid and SupplierGUID='{1}' and isnull(EndDate,0)=0 and PriceType='Buy';"
                + "if(isnull(@id,0)<>0) begin if(@sdate<={2}) begin "
                + "update [dbo].[tblItemPrice] set EndDate={2} where ID=@id;"
                + "insert into [dbo].[tblItemPrice](ItemGUID,SupplierGUID,StartDate,Price,PriceType) "
                + "values(@guid,'{1}',{3},{4},'Buy');end end else begin "
                + "insert into [dbo].[tblItemPrice](ItemGUID,SupplierGUID,StartDate,Price,PriceType) "
                + "values(@guid,'{1}',{3},{4},'Buy');end end";
            int sDate = DateTime.Now.AddDays(1).ToString("yyyyMMdd").ToInt();
            foreach (DataRow drSup in supData.Rows)
            {
                retData = ProcessPurchasePriceList(ds.Tables[drSup.Field<string>("SupplierCode").GetString()], processSql,
                    drSup.Field<string>("SupplierGUID").GetString(), drSup.Field<string>("SupplierCode").GetString(), helper, sDate);
                if (retData != null && retData.Rows.Count > 0)
                {
                    if (retDs == null) retDs = new DataSet();
                    retDs.Tables.Add(retData);
                }
            }
            if (retDs != null && retDs.Tables.Count > 0)
                return Utils.Excel.ExcelHelper.GetInstance().SaveDataSet(retDs, System.IO.Path.GetDirectoryName(file),
                    System.IO.Path.GetExtension(file));
            return string.Empty;
        }

        private DataTable ProcessPurchasePriceList(DataTable priceData, string processSql, string supplierGuid, string supplierCode,
            Utils.Database.SqlServer.DBHelper helper, int sDate)
        {
            DataTable retData = Utils.Common.Functions.CreateTableStructAsString(
                new List<string>() { "ItemCode", "Price", "StartDate", "Reason" }, supplierCode);
            string itemCode = string.Empty;
            string date = string.Empty;
            double price = 0;
            int count = 0;
            foreach (DataRow dr in priceData.Rows)
            {
                itemCode = dr.Field<string>("ItemCode").GetString();
                try { date = DateTime.Parse(dr.Field<string>("StartDate").GetString()).ToString("yyyyMMdd"); }
                catch { date = string.Empty; }
                price = dr.Field<string>("Price").GetString().ToDouble(4);
                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(date) || price <= 0 || date.ToInt() < sDate)
                {
                    retData.Rows.Add(dr.Field<string>("ItemCode").GetString(), dr.Field<string>("Price").GetString(),
                        dr.Field<string>("StartDate").GetString(), "Item代码为空或日期格式错误或日期小于明天或价格错误");
                    continue;
                }
                count = helper.Execute(string.Format(processSql, itemCode.SqlEscapeString(), supplierGuid,
                    DateTime.Parse(dr.Field<string>("StartDate").GetString()).AddDays(-1).ToString("yyyyMMdd"), date, price));
                if (count <= 0)
                {
                    retData.Rows.Add(dr.Field<string>("ItemCode").GetString(), dr.Field<string>("Price").GetString(),
                        dr.Field<string>("StartDate").GetString(), "数据执行失败,原料代码不存在或开始日期小于已存在价目表");
                }
            }
            return retData;
        }

        #endregion

        #region 批量导入销售价目表
        public string UploadSalesPriceList(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) throw new Exception("上传文件未找到.文件名为空,请联系管理员");
            DataSet ds = Utils.Excel.ExcelHelper.GetInstance().GetDataSet(file, new List<string>() { "instruction" }, 0, 1, 0);
            if (ds == null || ds.Tables.Count == 0) throw new Exception("Excel转换失败,请检查Excel文件是否符合要求");
            List<string> BUCodeList = new List<string>(ds.Tables.Count);
            foreach (DataTable dt in ds.Tables)
            {
                if (!dt.Columns.Contains("ItemCode") || !dt.Columns.Contains("Price") || !dt.Columns.Contains("StartDate"))
                    throw new Exception("工作表标题错误,工作表:" + dt.TableName + ",标题必须包含(ItemCode,Price,StartDate)");
                if (dt.AsEnumerable().Select(dr => dr.Field<string>("ItemCode").GetString()).Distinct().ToList().Count < dt.Rows.Count)
                    throw new Exception("工作表中存在重复的成品代码,工作表:" + dt.TableName);
                BUCodeList.Add(dt.TableName.GetString().SqlEscapeString());
            }
            string BUSql = "select BUGUID,Code from tblBU where Code in (" 
                + string.Join(",", BUCodeList.Select(BU => "'" + BU + "'").ToArray()) + ")";
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable BUData = helper.GetDataTable(BUSql);
            if (BUData == null || BUData.Rows.Count == 0) throw new Exception("公司代码错误,请检查所有工作表名称");
            var query = (from s1 in BUCodeList
                         join s2 in BUData.AsEnumerable()
                         on s1 equals s2.Field<string>("Code").GetString() into ls
                         from lrs in ls.DefaultIfEmpty()
                         where lrs == null
                         select s1);
            if (query.Any()) throw new Exception("公司代码错误,工作表:" + string.Join(",", query));
            DataSet retDs = null;
            DataTable retData = null;
            string processSql = "declare @guid char(36);select @guid=GUID from [dbo].[tblItem] where ItemCode='{0}' and ToSell=1 "
                + "and Status=1;if(isnull(@guid,'')<>'') begin declare @id int;declare @sdate int; select @id=ID,@sdate=StartDate "
                + "from [dbo].[tblItemPrice] where ItemGUID=@guid and BUGUID='{1}' and isnull(EndDate,0)=0 and PriceType='Sales';"
                + "if(isnull(@id,0)<>0) begin if(@sdate<={2}) begin "
                + "update [dbo].[tblItemPrice] set EndDate={2} where ID=@id;"
                + "insert into [dbo].[tblItemPrice](ItemGUID,BUGUID,StartDate,Price,PriceType) "
                + "values(@guid,'{1}',{3},{4},'Sales');end end else begin "
                + "insert into [dbo].[tblItemPrice](ItemGUID,BUGUID,StartDate,Price,PriceType) "
                + "values(@guid,'{1}',{3},{4},'Sales');end end";
            int sDate = DateTime.Now.AddDays(1).ToString("yyyyMMdd").ToInt();
            foreach (DataRow drBU in BUData.Rows)
            {
                retData = ProcessSalesPriceList(ds.Tables[drBU.Field<string>("Code").GetString()], processSql,
                    drBU.Field<string>("BUGUID").GetString(), drBU.Field<string>("Code").GetString(), helper, sDate);
                if (retData != null && retData.Rows.Count > 0)
                {
                    if (retDs == null) retDs = new DataSet();
                    retDs.Tables.Add(retData);
                }
            }
            if (retDs != null && retDs.Tables.Count > 0)
                return Utils.Excel.ExcelHelper.GetInstance().SaveDataSet(retDs, System.IO.Path.GetDirectoryName(file),
                    System.IO.Path.GetExtension(file));
            return string.Empty;
        }

        private DataTable ProcessSalesPriceList(DataTable priceData, string processSql, string BUGuid, string BUCode,
            Utils.Database.SqlServer.DBHelper helper, int sDate)
        {
            DataTable retData = Utils.Common.Functions.CreateTableStructAsString(
                new List<string>() { "ItemCode", "Price", "StartDate", "Reason" }, BUCode);
            string itemCode = string.Empty;
            string date = string.Empty;
            double price = 0;
            int count = 0;
            foreach (DataRow dr in priceData.Rows)
            {
                itemCode = dr.Field<string>("ItemCode").GetString();
                try { date = DateTime.Parse(dr.Field<string>("StartDate").GetString()).ToString("yyyyMMdd"); }
                catch { date = string.Empty; }
                price = dr.Field<string>("Price").GetString().ToDouble(4);
                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(date) || price <= 0 || date.ToInt() < sDate)
                {
                    retData.Rows.Add(dr.Field<string>("ItemCode").GetString(), dr.Field<string>("Price").GetString(),
                        dr.Field<string>("StartDate").GetString(), "成品代码为空或日期格式错误或日期小于明天或价格错误");
                    continue;
                }
                count = helper.Execute(string.Format(processSql, itemCode.SqlEscapeString(), BUGuid,
                    DateTime.Parse(dr.Field<string>("StartDate").GetString()).AddDays(-1).ToString("yyyyMMdd"), date, price));
                if (count <= 0)
                {
                    retData.Rows.Add(dr.Field<string>("ItemCode").GetString(), dr.Field<string>("Price").GetString(),
                        dr.Field<string>("StartDate").GetString(), "数据执行失败,成品代码不存在或开始日期小于已存在价目表");
                }
            }
            return retData;
        }

        #endregion
    }
}
