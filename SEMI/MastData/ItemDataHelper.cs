using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.MastData
{
    public class ItemDataHelper : Common.BaseDataHelper
    {
        private static ItemDataHelper instance = new ItemDataHelper();
        private ItemDataHelper() { }
        public static ItemDataHelper GetInstance() { return instance; }

        #region Item 主数据

        #region ItemList
        /// <summary>
        /// 用Table封装数据对象, 返回的JSON形式{"draw":"1","data":{[],[]}..}
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <param name="status">Item状态,0或1</param>
        /// <param name="keyWords">搜索关键字</param>
        /// <returns></returns>
        public Model.Table.TableMast<Model.Item.RMMast> GetTableRMList(int status, string keyWords, bool nullParent, string language)
        {
            Model.Table.TableMast<Model.Item.RMMast> tableMast = new Model.Table.TableMast<Model.Item.RMMast>();
            tableMast.draw = 1;
            tableMast.data = GetRMList(status, keyWords, nullParent, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.RMMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public Model.Table.TableMast<Model.Item.FGMast> GetTableFGList(int status, string keyWords, string language)
        {
            Model.Table.TableMast<Model.Item.FGMast> tableMast = new Model.Table.TableMast<Model.Item.FGMast>();
            tableMast.draw = 1;
            tableMast.data = GetFGList(status, keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.FGMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public List<Model.Item.RMMast> GetRMList(int status, string keyWords, bool nullParent, string language)
        {
            try
            {
                string sql = "select a1.GUID,a1.ItemNameZHCN as ItemName_CN,a1.ItemNameENUS as ItemName_EN,a1.ItemType,a1.ItemCode,a1.ItemSpec,a1.Loss,a1.PurchasePolicy,a1.CreateTime,"
                    + "a3.ItemName as ClassName,case when 'zh'='" + language + "' then a2.NameCn else a2.NameEn end ItemUnit "
                    + "from [dbo].[tblItem] a1 join [dbo].[tblUom] a2 on a1.PurUOMGUID=a2.GUID left join [dbo].[tblItem] a3 "
                    + "on a1.ParentGUID=a3.GUID and a3.ToBuy=1 where a1.ToBuy=1 and a1.Status=" + status
                    + (nullParent ? " and isnull(a1.ParentGUID,'')=''" : "")
                    + " and (a1.ItemName like @search or a1.ItemCode like @search) order by a1.Sort";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "ItemMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Item.RMMast()
                {
                    ItemClassName = dr.Field<string>("ClassName").GetString(),
                    ItemGuid = dr.Field<string>("GUID").GetString(),
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemUnit = dr.Field<string>("ItemUnit").GetString(),
                    ItemSpec = dr.Field<string>("ItemSpec").GetString(),
                    ItemType = dr.Field<string>("ItemType").GetString(),
                    ItemCreateTime = dr.Field<DateTime?>("CreateTime").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd HH:mm:ss" : "MM/dd/yyyy HH:mm:ss", "Min"),
                    ItemLoss = dr.Field<int?>("Loss") == null ? 0 : dr.Field<int>("Loss"),
                    PurchasePolicy = dr.Field<string>("PurchasePolicy").GetString()
                }).ToList();
            }
            catch { return null; }
        }
        public List<Model.Item.FGMast> GetFGList(int status, string keyWords, string language)
        {
            try
            {
                string sql = "select a1.GUID,a1.ItemName,a1.ItemNameZHCN as ItemName_CN,a1.ItemNameENUS as ItemName_EN,a1.ItemCode,a2.ClassName,a1.DishSize,a1.Container,a1.CreateTime,a1.Weight,"
                    + "a1.Sort,a1.OtherCost from [dbo].[tblItem] a1 left join [dbo].[tblItemClass] a2 "
                    + "on a1.ClassGUID=a2.GUID where a1.ToSell=1 "
                    + "and a1.Status=" + status + " and (a1.ItemName like @search or a1.ItemCode like @search) order by a1.Sort";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "ItemMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Item.FGMast()
                {
                    ItemClassName = dr.Field<string>("ClassName").GetString(),
                    ItemGuid = dr.Field<string>("GUID").GetString(),
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemDishSize = dr.Field<string>("DishSize").GetString(),
                    ItemContainer = dr.Field<string>("Container").GetString(),
                    ItemSort = dr.Field<int?>("Sort").ToInt(),
                    ItemWeight = dr.Field<int?>("Weight").ToInt(),
                    OtherCost = dr.Field<decimal?>("OtherCost").ToDouble(2),
                    ItemCreateTime = dr.Field<DateTime?>("CreateTime").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd HH:mm:ss" : "MM/dd/yyyy HH:mm:ss", "Min")
                }).ToList();
            }
            catch { return null; }
        }
        #endregion

        #region Item
        public Model.Item.RMMast GetRM(string language, string guid)
        {
            try
            {
                string sql = "select top 1 a1.ItemName,a1.ItemNameENUS as ItemName_EN,a1.ItemCode,a1.ItemSpec,a1.Loss,a1.PurchasePolicy,a3.GUID as ClassGUID,"
                    + "a3.ItemName as ClassName,a3.ItemCode as ClassCode,a2.GUID as UOMGUID,a1.Status,"
                    + "case when 'zh'='" + language + "' then a2.NameCn else a2.NameEn end ItemUnit,a1.ToSell,"
                    + "a1.PurUOMGUID from [dbo].[tblItem] a1 join [dbo].[tblUom] a2 on a1.PurUOMGUID=a2.GUID "
                    + "left join [dbo].[tblItem] a3 on a1.ParentGUID=a3.GUID and a3.ToBuy=1 where a1.ToBuy=1 "
                    + "and a1.GUID='" + guid + "'";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count != 1) return null;
                return new Model.Item.RMMast()
                {
                    ItemClassName = data.Rows[0].Field<string>("ClassName").GetString(),
                    ItemGuid = guid,
                    ItemCode = data.Rows[0].Field<string>("ItemCode").GetString(),
                    ItemName = data.Rows[0].Field<string>("ItemName").GetString(),
                    ItemName_EN=data.Rows[0].Field<string>("ItemName_EN").GetString(),
                    ItemUnit = data.Rows[0].Field<string>("ItemUnit").GetString(),
                    ItemSpec = data.Rows[0].Field<string>("ItemSpec").GetString(),
                    ItemLoss = data.Rows[0].Field<int?>("Loss") == null ? 0 : data.Rows[0].Field<int>("loss"),
                    ItemClassGuid = data.Rows[0].Field<string>("ClassGUID").GetString(),
                    ItemUnitGuid = data.Rows[0].Field<string>("PurUOMGUID").GetString(),
                    ItemStatus = data.Rows[0].Field<bool>("Status") ? 1 : 0,
                    ItemSell = data.Rows[0].Field<bool?>("ToSell").BoolToInt(),
                    ItemClassCode = data.Rows[0].Field<string>("ClassCode").GetString(),
                    PurchasePolicy = data.Rows[0].Field<string>("PurchasePolicy").GetString()
                };
            }
            catch { return null; }
        }
        public Model.Item.FGMast GetFG(string language, string guid)
        {
            try
            {
                List<Utils.Database.SqlServer.DBQueryDic> sqlDics = new List<Utils.Database.SqlServer.DBQueryDic>();
                sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
                {
                    Sql = "select ItemName,ItemNameZHCN as ItemName_CN,ItemNameENUS as ItemName_EN,ItemCode,ToBuy,Weight,Sort,Status,DishSize,Container,Cooking,Nutrition,Tips,"
                        + "Image1,Image2,Image3,ClassGUID,OtherCost from [dbo].[tblItem] where ToSell=1 and GUID='" + guid + "'",
                    TableName = "ItemMast"
                });
                sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
                {
                    Sql = "select DictCode,PropValue from [dbo].[tblItemPropery] where ItemGUID='" + guid + "'",
                    TableName = "ItemPropery"
                });
                DataSet ds = new Utils.Database.SqlServer.DBHelper(_conn).GetDataSet(sqlDics);
                if (ds == null) return null;
                DataTable data = ds.Tables["ItemMast"];
                DataTable propery = ds.Tables["ItemPropery"];
                if (data == null || data.Rows.Count != 1) return null;
                return new Model.Item.FGMast()
                {
                    ItemClassGuid = data.Rows[0].Field<string>("ClassGUID").GetString(),
                    ItemGuid = guid,
                    ItemStatus = data.Rows[0].Field<bool?>("Status").BoolToInt(),
                    ItemCode = data.Rows[0].Field<string>("ItemCode").GetString(),
                    ItemName = data.Rows[0].Field<string>("ItemName").GetString(),
                    ItemName_CN = data.Rows[0].Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = data.Rows[0].Field<string>("ItemName_EN").GetString(),
                    ItemDishSize = data.Rows[0].Field<string>("DishSize").GetString(),
                    ItemContainer = data.Rows[0].Field<string>("Container").GetString(),
                    ItemCooking = data.Rows[0].Field<string>("Cooking").GetString(),
                    ItemTips = data.Rows[0].Field<string>("Tips").GetString(),
                    ItemNutrition = data.Rows[0].Field<string>("Nutrition").GetString(),
                    ItemSort = data.Rows[0].Field<int?>("Sort").ToInt(),
                    ItemWeight = data.Rows[0].Field<int?>("Weight").ToInt(),
                    ItemBuy = data.Rows[0].Field<bool?>("ToBuy").BoolToInt(),
                    ImageName1 = System.IO.Path.GetFileName(data.Rows[0].Field<string>("Image1").GetString()),
                    Image1 = data.Rows[0].Field<string>("Image1").GetString(),
                    ImageName2 = System.IO.Path.GetFileName(data.Rows[0].Field<string>("Image2").GetString()),
                    Image2 = data.Rows[0].Field<string>("Image2").GetString(),
                    ImageName3 = System.IO.Path.GetFileName(data.Rows[0].Field<string>("Image3").GetString()),
                    Image3 = data.Rows[0].Field<string>("Image3").GetString(),
                    OtherCost = data.Rows[0].Field<decimal?>("OtherCost").ToDouble(2),
                    ItemProperies = (propery == null || propery.Rows.Count == 0 ? null
                        : propery.AsEnumerable().Select(pr => new Model.Item.ItemPropery()
                        {
                            DictCode = pr.Field<string>("DictCode").GetString(),
                            PropValue = pr.Field<string>("PropValue").GetString()
                        }).ToList())
                };
            }
            catch { return null; }
        }

        #endregion

        #region Modify Item
        public Model.Common.BaseResponse ModifyRM(Model.Item.RMMast item, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (item == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                string ckStr = "select top 1 GUID from [dbo].[tblItem] where ItemCode='" + item.ItemCode.GetString() + "';";
                ckStr = helper.GetDataScalar(ckStr).GetString();
                if (string.IsNullOrWhiteSpace(item.ItemGuid))
                {
                    if (!string.IsNullOrWhiteSpace(ckStr)) throw new Exception("Item code already exists");
                    sql = "insert into [dbo].[tblItem](GUID,ParentGUID,ItemType,ToBuy,ToSell,ItemName,ItemNameZHCN,,ItemNameENUS,ItemCode,ItemSpec,"
                       + "PurUOMGUID,Loss,Status,CreateTime,LastUpdate,IsDel,PurchasePolicy) "
                       + "values(newid(),@pguid,'RM',1,@sell,@name,@name,@name_EN,@code,@spec,@uomguid,@loss,@status,@date,@date,0,@policy);";
                }
                else
                {
                    if(!string.IsNullOrWhiteSpace(ckStr) && !ckStr.Equals(item.ItemGuid))
                        throw new Exception("Item code already exists");
                    sql = "update [dbo].[tblItem] set ItemCode=@code,ItemName=@name,ItemNameZHCN=@name,ItemNameENUS=@name_EN,ItemSpec=@spec,PurUOMGUID=@uomguid,"
                         + "ParentGUID=@pguid,ToSell=@sell,Loss=@loss,Status=@status,LastUpdate=@date,PurchasePolicy=@policy "
                         + "where GUID='" + item.ItemGuid + "'";
                }
                System.Data.SqlClient.SqlParameter date = new System.Data.SqlClient.SqlParameter("@date", SqlDbType.DateTime, 23);
                date.Value = DateTime.Now;
                System.Data.SqlClient.SqlParameter code = new System.Data.SqlClient.SqlParameter("@code", SqlDbType.VarChar, 32);
                code.Value = item.ItemCode.GetString();
                System.Data.SqlClient.SqlParameter name = new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 128);
                name.Value = item.ItemName.GetString();
                System.Data.SqlClient.SqlParameter name_EN = new System.Data.SqlClient.SqlParameter("@name_EN", SqlDbType.VarChar, 128);
                name_EN.Value = item.ItemName_EN.GetString();
                System.Data.SqlClient.SqlParameter spec = new System.Data.SqlClient.SqlParameter("@spec", SqlDbType.VarChar, 32);
                if (string.IsNullOrWhiteSpace(item.ItemSpec)) spec.Value = DBNull.Value;
                else spec.Value = item.ItemSpec.GetString();
                System.Data.SqlClient.SqlParameter uom = new System.Data.SqlClient.SqlParameter("@uomguid", SqlDbType.Char, 36);
                uom.Value = item.ItemUnitGuid.GetString();
                System.Data.SqlClient.SqlParameter pguid = new System.Data.SqlClient.SqlParameter("@pguid", SqlDbType.Char, 36);
                if (string.IsNullOrWhiteSpace(item.ItemClassGuid)) pguid.Value = DBNull.Value;
                else pguid.Value = item.ItemClassGuid.GetString();
                System.Data.SqlClient.SqlParameter sell = new System.Data.SqlClient.SqlParameter("@sell", SqlDbType.Bit, 1);
                sell.Value = item.ItemSell;
                System.Data.SqlClient.SqlParameter loss = new System.Data.SqlClient.SqlParameter("@loss", SqlDbType.Int, 5);
                loss.Value = item.ItemLoss;
                System.Data.SqlClient.SqlParameter policy = new System.Data.SqlClient.SqlParameter("@policy", SqlDbType.VarChar, 10);
                policy.Value = item.PurchasePolicy;
                System.Data.SqlClient.SqlParameter status = new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                status.Value = item.ItemStatus;
                int count = helper.Execute(sql, CommandType.Text,
                          new System.Data.SqlClient.SqlParameter[] { date, code, name, name_EN, spec, uom, pguid, sell, loss, status, policy });
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = item.ItemCode;
                }
                else
                {
                    resp.Status = "error";
                    resp.Msg = "No record been modified.";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        public Model.Common.BaseResponse ModifyFG(Model.Item.FGMast item, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (item == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                string propSql = string.Empty;
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);

                string ckStr = "select top 1 GUID from [dbo].[tblItem] where ItemCode='" + item.ItemCode.GetString() + "';";
                ckStr = helper.GetDataScalar(ckStr).GetString();

                if (string.IsNullOrWhiteSpace(item.ItemGuid))
                {
                    if (!string.IsNullOrWhiteSpace(ckStr)) throw new Exception("Item code already exists");

                    string guid = Guid.NewGuid().ToString().ToUpper();

                    sql = "insert into [dbo].[tblItem](GUID,ItemType,ToBuy,ToSell,ItemName,ItemNameZHCN,ItemNameENUS,ItemCode,OtherCost,"
                        + "ClassGUID,DishSize,Container,Cooking,Nutrition,Tips,Sort,Status,CreateTime,LastUpdate,IsDel,Image1,Image2,"
                        + "Image3) values('" + guid + "','FG',@buy,1,@name,@name,@name_EN,@code,@ocost,@classguid,@dishsize,@container,@cooking,"
                        + "@nutrition,@tips,@sort,@status,@date,@date,0,@img1,@img2,@img3);";

                    propSql = "insert into [dbo].[tblItemPropery](ItemGUID,DictCode,PropName,PropValue) "
                        + "values('" + guid + "','{0}','{1}','{2}');";
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(ckStr) && !ckStr.Equals(item.ItemGuid))
                        throw new Exception("Item code already exists");

                    sql = "update [dbo].[tblItem] set ItemCode=@code,ItemName=@name,ItemNameZHCN=@name,ItemNameENUS=@name_EN,ClassGUID=@classguid,DishSize=@dishsize,"
                       + "Container=@container,ToBuy=@buy,Tips=@tips,Nutrition=@nutrition,Sort=@sort,Status=@status,"
                       + "OtherCost=@ocost,Cooking=@cooking,LastUpdate=@date,IsDel=@del,Image1=@img1,Image2=@img2,Image3=@img3 "
                       + "where GUID='" + item.ItemGuid + "';";

                    propSql = "update [dbo].[tblItemPropery] set PropValue='{1}' where ItemGUID='" + item.ItemGuid
                        + "' and DictCode='{0}';";
                }
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append(sql);
                if (item.ItemProperies != null && item.ItemProperies.Count > 0)
                {
                    foreach (var prop in item.ItemProperies)
                    {
                        if (string.IsNullOrWhiteSpace(item.ItemGuid)) sqlBuilder.Append(string.Format(propSql, prop.DictCode, prop.DictName, prop.PropValue.Replace("--", "")));
                        else sqlBuilder.Append(string.Format(propSql, prop.DictCode, prop.PropValue.Replace("--", "")));
                    }
                }
                System.Data.SqlClient.SqlParameter date = new System.Data.SqlClient.SqlParameter("@date", SqlDbType.DateTime, 23);
                date.Value = DateTime.Now;
                System.Data.SqlClient.SqlParameter code = new System.Data.SqlClient.SqlParameter("@code", SqlDbType.VarChar, 32);
                code.Value = item.ItemCode.GetString();
                System.Data.SqlClient.SqlParameter name = new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 128);
                name.Value = item.ItemName.GetString();
                System.Data.SqlClient.SqlParameter name_EN = new System.Data.SqlClient.SqlParameter("@name_EN", SqlDbType.VarChar, 128);
                name_EN.Value = item.ItemName_EN.GetString();
                System.Data.SqlClient.SqlParameter cguid = new System.Data.SqlClient.SqlParameter("@classguid", SqlDbType.Char, 36);
                if (string.IsNullOrWhiteSpace(item.ItemClassGuid)) cguid.Value = DBNull.Value;
                else cguid.Value = item.ItemClassGuid.GetString();
                System.Data.SqlClient.SqlParameter buy = new System.Data.SqlClient.SqlParameter("@buy", SqlDbType.Bit, 1);
                buy.Value = item.ItemBuy;
                System.Data.SqlClient.SqlParameter status = new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                status.Value = item.ItemStatus;
                System.Data.SqlClient.SqlParameter ocost = new System.Data.SqlClient.SqlParameter("@ocost", SqlDbType.Decimal, 18);
                ocost.Value = item.OtherCost.ToDouble(2);
                System.Data.SqlClient.SqlParameter del = new System.Data.SqlClient.SqlParameter("@del", SqlDbType.Bit, 1);
                if (item.ItemStatus.Equals(0)) del.Value = 1;
                else del.Value = 0;
                System.Data.SqlClient.SqlParameter dishSize = new System.Data.SqlClient.SqlParameter("@dishsize", SqlDbType.VarChar, 16);
                dishSize.Value = item.ItemDishSize.GetString();
                System.Data.SqlClient.SqlParameter container = new System.Data.SqlClient.SqlParameter("@container", SqlDbType.VarChar, 16);
                container.Value = item.ItemContainer.GetString();
                System.Data.SqlClient.SqlParameter tips = new System.Data.SqlClient.SqlParameter("@tips", SqlDbType.VarChar, 1024);
                tips.Value = item.ItemTips.GetString();
                System.Data.SqlClient.SqlParameter nutrition = new System.Data.SqlClient.SqlParameter("@nutrition", SqlDbType.VarChar, 1024);
                nutrition.Value = item.ItemNutrition.GetString();
                System.Data.SqlClient.SqlParameter cooking = new System.Data.SqlClient.SqlParameter("@cooking", SqlDbType.VarChar, 1024);
                cooking.Value = item.ItemCooking.GetString();
                System.Data.SqlClient.SqlParameter sort = new System.Data.SqlClient.SqlParameter("@sort", SqlDbType.Int, 5);
                sort.Value = item.ItemSort.ToInt();
                System.Data.SqlClient.SqlParameter image1 = new System.Data.SqlClient.SqlParameter("@img1", SqlDbType.VarChar, 64);
                image1.Value = item.Image1.GetString();
                System.Data.SqlClient.SqlParameter image2 = new System.Data.SqlClient.SqlParameter("@img2", SqlDbType.VarChar, 64);
                image2.Value = item.Image2.GetString();
                System.Data.SqlClient.SqlParameter image3 = new System.Data.SqlClient.SqlParameter("@img3", SqlDbType.VarChar, 64);
                image3.Value = item.Image3.GetString();
                int count = helper.Execute(sqlBuilder.ToString(), CommandType.Text, new System.Data.SqlClient.SqlParameter[] { date, 
                    code, name, name_EN, cguid, buy, status, dishSize, container, tips, nutrition, cooking, ocost,
                    sort, del, image1, image2, image3 });
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = item.ItemCode;
                }
                else
                {
                    resp.Status = "error";
                    resp.Msg = "No record been modified.";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        public void ModifyFGPics(string picFile, string tag, string dataField, Model.Upload.UploadFileName uploadFileName)
        {
            if (string.IsNullOrWhiteSpace(dataField)) throw new Exception("Data filed not found.");
            string itemCode = System.IO.Path.GetFileNameWithoutExtension(uploadFileName.OrignFileName).GetString();
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            string itemGuid = helper.GetDataScalar("select GUID from [dbo].[tblItem] where ItemCode='" 
                + itemCode + "' and ToSell=1").GetString();
            if (string.IsNullOrWhiteSpace(itemGuid)) throw new Exception("Item not found.");
            string imgStr = picFile + "\\" + tag + "\\" + uploadFileName.NewFileName;
            string sql = "update [dbo].[tblItem] set " + dataField + "=@img where GUID='" + itemGuid + "'";
            System.Data.SqlClient.SqlParameter img = new System.Data.SqlClient.SqlParameter("@img", SqlDbType.VarChar, 64);
            img.Value = imgStr;
            int count = helper.Execute(sql, CommandType.Text, new System.Data.SqlClient.SqlParameter[] { img });
            if (count <= 0) throw new Exception("Data process failed");
        }

        #endregion

        #endregion

        #region 上传成品,原料数据

        /// <summary>
        /// 仅新增
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string UploadRMDatas(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) throw new Exception("上传文件未找到.文件名为空,请联系管理员");
            DataTable data = Utils.Excel.ExcelHelper.GetInstance().GetDataTable(file, "RM", 1, 0);
            if (data == null || data.Rows.Count == 0) throw new Exception("Excel转换失败或数据为空,检查Sheet名是否存在RM");
            if(!data.Columns.Contains("ItemCode") || !data.Columns.Contains("ItemName") || !data.Columns.Contains("Unit") 
                || !data.Columns.Contains("ItemSpec") || !data.Columns.Contains("PurchasePolicy")) 
                throw new Exception("标题错误,标题必须包含(ItemCode,ItemName,Unit,ItemSpec,PurchasePolicy)");
            string processSql = "declare @guid char(36);declare @uom char(36);select @guid=GUID from [dbo].[tblItem] "
                + "where ItemCode='{0}' and ToBuy=1;select @uom=GUID from [dbo].[tblUOM] where NameCn='{2}' or NameEn='{2}';"
                + "if(isnull(@uom,'')<>'') begin if(isnull(@guid,'')='') insert into [dbo].[tblItem](GUID,ItemType,ToBuy,ItemCode,"
                + "ItemName,PurUOMGUID,ItemSpec,Status,CreateTime,Isdel,PurchasePolicy) "
                + "values(newid(),'RM',1,'{0}','{1}',@uom,'{3}',1,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "',0,'{4}');"
                + "else update [dbo].[tblItem] set ItemName='{1}',PurUOMGUID=@uom,ItemSpec='{3}',LastUpdate='" 
                + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "',PurchasePolicy='{4}' where GUID=@guid;end";
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable retData = Utils.Common.Functions.CreateTableStructAsString(new List<string>() { "ItemCode", "Reason" }, "RM");
            string itemCode = string.Empty, itemName = string.Empty, unit = string.Empty, itemSpec = string.Empty, policy = string.Empty;
            int count = 0;
            foreach (DataRow dr in data.Rows)
            {
                itemCode = dr.Field<string>("ItemCode").GetString();
                unit = dr.Field<string>("Unit").GetString();
                itemName = dr.Field<string>("ItemName").GetString();
                policy = dr.Field<string>("PurchasePolicy").GetString();
                if (!policy.Equals("OnDemand") && !policy.Equals("NoPurchase")) policy = string.Empty;
                if (string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(unit) || string.IsNullOrWhiteSpace(itemName)
                    || string.IsNullOrWhiteSpace(policy))
                {
                    retData.Rows.Add(itemCode, "红色标题列存在空值,或采购策略错误");
                    continue;
                }
                itemSpec = dr.Field<string>("ItemSpec").GetString();
                count = helper.Execute(string.Format(processSql, itemCode.SqlEscapeString(), itemName.SqlEscapeString(),
                    unit.SqlEscapeString(), itemSpec.SqlEscapeString(), policy));
                if (count <= 0) retData.Rows.Add(itemCode, "数据执行失败,检查采购单位");
            }
            if (retData.Rows.Count > 0)
                return Utils.Excel.ExcelHelper.GetInstance().SaveDataTable(retData, System.IO.Path.GetDirectoryName(file),
                    System.IO.Path.GetExtension(file));
            return string.Empty;
        }

        public string UploadFGDatas(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) throw new Exception("上传文件未找到.文件名为空,请联系管理员");
            DataSet ds = Utils.Excel.ExcelHelper.GetInstance().GetDataSet(file, new List<string>() { "FG", "FGProperties" }, 1, 1, 0);
            if(ds == null || ds.Tables.Count == 0) throw new Exception("Excel转换失败或数据为空,检查Sheet名是否存在FG或FGProperties");
            DataTable fgData = ds.Tables["FG"];
            DataTable propertiesData = ds.Tables["FGProperties"];
            DataTable propData = null;
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            if(fgData != null && fgData.Rows.Count > 0)
            {
                 if(!fgData.Columns.Contains("ItemCode") || !fgData.Columns.Contains("ItemName") 
                    || !fgData.Columns.Contains("ItemClass") || !fgData.Columns.Contains("DishSize") 
                    || !fgData.Columns.Contains("Container") || !fgData.Columns.Contains("Cooking") 
                    ||!fgData.Columns.Contains("Nutrition") || !fgData.Columns.Contains("Tips") 
                    || !fgData.Columns.Contains("Sort") || !fgData.Columns.Contains("OtherCost"))
                    throw new Exception("FG标题错误,标题必须包含(ItemCode,ItemName,ItemClass,DishSize,Container,"
                        + "Cooking,Nutrition,Tips,Sort,OtherCost)");
            }
            if(propertiesData != null && propertiesData.Rows.Count > 0)
            {
                if(!propertiesData.Columns.Contains("ItemCode")) throw new Exception("FGProperties标题错误,标题必须包含(ItemCode)");
                string dictSql = "select Code,Name from [dbo].[tblDict]";
                propData = helper.GetDataTable(dictSql);
                if (propData == null || propData.Rows.Count == 0) throw new Exception("tblDict表数据不存在");
                List<string> propList = propData.AsEnumerable().Select(dr => dr.Field<string>("Code").GetString()).ToList();
                foreach(DataColumn dc in propertiesData.Columns)
                {
                    if (!dc.ColumnName.Equals("ItemCode") && !propList.Contains(dc.ColumnName)) 
                        throw new Exception("FGProperties标题错误,标题:" + dc.ColumnName + ",不存在");
                }
            }

            string itemCode = string.Empty;
            DataSet retDs = null;
            int count = 0;

            #region 插入或更新成品
            if (fgData != null && fgData.Rows.Count > 0)
            {
                #region 插入更新成品Sql
                string fgSql = "declare @guid char(36);declare @cguid char(36);select @guid=GUID from [dbo].[tblItem] "
                    + "where ItemCode='{0}' and ToSell=1;select @cguid=GUID from tblItemClass where ClassName='{1}';"
                    + "if(isnull(@cguid,'')<>'') begin if(isnull(@guid,'')='') insert into [dbo].[tblItem](GUID,ItemType,"
                    + "ToSell,ItemName,ItemCode,ClassGUID,DishSize,Container,Cooking,Nutrition,Tips,Sort,Status,IsDel,CreateTime,"
                    + "LastUpdate,OtherCost) values(newid(),'FG',1,'{2}','{0}',@cguid,'{3}','{4}','{5}','{6}','{7}',{8},1,0,'"
                    + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") 
                    + "',{9});else update [dbo].[tblItem] set ItemName='{2}',ClassGUID=@cguid,DishSize='{3}',Container='{4}',"
                    + "Cooking='{5}',Nutrition='{6}',Tips='{7}',Sort={8},OtherCost={9},LastUpdate='" 
                    + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "' where GUID=@guid;end";
                #endregion

                string itemName = string.Empty, itemClass = string.Empty, dishSize = string.Empty, container = string.Empty,
                    cooking = string.Empty, nutrition = string.Empty, tips = string.Empty, otherCost = string.Empty;
                int sort = 0;

                DataTable retData1 = Utils.Common.Functions.CreateTableStructAsString(new List<string>() { "ItemCode", "Reason" }, "FG");

                foreach (DataRow dr in fgData.Rows)
                {
                    itemCode = dr.Field<string>("ItemCode").GetString();
                    itemName = dr.Field<string>("ItemName").GetString();
                    itemClass = dr.Field<string>("ItemClass").GetString();
                    dishSize = dr.Field<string>("DishSize").GetString();
                    container = dr.Field<string>("Container").GetString();
                    cooking = dr.Field<string>("Cooking").GetString();
                    nutrition = dr.Field<string>("Nutrition").GetString();
                    tips = dr.Field<string>("Tips").GetString();
                    sort = dr.Field<string>("Sort").ToInt();
                    otherCost = dr.Field<string>("OtherCost").GetString();
                    if(string.IsNullOrWhiteSpace(itemCode) || string.IsNullOrWhiteSpace(itemName) 
                        || string.IsNullOrWhiteSpace(itemClass) || string.IsNullOrWhiteSpace(dishSize)
                        || string.IsNullOrWhiteSpace(container) || string.IsNullOrWhiteSpace(nutrition) 
                        || string.IsNullOrWhiteSpace(tips))
                    {
                        retData1.Rows.Add(itemCode, "红色标题列存在空值");
                        continue;
                    }
                    count = helper.Execute(string.Format(fgSql, itemCode.SqlEscapeString(), itemClass.SqlEscapeString(),
                        itemName.SqlEscapeString(), dishSize.SqlEscapeString(), container.SqlEscapeString(),
                        cooking.SqlEscapeString(), nutrition.SqlEscapeString(), tips.SqlEscapeString(), sort,
                        otherCost.ToDouble(2)));
                    if (count <= 0) retData1.Rows.Add(itemCode, "执行失败,检查产品大类是否存在");
                }
                if(retData1.Rows.Count > 0)
                {
                    retDs = new DataSet();
                    retDs.Tables.Add(retData1);
                }
            }
            #endregion

            #region 插入或更新成品属性
            if (propertiesData != null && propertiesData.Rows.Count > 0)
            {
                DataTable retData2 = Utils.Common.Functions.CreateTableStructAsString(new List<string>() { "ItemCode", "Reason" }, 
                    "FGProperties");

                string propSql = "declare @guid char(36);select @guid=GUID from [dbo].[tblItem] where ItemCode='{0}' and ToSell=1;"
                          + "if(isnull(@guid,'')<>'') begin declare @id int;select @id=ID from [dbo].[tblItemPropery] "
                          + "where ItemGUID=@guid and DictCode='{1}';if(isnull(@id,0)=0) insert into [dbo].[tblItemPropery]"
                          + "(ItemGUID,DictCode,PropName,PropValue) values(@guid,'{1}','{3}','{2}');else update [dbo].[tblItemPropery] "
                          + "set PropValue='{2}' where ID=@id;end";

                foreach (DataRow dr in propertiesData.Rows)
                {
                    itemCode = dr.Field<string>("ItemCode").GetString();
                    if (string.IsNullOrWhiteSpace(itemCode))
                    {
                        retData2.Rows.Add(itemCode, "成品代码为空");
                        continue;
                    }
                    foreach(DataRow drp in propData.Rows)
                    {
                        count = helper.Execute(string.Format(propSql, itemCode.SqlEscapeString(),
                            drp.Field<string>("Code").GetString().SqlEscapeString(),
                            dr.Field<string>(drp.Field<string>("Code").GetString()).GetString().SqlEscapeString(),
                            drp.Field<string>("Name").GetString().SqlEscapeString()));
                        if (count <= 0)
                            retData2.Rows.Add(itemCode, "数据执行失败,列名:" + drp.Field<string>("Code").GetString());
                    }
                }
                if(retData2.Rows.Count > 0)
                {
                    if (retDs == null) retDs = new DataSet();
                    retDs.Tables.Add(retData2);
                }
            }
            #endregion

            if (retDs != null)
                return Utils.Excel.ExcelHelper.GetInstance().SaveDataSet(retDs, System.IO.Path.GetDirectoryName(file),
                    System.IO.Path.GetExtension(file));
            return string.Empty;
        }

        #endregion

        #region Item数据字典字段

        /// <summary>
        /// 根据字典类型,获取字典中的内容
        /// </summary>
        /// <param name="dictType">类型</param>
        /// <returns></returns>
        public List<Model.Dict.DictGroup> GetItemDict(List<Model.Item.ItemPropery> itemProperies)
        {
            try
            {
                string sql = "select DictType,Code,Name from [dbo].[tblDict]";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return null;
                if (itemProperies == null)
                {
                    return data.AsEnumerable().GroupBy(dr => dr.Field<string>("DictType").GetString()).Select(dg => new Model.Dict.DictGroup()
                    {
                        DictType = dg.Key,
                        Details = dg.Select(r => new Model.Dict.DictDetail()
                        {
                            Code = r.Field<string>("Code").GetString(),
                            Name = r.Field<string>("Name").GetString()
                        }).ToList()
                    }).ToList();
                }
                else
                {
                    var query = (from dr in data.AsEnumerable()
                                 join i in itemProperies
                                 on dr.Field<string>("Code").GetString() equals i.DictCode into ldata
                                 from ldr in ldata.DefaultIfEmpty()
                                 select new
                                 {
                                     DictType = dr.Field<string>("DictType").GetString(),
                                     Code = dr.Field<string>("Code").GetString(),
                                     Name = dr.Field<string>("Name").GetString(),
                                     Value = ldr == null ? string.Empty : ldr.PropValue
                                 }).ToList();
                    return query.GroupBy(q => q.DictType).Select(qg => new Model.Dict.DictGroup()
                    {
                        DictType = qg.Key,
                        Details = qg.Select(r => new Model.Dict.DictDetail()
                        {
                            Code = r.Code,
                            Name = r.Name,
                            Value = r.Value
                        }).ToList()
                    }).ToList();
                }
            }
            catch { return null; }
        }

        #endregion

        #region 成品大类

        public List<Model.Item.ItemClass> GetItemClass(string menuClass)
        {
            try
            {
                string sql = "select GUID,ClassName,Sort from [dbo].[tblItemClass] order by Sort";
                if (!string.IsNullOrWhiteSpace(menuClass))
                    sql = "select distinct 0 as Sort,DataType as GUID,DataType as ClassName from tblDatas ";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Item.ItemClass()
                {
                    CLassGUID = dr.Field<string>("GUID").GetString(),
                    ClassName = dr.Field<string>("ClassName") .GetString(),
                    Sort = dr.Field<int?>("Sort").ToInt()
                }).ToList();
            }
            catch { return null; }

        }

        public Model.Common.BaseResponse ModifyItemClass(IList<Model.Item.ItemClass> classList)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if(classList == null || classList.Count == 0) throw new Exception("Nothing to process.");
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (Model.Item.ItemClass iclass in classList)
                {
                    if (string.IsNullOrWhiteSpace(iclass.CLassGUID))
                    {
                        if (!string.IsNullOrWhiteSpace(iclass.ClassName) && iclass.Sort.ToInt() > 0)
                            sqlBuilder.Append("insert into [dbo].[tblItemClass](GUID,ClassName,Sort) values(newid(),'"
                                + iclass.ClassName.GetString() + "'," + iclass.Sort + ");");
                    }
                    else
                    {
                        if (iclass.Sort.ToInt() > 0)
                            sqlBuilder.Append("update [dbo].[tblItemClass] set Sort=" + iclass.Sort + " where GUID='"
                                + iclass.CLassGUID + "';");
                    }
                }        
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sqlBuilder.ToString());
                if (count > 0) resp.Status = "ok";
                else
                {
                    resp.Status = "error";
                    resp.Msg = "No record been modified.";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        #endregion

        public List<Model.Item.FGMast> GetTimeList(string language)
        {
            try
            {
                string sql = "select distinct StartDate from tblDatas";
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "TimeList");
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Item.FGMast()
                {
                    startDate = dr.Field<DateTime?>("StartDate").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy", "Min")
                }).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Model.Table.TableMast<Model.Item.FGMast> GetTableMenuList(string itemClass,string startDate, string weekDay, string KeyWords, string siteGuid,string language)
        {
            Model.Table.TableMast<Model.Item.FGMast> tableMast = new Model.Table.TableMast<Model.Item.FGMast>();
            tableMast.draw = 1;
            tableMast.data = GetMenuList(itemClass, startDate, weekDay, KeyWords,siteGuid, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Item.FGMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        

        //菜单数据界面（包括自助餐)
        public List<Model.Item.FGMast> GetMenuList(string itemClass,string startDate, string weekDay, string KeyWords, string siteGuid,string language)
        {
            try
            {
                string filter = "";string filter2 = "";
                if (!string.IsNullOrWhiteSpace(KeyWords))
                {
                    filter = string.Format(" and (a1.Val2 like '%{0}%' or a2.Val2 like '%{0}%' or a1.BusinessType like '%{0}%') ", KeyWords);
                    filter2= string.Format(" and (a1.Val2 like '%{0}%' or a1.BusinessType like '%{0}%') ", KeyWords);
                }
                    
                string sql = "select a.* from (select a1.id,Case a1.Val1 when 'img' then a1.Val1 else '' end ImageName1,isnull(a1.SortName,'0') as Sort,a1.BusinessType,a1.GUID,(Case when a1.Val1 ='AM' or a1.Val1='PM' then a1.Val1 +': ' +a1.Val2 else a1.Val2 End) as ItemName_EN,(Case when a1.Val1 ='AM' or a1.Val1='PM' then a1.Val1 +': ' +a2.Val2 else a2.Val2 End) as ItemName_CN,a1.DataType as ItemClass,a1.StartDate,a2.EndDate, "
                    + "a1.Val1,a1.Val3,isnull(a1.ItemStatus,0) ItemStatus,a1.langCode from tblDatas a1 join tblDatas a2 on a1.BusinessType = a2.BusinessType and a1.DataType = a2.DataType and a1.StartDate = a2.StartDate and a1.SortName = a2.SortName "
                    + "and a1.Val1 = a2.Val1 and a1.Val3 = a2.Val3 and a1.guid=a2.Guid "
                    + "where a1.langCode = 'ENUS' and a2.LangCode = 'ZHCN' "
                    + "and a1.Val1 in ('Name', 'ShortName', 'AM', 'PM') and a2.Val1 in ('Name', 'ShortName', 'AM', 'PM') "
                    + "and a1.DataType = '" + itemClass + "' and a1.BusinessType like '%"+siteGuid+"%' and convert(varchar(10),a1.StartDate,23) = '"+ startDate + "' and a1.val3 = '"+ weekDay + "' "
                    + filter+ ") a union select b.* from "
                    + " (select a1.id,Case a1.Val1 when 'img' then a1.Val1 else '' end ImageName1,isnull(a1.SortName,'100') as Sort,a1.BusinessType,a1.GUID, "
                    + " (Case when a1.langCode ='ZHCN' then 'Picture of Chinese' else 'Picture of English' End) as ItemName_EN, "
                    + " (Case when a1.langCode ='ZHCN' then '图片（中文）' else '图片（英文）' End) as ItemName_CN, "
                    + " a1.DataType as ItemClass,a1.StartDate,a1.EndDate,a1.Val1,a1.Val3,isnull(a1.ItemStatus,0) ItemStatus,a1.langCode from tblDatas a1 "
                    + " where a1.Val1='img' and a1.DataType='" + itemClass + "' and a1.BusinessType like '%" + siteGuid + "%' and convert(varchar(10),a1.StartDate,23)='" + startDate + "'" + filter2 + ") b";
                
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "ItemMast");
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Item.FGMast()
                {
                    ItemType = dr.Field<string>("BusinessType").GetString(),
                    ItemClassName = dr.Field<string>("ItemClass").GetString(),
                    ItemGuid = dr.Field<string>("GUID").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemSort = dr.Field<string>("Sort").ToInt(),
                    startDate = dr.Field<DateTime?>("StartDate").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy", "Min"),
                    endDate = dr.Field<DateTime?>("EndDate").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy", "Min"),
                    comments = language.Equals("zh") ? (dr.Field<int>("ItemStatus").Equals(1) ? "有效" : "无效") : (dr.Field<int>("ItemStatus").Equals(1) ? "Valid" : "Blocked"),
                    ImageName1 = dr.Field<string>("ImageName1").GetString(),
                    langCode = dr.Field<string>("langCode")
                }).ToList();
            }
            catch {
                return null;
            }
        }

        //菜单(包括自助餐)明细
        public Model.Item.FGMast GetMenuDetail(string language, string guid,string ItemName_EN,string langCode)
        {
            try
            {
                List<Utils.Database.SqlServer.DBQueryDic> sqlDics = new List<Utils.Database.SqlServer.DBQueryDic>();
                string sql = "";
                if (ItemName_EN!= "Picture of Chinese" && ItemName_EN != "Picture of English")
                {
                    sql = string.Format("select a1.id,convert(int,isnull(a1.SortName,'0')) as Sort,a1.BusinessType,a1.GUID,a1.Val2 as ItemName_EN,a2.Val2 as ItemName_CN,a1.DataType as ItemClass, "
                                + "a1.StartDate, a2.EndDate,a1.Val1,isnull(a1.ItemStatus,0) Status,a1.Val3 as weekday,'' as Image1,a1.langCode from tblDatas a1 join tblDatas a2 on a1.BusinessType = a2.BusinessType and a1.DataType = a2.DataType "
                                + "and a1.StartDate = a2.StartDate and a1.SortName = a2.SortName and a1.Val1 = a2.Val1 and a1.Val3 = a2.Val3 and a1.guid=a2.Guid "
                                + "where a1.langCode = 'ENUS' and a2.LangCode = 'ZHCN' "
                                + "and a1.Guid = '{0}' order by a1.SortName  ", guid);
                }
                else
                {
                    sql = string.Format("select id,convert(int,isnull(SortName,'0')) as Sort,BusinessType,GUID, "
                    + " (Case when langCode ='ZHCN' then 'Picture of Chinese' else 'Picture of English' End) as ItemName_EN, "
                    + " (Case when langCode ='ZHCN' then '图片（中文）' else '图片（英文）' End) as ItemName_CN, "
                    + " DataType as ItemClass,StartDate,EndDate,Val1,isnull(ItemStatus,0) Status,Val3 as weekday,Val2 as Image1,langCode from tblDatas "
                    + " where guid='{0}' and LangCode like '%{1}%'", guid,langCode);
                }

                sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
                {
                    Sql = sql,
                    TableName = "ItemMast"
                });
             
                DataSet ds = new Utils.Database.SqlServer.DBHelper(_conn).GetDataSet(sqlDics);
                if (ds == null) return null;
                DataTable data = ds.Tables["ItemMast"];
           

                var dataOfnoneIngredients = ds.Tables["ItemMast"].AsEnumerable().Where(dr => dr.Field<string>("Val1") != "Ingredients" && dr.Field<string>("Val1") != "Qty");
                var dataOfIngredients = ds.Tables["ItemMast"].AsEnumerable().Where(dr => dr.Field<string>("Val1") == "Ingredients" || dr.Field<string>("Val1") == "Qty" ||dr.Field<string>("Val1")==string.Empty);


                if (data == null || data.Rows.Count == 0) return null;
                return new Model.Item.FGMast()
                {
                    ingredientTips = dataOfIngredients.ToList().Count==0?"": dataOfIngredients.FirstOrDefault().Field<string>("Val1"),
                    ItemTips = dataOfnoneIngredients.ToList().Count==0?"":dataOfnoneIngredients.FirstOrDefault().Field<string>("Val1").GetString(),
                    ItemClassGuid = data.Rows[0].Field<string>("ItemClass")==null?"":data.Rows[0].Field<string>("ItemClass").GetString(),
                    ItemGuid = guid,
                    ItemStatus = data.Rows[0].Field<int?>("Status")==null? 0 :data.Rows[0].Field<int?>("Status").ToInt(),
                    ItemName_CN = dataOfnoneIngredients.ToList().Count==0?"": dataOfnoneIngredients.FirstOrDefault().Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dataOfnoneIngredients.ToList().Count==0?"": dataOfnoneIngredients.FirstOrDefault().Field<string>("ItemName_EN").GetString(),
                    ingredientTips_CN = dataOfIngredients.ToList().Count==0?"": dataOfIngredients.FirstOrDefault().Field<string>("ItemName_CN").GetString(),
                    ingredientTips_EN = dataOfIngredients.ToList().Count==0?"": dataOfIngredients.FirstOrDefault().Field<string>("ItemName_EN").GetString(),
                    ItemSort = data.Rows[0].Field<int?>("Sort")==null? 0: data.Rows[0].Field<int?>("Sort").ToInt(),
                    startDate = data.Rows[0].Field<DateTime?>("StartDate")==null? "" : data.Rows[0].Field<DateTime?>("StartDate").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy", "Min"),
                    endDate =  data.Rows[0].Field<DateTime?>("EndDate")==null? "" : data.Rows[0].Field<DateTime?>("EndDate").ToFormatDate(language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy", "Min"),
                    ItemType = data.Rows[0].Field<string>("BusinessType")==null?"" : data.Rows[0].Field<string>("BusinessType").GetString(),
                    weekday = data.Rows[0].Field<string>("weekday")==null?"1":data.Rows[0].Field<string>("weekday").GetString(),
                    Image1=data.Rows[0].Field<string>("Image1").GetString(),
                    ImageName1 = string.IsNullOrWhiteSpace(data.Rows[0].Field<string>("Image1")) ?"":System.IO.Path.GetFileName(data.Rows[0].Field<string>("Image1").GetString()),
                    langCode=data.Rows[0].Field<string>("langCode")==null?"": data.Rows[0].Field<string>("langCode").GetString()
                };
            }
            catch {
                return null;
            }
        }
        //修改保存自助餐明细
        public Model.Common.BaseResponse ModifyMenuItem(Model.Item.FGMast item, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (item == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                string propSql = string.Empty;
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);

                if (string.IsNullOrWhiteSpace(item.ItemGuid))
                {
                    string guid = Guid.NewGuid().ToString().ToUpper();
                    if (!item.ItemTips.Equals("img"))
                    {
                        sql = string.Format("insert into tblDatas(BusinessType,DataType,LangCode,Val1,Val2,Val3,StartDate,EndDate,SortName,"
                        + "Guid,ItemStatus) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                        item.ItemType, item.ItemClassGuid, "ENUS", item.ItemTips, item.ItemName_EN, item.ItemStatus, item.startDate, item.endDate, item.ItemSort,
                        guid, item.ItemStatus);

                        sql += string.Format("insert into tblDatas(BusinessType,DataType,LangCode,Val1,Val2,Val3,StartDate,EndDate,SortName,"
                            + "Guid,ItemStatus) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                            item.ItemType, item.ItemClassGuid, "ZHCN", item.ItemTips, item.ItemName_CN, item.ItemStatus, item.startDate, item.endDate, item.ItemSort,
                            guid, item.ItemStatus);

                       
                        sql += string.Format("insert into tblDatas(BusinessType,DataType,LangCode,Val1,Val2,Val3,StartDate,EndDate,SortName,"
                            + "Guid,ItemStatus) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                            item.ItemType, item.ItemClassGuid, "ENUS", item.ingredientTips, item.ingredientTips_EN, item.ItemStatus, item.startDate, item.endDate, item.ItemSort,
                            guid, item.ItemStatus);


                        sql += string.Format("insert into tblDatas(BusinessType,DataType,LangCode,Val1,Val2,Val3,StartDate,EndDate,SortName,"
                                + "Guid,ItemStatus) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}');",
                                item.ItemType, item.ItemClassGuid, "ZHCN", item.ingredientTips, item.ingredientTips_CN, item.ItemStatus, item.startDate, item.endDate, item.ItemSort,
                                guid, item.ItemStatus);
                        
                    }
                    else if (item.ItemTips.Equals("img"))
                    {
                        string ckStr = "select top 1 Guid from tblDatas where BusinessType='" + item.ItemType.GetString() + "' and Val1='"+item.ItemTips+"' "
                            +" and DataType='"+item.ItemClassGuid+"' and langCode='"+item.langCode+"' and StartDate='"+item.startDate+"'";
                        ckStr = helper.GetDataScalar(ckStr).GetString();


                        if (!string.IsNullOrWhiteSpace(item.Image1))
                        {
                            if (!string.IsNullOrWhiteSpace(ckStr)) throw new Exception("picture already exists");

                            sql += string.Format("insert into tblDatas(BusinessType,DataType,LangCode,Val1,Val2,StartDate,EndDate,SortName, " 
                                + "Guid,ItemStatus) values('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}');",
                               item.ItemType, item.ItemClassGuid,item.langCode,item.ItemTips, item.Image1.Contains("\\") ? item.Image1.Substring(item.Image1.LastIndexOf("\\") + 1) : item.Image1, item.startDate,item.endDate,item.ItemSort,
                               guid,item.ItemStatus);
                        }
                    }

                   
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(item.Image1))
                    {
                        //更新中英文图片
                        sql = string.Format("update tblDatas set BusinessType='{0}',DataType='{1}',Val2='{2}', "
                           + " SortName='{3}',ItemStatus={4}, StartDate='{5}',EndDate='{6}' "
                           + "where GUID='{7}' and LangCode = '{8}' and Val1='img';"
                           , item.ItemType, item.ItemClassGuid, item.Image1.Contains("\\") ? item.Image1.Substring(item.Image1.LastIndexOf("\\") + 1) : item.Image1, item.ItemSort.ToString(), item.ItemStatus, item.startDate, item.endDate, item.ItemGuid, item.langCode);
                    }
                    //跟新中文菜谱
                    sql += string.Format("update tblDatas set BusinessType='{0}',DataType='{1}',Val2='{2}',Val3='{3}', "
                       + " SortName='{4}',ItemStatus={5}, StartDate='{6}',EndDate='{7}',Val1='{8}' "
                       + "where GUID='{9}' and LangCode = 'ZHCN' and Val1 not in ('Qty','Ingredients','img','');"
                       , item.ItemType, item.ItemClassGuid, item.ItemName_CN, item.weekday, item.ItemSort.ToString(), item.ItemStatus, item.startDate, item.endDate, item.ItemTips,item.ItemGuid);
                    //跟新英文菜谱
                    sql += string.Format("update tblDatas set BusinessType='{0}',DataType='{1}',Val2='{2}',Val3='{3}', "
                       + " SortName='{4}',ItemStatus={5}, StartDate='{6}',EndDate='{7}',Val1='{8}' "
                       + "where GUID='{9}' and LangCode = 'ENUS' and Val1 not in ('Qty','Ingredients','img','');", 
                       item.ItemType,item.ItemClassGuid, item.ItemName_EN, item.weekday, item.ItemSort.ToString(),item.ItemStatus, item.startDate, item.endDate, item.ItemTips,item.ItemGuid);
                    //跟新中文原材料
                    sql += string.Format("update tblDatas set BusinessType='{0}',DataType='{1}',Val2='{2}',Val3='{3}', "
                       + " SortName='{4}',ItemStatus={5}, StartDate='{6}',EndDate='{7}',Val1='{8}' "
                       + "where GUID='{9}' and LangCode = 'ZHCN' and Val1 in ('Qty','Ingredients','');", 
                       item.ItemType, item.ItemClassGuid, item.ingredientTips_CN, item.weekday, item.ItemSort.ToString(),item.ItemStatus, item.startDate,
                       item.endDate, item.ingredientTips,item.ItemGuid);
                    //跟新英文原材料
                    sql += string.Format("update tblDatas set BusinessType='{0}',DataType='{1}',Val2='{2}',Val3='{3}', "
                       + " SortName='{4}',ItemStatus={5}, StartDate='{6}',EndDate='{7}',Val1='{8}' "
                       + "where GUID='{9}' and LangCode = 'ENUS' and Val1 in ('Qty','Ingredients','');", 
                       item.ItemType, item.ItemClassGuid, item.ingredientTips_EN,item.weekday,item.ItemSort.ToString(),item.ItemStatus,
                       item.startDate, item.endDate, item.ingredientTips,item.ItemGuid);
                }
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append(sql);
                
                int count = helper.Execute(sqlBuilder.ToString());
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = item.ItemName_CN;
                }
                else
                {
                    resp.Status = "error";
                    resp.Msg = "No record been modified.";
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }


    }
}
