using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.BOM
{
    public class BOMHelper : Common.BaseDataHelper
    {
        private static BOMHelper instance = new BOMHelper();
        private BOMHelper() { }
        public static BOMHelper GetInstance() { return instance; }

        public Model.Common.BaseResponse EditBOM(Model.BOM.BOMMast bomMast, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (bomMast == null || bomMast.Details == null || bomMast.Details.Count == 0) throw new Exception("BOM not found.");
                string productGuid = bomMast.ProductGuid;
                if (string.IsNullOrWhiteSpace(productGuid)) throw new Exception("Product Guid not found.");
                StringBuilder sqlBuilder = new StringBuilder();
                foreach (Model.BOM.BOMDetail detail in bomMast.Details)
                {
                    if (detail.BOMID.Equals(0)) //新增
                    {
                        if (string.IsNullOrWhiteSpace(detail.ItemGuid) || string.IsNullOrWhiteSpace(detail.UOMGuid)) continue;
                        sqlBuilder.Append("insert into [dbo].[tblItemBOM](ProductGUID,ItemGUID,StdQty,ActQty,UOMGUID) "
                            + "values('" + productGuid + "','" + detail.ItemGuid + "'," + detail.StdQty.ToDouble(3)
                            + "," + detail.ActualQty + ",'" + detail.UOMGuid + "');");
                    }
                    else
                    {
                        sqlBuilder.Append("update [dbo].[tblItemBOM] set StdQty=" + detail.StdQty.ToDouble(3)
                            + ",ActQty=" + detail.ActualQty.ToDouble(3) + " where ID=" + detail.BOMID + ";");
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

        public Model.BOM.BOMMast GetFGBOM(string productGuid, string language)
        {
            try
            {
                string bomSql = "select a1.ID,a1.ItemGUID,a2.ItemCode,a2.ItemNameZHCN as ItemName_CN,a2.ItemNameENUS as ItemName_EN,a2.ItemType,a2.PurUOMGUID,a1.StdQty,a1.ActQty,a1.UOMGUID,"
                    + "case when 'zh'='" + language + "' then NameCn else NameEn end UOMName,a2.ParentGUID,a4.PurUOMGUID as PPurUOMGUID "
                    + "from [dbo].[tblItemBOM] a1 join [dbo].[tblItem] a2 on a1.ItemGUID=a2.GUID and (a2.ToBuy=1 or a2.ToSell=1) and a2.status=1 "
                    + "join [dbo].[tblUOM] a3 on a1.UOMGUID=a3.GUID left join [dbo].[tblItem] a4 "
                    + "on a2.ParentGUID=a4.GUID and a4.ToBuy=1 and a4.status=1 where a1.ProductGUID='" + productGuid + "' order by a1.ID";
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(bomSql);
                if (data == null || data.Rows.Count == 0) return null;
                Model.BOM.BOMMast bom = new Model.BOM.BOMMast();
                int sort = 1;
                bom.Details = data.AsEnumerable().Select(dr => new Model.BOM.BOMDetail()
                {
                    BOMID = dr.Field<int>("ID"),
                    ItemCode = dr.Field<string>("ItemCode").GetString(),
                    ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                    ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                    ItemType = dr.Field<string>("ItemType").GetString(),
                    Sort = sort++,
                    StdQty = dr.Field<decimal?>("StdQty").ToDouble(3),
                    ActualQty = dr.Field<decimal?>("ActQty").ToDouble(3),
                    UOMGuid = dr.Field<string>("UOMGUID").GetString(),
                    UOMName = dr.Field<string>("UOMName").GetString()
                }).ToList();
                return bom;
            }
            catch { return null; }
        }

        /// <summary>
        /// 根据原料代码,返回所有BOM中引用到此物料的成品
        /// </summary>
        /// <param name="itemGuid"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public List<Model.BOM.BOMDetail> GetBOMProducts(string itemGuid, string language)
        {
            string sql = "select a2.ItemCode,a2.ItemName,a2.ItemNameZHCN as ItemName_CN,a2.ItemNameENUS as ItemName_EN,a1.StdQty,a1.ActQty,case when 'zh'='" + language
                + "' then a3.NameCn else a3.NameEn end UnitName from [dbo].[tblItemBOM] a1 join [dbo].[tblItem] a2 "
                + "on a1.ProductGUID=a2.GUID and a2.ToSell=1 and a2.Status=1 join [dbo].[tblUOM] a3 on a1.UOMGUID=a3.GUID "
                + "and a3.Active=1 where a1.ItemGUID= '" + itemGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.BOM.BOMDetail()
            {
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName_CN = dr.Field<string>("ItemName_CN").GetString(),
                ItemName_EN = dr.Field<string>("ItemName_EN").GetString(),
                StdQty = dr.Field<decimal?>("StdQty").ToDouble(3),
                ActualQty = dr.Field<decimal?>("ActQty").ToDouble(3),
                UOMName = dr.Field<string>("UnitName").GetString()
            }).ToList();
        }

        #region 上传BOM数据
        /// <summary>
        /// 仅新增
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string UploadBOMs(string file)
        {
            if (string.IsNullOrWhiteSpace(file)) throw new Exception("上传文件未找到.文件名为空,请联系管理员");
            DataTable data = Utils.Excel.ExcelHelper.GetInstance().GetDataTable(file, "BOM", 1, 0);
            if (data == null || data.Rows.Count == 0) throw new Exception("Excel转换失败或数据为空,Sheet名BOM是否存在");
            if (!data.Columns.Contains("ProductCode") || !data.Columns.Contains("ItemCode") || !data.Columns.Contains("ActQty") 
                || !data.Columns.Contains("StdQty") || !data.Columns.Contains("Unit")) 
                throw new Exception("标题错误,标题必须包含(ProductCode,ItemCode,StdQty,ActQty,Unit)");

            #region 基本检查
            var ck1 = data.AsEnumerable().Where(dr => dr.Field<string>("ProductCode").GetString().Equals(string.Empty));
            if (ck1.Any()) throw new Exception("成品代码存在空值");
            var ck2 = data.AsEnumerable().Where(dr => dr.Field<string>("ItemCode").GetString().Equals(string.Empty));
            if (ck2.Any()) throw new Exception("原料代码存在空值");
            var ck3 = data.AsEnumerable().Where(dr => dr.Field<string>("ActQty").ToDouble(3) <= 0);
            if (ck3.Any()) throw new Exception("实际数量小于等于零或非数值");
            var ck4 = data.AsEnumerable().Where(dr => dr.Field<string>("Unit").GetString().Equals(string.Empty));
            if (ck4.Any()) throw new Exception("单位存在空值");
            #endregion

            #region 检查成品是否存在,并不存在BOM数据

            StringBuilder fgSql = new StringBuilder("select ItemCode,GUID from [dbo].[tblItem] where ToSell=1 "
                + "and Status=1 and GUID not in (select distinct ProductGUID from [dbo].[tblItemBOM]) and ItemCode in (");
            fgSql.Append(string.Join(",", data.AsEnumerable().Select(dr => "'" + dr.Field<string>("ProductCode").GetString() + "'")
                .Distinct().ToArray())).Append(")");
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable fgData = helper.GetDataTable(fgSql.ToString());
            if (fgData == null || fgData.Rows.Count == 0) throw new Exception("所有成品代码不存在或已有BOM");
            var query1 = (from dr in data.AsEnumerable()
                          join fg in fgData.AsEnumerable()
                          on dr.Field<string>("ProductCode").GetString() equals fg.Field<string>("ItemCode").GetString() into ldata
                          from ldr in ldata.DefaultIfEmpty()
                          select new
                          {
                              ProductCode = dr.Field<string>("ProductCode").GetString(),
                              ProductGuid = ldr == null? string.Empty:ldr.Field<string>("GUID").GetString(),
                              StdQty = dr.Field<string>("StdQty").ToDouble(3),
                              ActQty = dr.Field<string>("ActQty").ToDouble(3),
                              ItemCode = dr.Field<string>("ItemCode").GetString(),
                              Unit = dr.Field<string>("Unit").GetString()
                          }).ToList();
            var ckq1 = query1.Where(q=>q.ProductGuid.Equals(string.Empty));
            if (ckq1.Any())
                throw new Exception("成品代码不存在或已存在BOM,代码:" + string.Join(",", ckq1.Select(c => c.ProductCode).ToArray()));

            #endregion

            #region 检查原料是否存在,并且不重复
            StringBuilder rmSql = new StringBuilder("select ItemCode,GUID from [dbo].[tblItem] where ToBuy=1 "
             + "and Status=1 and ItemCode in (");
            rmSql.Append(string.Join(",", data.AsEnumerable().Select(dr => "'" + dr.Field<string>("ItemCode").GetString() + "'")
                .Distinct().ToArray())).Append(")");
            DataTable rmData = helper.GetDataTable(rmSql.ToString());
            if (rmData == null || rmData.Rows.Count == 0) throw new Exception("所有原料代码不存在");
            var query2 = (from q in query1
                          join rm in rmData.AsEnumerable()
                          on q.ItemCode equals rm.Field<string>("ItemCode").GetString() into ldata
                          from ldr in ldata.DefaultIfEmpty()
                          select new
                          {
                              ProductGuid = q.ProductGuid,
                              StdQty = q.StdQty,
                              ActQty = q.ActQty,
                              ItemGuid = ldr == null? string.Empty:ldr.Field<string>("GUID").GetString(),
                              ItemCode = q.ItemCode,
                              Unit = q.Unit
                          }).ToList();
            var ckq2 = query2.Where(q => q.ItemGuid.Equals(string.Empty));
            if (ckq2.Any())
                throw new Exception("原料代码不存在,代码:" + string.Join(",", ckq2.Select(c => c.ItemCode).ToArray()));
            var queryGrp = (from q in query2
                            group q by q.ProductGuid into g
                            select new
                            {
                                Key = g.Key,
                                Count = g.Count(),
                                Distinct = g.Select(g1=>g1.ItemGuid).Distinct().Count(),
                                Values = g
                            }).ToList();
            var ckq3 = queryGrp.Where(qg=>!qg.Count.Equals(qg.Distinct));
            if (ckq3.Any())
                throw new Exception("存在重复的原料代码,成品代码:" + string.Join(",", ckq3.Select(c => c.Key).ToArray()));

            #endregion

            string unitSql = "select GUID,NameCn,NameEn from [dbo].[tblUOM] where Active=1";
            DataTable uomData = helper.GetDataTable(unitSql);
            if (uomData == null || uomData.Rows.Count == 0) throw new Exception("系统中不存在单位数据");
            StringBuilder insertSql = new StringBuilder();
            string iSql = "insert into [dbo].[tblItemBOM](ProductGUID,ItemGUID,StdQty,ActQty,UOMGUID) "
                + "values('{0}','{1}',{2},{3},'{4}');";
            string unit = string.Empty;
            string uomGuid = string.Empty;
            foreach (var q in queryGrp)
            {
                foreach (var q1 in q.Values)
                {
                    if (string.IsNullOrWhiteSpace(uomGuid) || !q1.Unit.Equals(unit))
                    {
                        var uq = uomData.AsEnumerable().Where(u => u.Field<string>("NameCn").GetString().Equals(q1.Unit)
                            || u.Field<string>("NameEn").GetString().Equals(q1.Unit));
                        if (uq.Any()) uomGuid = uq.Select(u => u.Field<string>("GUID").GetString()).First();
                        else uomGuid = string.Empty;
                        if (string.IsNullOrWhiteSpace(uomGuid)) throw new Exception("单位错误:" + q1.Unit);
                        unit = q1.Unit;
                    }
                    insertSql.AppendFormat(iSql, q.Key, q1.ItemGuid, q1.StdQty, q1.ActQty, uomGuid);
                }
            }
            int count = helper.Execute(insertSql.ToString());
            if (count <= 0) throw new Exception("数据执行错误,没有数据被执行,请联系管理员");
            return string.Empty;
        }
        #endregion
    }
}
