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
    public class MastDataHelper : Common.BaseDataHelper
    {
        private static MastDataHelper instance = new MastDataHelper();
        private MastDataHelper() { }
        public static MastDataHelper GetInstance() { return instance; }

        #region Client
        public int GetSiteClientVersion(string siteGuid)
        {
            try
            {
                string sql = "select AppVersion from [dbo].[tblSite] where GUID='" + siteGuid + "'";
                return new Utils.Database.SqlServer.DBHelper(_conn).GetDataScalar(sql).ToInt();
            }
            catch { return 0; }
        }
        #endregion

        #region UOM
        /// <summary>
        /// 获取单位List
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="guid">union的单位guid</param>
        /// <returns></returns>
        public List<Model.UOM.UOMMast> GetItemUOMList(string language, string guid)
        {
            string sql = "select GUID,case when 'zh'='" + language + "' then NameCn else NameEn end UOMName from [dbo].[tblUOM] where Active=1";
            if (!string.IsNullOrWhiteSpace(guid))
                sql += " union select GUID,case when 'zh'='" + language + "' then NameCn else NameEn end UOMName from [dbo].[tblUOM] where GUID='" + guid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.UOM.UOMMast()
            {
                UOMGuid = dr.Field<string>("GUID").GetString(),
                UOMName = dr.Field<string>("UOMName").GetString()
            }).ToList();
        }

        /// <summary>
        /// 获取UOM数据对象
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        public List<Model.UOM.UOMMast> GetUOMList(string language)
        {
            try
            {
                string sql = "select GUID,case when 'zh'='" + language + "' then NameCn else NameEn end UOMName,ToUOMGUID,ToQty "
              + "from [dbo].[tblUOM] where Active=1 order by ID";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return null;
                List<Model.UOM.UOMMast> uomList = data.AsEnumerable().Select(dr => new Model.UOM.UOMMast()
                {
                    UOMGuid = dr.Field<string>("GUID").GetString(),
                    UOMName = dr.Field<string>("UOMName").GetString(),
                    BaseQty = dr.Field<decimal?>("ToQty").ToDouble(3),
                    BaseUOMGuid = dr.Field<string>("ToUOMGUID").GetString()
                }).ToList();
                List<Model.UOM.UOMMast> retUOMList = new List<Model.UOM.UOMMast>();
                foreach (DataRow dr in data.Rows)
                    retUOMList.Add(GetUOM(dr.Field<string>("GUID").ToString(), dr.Field<string>("UOMName").GetString(),
                        dr.Field<string>("ToUOMGUID").GetString(), dr.Field<decimal?>("ToQty").ToDouble(3, 1), uomList));
                return retUOMList;
            }
            catch { return null; }
        }

        private Model.UOM.UOMMast GetUOM(string guid, string name, string pGuid, double qty, List<Model.UOM.UOMMast> data)
        {
            Model.UOM.UOMMast uom = new Model.UOM.UOMMast();
            uom.UOMGuid = guid;
            uom.UOMName = name;
            uom.BaseUOMGuid = pGuid;
            uom.BaseQty = qty;
            if (string.IsNullOrWhiteSpace(pGuid)) return uom;
            int i = 0;
            int maxLevel = ConfigurationManager.AppSettings["UOMLevel"].ToInt();
            if (maxLevel.Equals(0)) maxLevel = 3; //不配置,只看3层
            while (i < maxLevel)
            {
                var query = data.Where(d => d.UOMGuid.Equals(uom.BaseUOMGuid));
                if (query.Any())
                {
                    var q = query.First();
                    if (string.IsNullOrWhiteSpace(q.BaseUOMGuid))
                    {
                        uom.BaseQty = uom.BaseQty.ToDouble(3);
                        uom.BaseUOMName = q.UOMName;
                        return uom;
                    }
                    else
                    {
                        uom.BaseUOMGuid = q.BaseUOMGuid;
                        uom.BaseQty *= q.BaseQty;
                    }
                }
                else
                {
                    uom.BaseQty = uom.BaseQty.ToDouble(3);
                    return uom;
                }
                i++;
            }
            return uom;
        }

        #endregion

        #region BU
        public Model.Table.TableMast<Model.BU.BUMast> GetTableBUList(string keyWords, string language)
        {
            Model.Table.TableMast<Model.BU.BUMast> tableMast = new Model.Table.TableMast<Model.BU.BUMast>();
            tableMast.draw = 1;
            tableMast.data = GetBUMastList(keyWords);
            if (tableMast.data == null) tableMast.data = new List<Model.BU.BUMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }


        /// <summary>
        /// 获取BU信息(BUGuid, BUCode, BUERPCode)
        /// </summary>
        /// <param name="BUGuids"></param>
        /// <returns></returns>
        public List<Model.BU.BUMast> GetBUList(List<string> BUGuids, bool contains)
        {
            try
            {
                StringBuilder sqlBuilder = new StringBuilder();
                sqlBuilder.Append("select BUGUID,Code,ERPCode from [dbo].[tblBU]");
                if (BUGuids != null && BUGuids.Count > 0)
                {
                    sqlBuilder.Append(" where BUGUID " + (contains ? "in" : "not in") + " (")
                     .Append(string.Join(",", BUGuids.Select(b => "'" + b + "'").ToArray())).Append(")");
                }
                sqlBuilder.Append(" order by ID");
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sqlBuilder.ToString());
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.BU.BUMast()
                {
                    BUGuid = dr.Field<string>("BUGUID").GetString(),
                    BUCode = dr.Field<string>("ERPCode").GetString(),
                    BUName = dr.Field<string>("Code").GetString()
                }).ToList();
            }
            catch { return null; }
        }

        /// <summary>
        /// 获取BU信息(BUGUID,BUCode,BUERPCode,TimeOut,EndTime,ParentGUID,ParentCode)
        /// </summary>
        /// <returns></returns>
        public List<Model.BU.BUMast> GetBUMastList(string keyWords)
        {
            try
            {
                string sql = "select a1.BUGUID,a1.Code,a1.ERPCode,a1.EndHour,a1.TimeOut,a2.Code as ParentCode "
                    + "from [dbo].[tblBU] a1 left join [dbo].[tblBU] a2 on a1.ParentGUID=a2.BUGUID and "
                    + "(a1.Code like @search or a1.ERPCode like @search) order by a1.ID";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "BUMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.BU.BUMast()
                {
                    BUGuid = dr.Field<string>("BUGUID").GetString(),
                    BUCode = dr.Field<string>("ERPCode").GetString(),
                    BUName = dr.Field<string>("Code").GetString(),
                    ParentName = dr.Field<string>("ParentCode").GetString(),
                    EndTime = dr.Field<string>("EndHour").GetString(),
                    TimeOut = dr.Field<int?>("TimeOut").ToInt()
                }).ToList();
            }
            catch { return null; }
        }

        public Model.BU.BUMast GetBUMast(string BUGuid)
        {
            string sql = "select a1.Code,a1.ERPCode,a1.EndHour,a1.TimeOut,a1.ParentGUID,a2.Code as ParentCode "
                   + "from [dbo].[tblBU] a1 left join [dbo].[tblBU] a2 on a1.ParentGUID=a2.BUGUID where a1.BUGUID='"
                   + BUGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return new Model.BU.BUMast()
            {
                BUCode = data.Rows[0].Field<string>("ERPCode").GetString(),
                BUGuid = BUGuid,
                BUName = data.Rows[0].Field<string>("Code").GetString(),
                EndTime = data.Rows[0].Field<string>("EndHour").GetString(),
                ParentGuid = data.Rows[0].Field<string>("ParentGUID").GetString(),
                ParentName = data.Rows[0].Field<string>("ParentCode").GetString(),
                TimeOut = data.Rows[0].Field<int?>("TimeOut").ToInt()
            };
        }

        public DataTable GetBUDatas()
        {
            try
            {
                string sql = "select BUGUID,ParentGUID from [dbo].[tblBU]";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                return data;
            }
            catch { return null; }
        }

        public List<string> GetChildBUGuids(string BUGuid)
        {
            return GetChildBUGuids(BUGuid, GetBUDatas());
        }

        /// <summary>
        /// 根据BUGuid获取所有子级BUGuid
        /// </summary>
        /// <param name="BUGuid"></param>
        /// <returns>所有子级和自身</returns>
        public List<string> GetChildBUGuids(string BUGuid, DataTable data)
        {
            if (string.IsNullOrWhiteSpace(BUGuid)) return null;
            if (data == null || data.Rows.Count == 0) return null;
            List<string> retGuids = new List<string>() { BUGuid };
            var query = data.AsEnumerable().Where(dr => dr.Field<string>("ParentGUID").GetString().Equals(BUGuid));
            if (query.Any())
            {
                foreach (var q in query)
                {
                    retGuids.Add(q.Field<string>("BUGUID").GetString());
                    List<string> childGuids = GetAllChildGUGuids(q.Field<string>("BUGUID").GetString(), data);
                    if (childGuids != null && childGuids.Count > 0) retGuids.AddRange(childGuids);
                }
            }
            return retGuids;
        }

        /// <summary>
        /// 递归获取所有子BUGUID
        /// </summary>
        /// <param name="parentGuid">父层GUID</param>
        /// <param name="data">BU数据</param>
        /// <returns></returns>
        private List<string> GetAllChildGUGuids(string parentGuid, DataTable data)
        {
            var query = data.AsEnumerable().Where(dr => dr.Field<string>("ParentGUID").GetString().Equals(parentGuid));
            if (query.Any())
            {
                List<string> retGuids = new List<string>();
                foreach (var q in query)
                {
                    retGuids.Add(q.Field<string>("BUGUID").GetString());
                    List<string> cGuids = GetAllChildGUGuids(q.Field<string>("BUGUID").GetString(), data);
                    if (cGuids != null && cGuids.Count > 0) retGuids.AddRange(cGuids);
                }
                return retGuids;
            }
            else return null;
        }

        public List<string> GetParentBUGuids(string BUGuid)
        {
            return GetParentBUGuids(BUGuid, GetBUDatas());
        }

        /// <summary>
        /// 根据BUGuid获取此BU的所有父层Guid,以及自身
        /// </summary>
        /// <param name="BUGuid">自身Guid</param>
        /// <returns>所有父层Guid以及自身</returns>
        public List<string> GetParentBUGuids(string BUGuid, DataTable data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BUGuid)) return null;
                if (data == null || data.Rows.Count == 0) return null;
                string guid = BUGuid;
                List<string> parentGuids = new List<string>();
                int i = 0;
                int maxLevel = ConfigurationManager.AppSettings["BULevel"].ToInt();
                if (maxLevel.Equals(0)) maxLevel = 3; //不配置,只看3层
                while (i < maxLevel)
                {
                    var query = data.AsEnumerable().Where(dr => dr.Field<string>("BUGUID").GetString().Equals(guid));
                    if (query.Any())
                    {
                        var q = query.First();
                        parentGuids.Add(guid);
                        if (q.Field<string>("ParentGUID").GetString().Equals(string.Empty)) break;
                        else guid = q.Field<string>("ParentGUID").GetString();
                    }
                    else break;
                }
                return parentGuids;
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse EditBU(Model.BU.BUMast data)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (data == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                if (string.IsNullOrWhiteSpace(data.BUGuid))
                    sql = "insert into [dbo].[tblBU](BUGUID,Code,ParentGUID,ERPCode,EndHour,TimeOut) "
                        + "values(newid(),@name,@pguid,@code,@endhour,@timeout)";
                else sql = "update [dbo].[tblBU] set Code=@name,ERPCode=@code,ParentGUID=@pguid,EndHour=@endhour,"
                        + "TimeOut=@timeout where BUGUID='" + data.BUGuid + "'";
                System.Data.SqlClient.SqlParameter name =
                    new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 16);
                name.Value = data.BUName.GetString();
                System.Data.SqlClient.SqlParameter code =
                    new System.Data.SqlClient.SqlParameter("@code", SqlDbType.Char, 3);
                code.Value = data.BUCode.GetString();
                System.Data.SqlClient.SqlParameter pguid =
                 new System.Data.SqlClient.SqlParameter("@pguid", SqlDbType.Char, 36);
                pguid.Value = data.ParentGuid.GetString();
                System.Data.SqlClient.SqlParameter endhour =
                   new System.Data.SqlClient.SqlParameter("@endhour", SqlDbType.VarChar, 50);
                endhour.Value = data.EndTime.GetString();
                System.Data.SqlClient.SqlParameter timeout =
                   new System.Data.SqlClient.SqlParameter("@timeout", SqlDbType.Int, 8);
                timeout.Value = data.TimeOut;
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                          new System.Data.SqlClient.SqlParameter[] { code, name, pguid, endhour, timeout });
                if (count > 0)
                {
                    resp.Status = "ok";
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
        #endregion

        #region Site
        public Model.Table.TableMast<Model.Site.SiteMast> GetTableSiteList(string BUGuid, string keyWords, string language)
        {
            Model.Table.TableMast<Model.Site.SiteMast> tableMast = new Model.Table.TableMast<Model.Site.SiteMast>();
            tableMast.draw = 1;
            tableMast.data = GetSiteMastList(BUGuid, keyWords);
            if (tableMast.data == null) tableMast.data = new List<Model.Site.SiteMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        /// <summary>
        /// 根据SiteGuid或者BuGuid获取营运点(GUID,代码,名称)
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="siteGuid">营运点Guid</param>
        /// <param name="BUGuid">BUGuid</param>
        /// <returns></returns>
        public List<Model.Site.SiteMast> GetSiteMastList(string language, string siteGuid, string BUGuid)
        {
            if (string.IsNullOrWhiteSpace(siteGuid) && string.IsNullOrWhiteSpace(BUGuid)) return null;
            StringBuilder sqlBuider = new StringBuilder();
            sqlBuider.Append("select guid,code,case when 'zh'='"
                + language + "' then compnamecn else compnameen end name,needWork from [dbo].tblSite where ");
            if (string.IsNullOrWhiteSpace(siteGuid)) sqlBuider.Append("buguid='" + BUGuid + "'");
            else sqlBuider.Append("guid='" + siteGuid + "'");
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sqlBuider.ToString());
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Site.SiteMast()
            {
                SiteCode = dr.Field<string>("code").GetString(),
                SiteGuid = dr.Field<string>("guid").GetString(),
                SiteName = dr.Field<string>("name").GetString(),
                needWork = dr.Field<bool>("needWork")
            }).ToList();
        }

        public List<Model.Site.SiteMast> GetSiteMastList()
        {
            string sql = "select guid,code from [dbo].[tblSite]";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Site.SiteMast()
            {
                SiteCode = dr.Field<string>("code").GetString(),
                SiteGuid = dr.Field<string>("guid").GetString()
            }).ToList();
        }

        /// <summary>
        /// 根据BUGuid获取营运点明细(GUID,中文名称,英文名称,代码,地址,电话)
        /// </summary>
        /// <param name="BUGuid">BUGuid</param>
        /// <returns></returns>
        public List<Model.Site.SiteMast> GetSiteMastList(string BUGuid, string keyWords)
        {

            string sql = "select GUID,Code,CompNameCn,CompNameEn,Address,TelNbr from [dbo].[tblSite] "
                + "where BUGUID='" + BUGuid + "' and (Code like @search or CompNameCn like @search or CompNameEn like @search) order by ID";
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = dbHelper.GetDataTable(sql, "SiteMast", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Site.SiteMast()
            {
                SiteCode = dr.Field<string>("code").GetString(),
                SiteGuid = dr.Field<string>("guid").GetString(),
                NameCn = dr.Field<string>("CompNameCn").GetString(),
                NameEn = dr.Field<string>("CompNameEn").GetString(),
                Address = dr.Field<string>("Address").GetString(),
                TelNbr = dr.Field<string>("TelNbr").GetString()
            }).ToList();
        }
        public Model.Site.SiteMast GetSiteMast(string siteGuid)
        {
            string sql = "select Code,CompNameCn,CompNameEn,Address,TelNbr,PostCode,BUGUID from [dbo].[tblSite] "
                + "where GUID='" + siteGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count != 1) return null;
            return new Model.Site.SiteMast()
            {
                SiteCode = data.Rows[0].Field<string>("Code").GetString(),
                NameCn = data.Rows[0].Field<string>("CompNameCn").GetString(),
                NameEn = data.Rows[0].Field<string>("CompNameEn").GetString(),
                Address = data.Rows[0].Field<string>("Address").GetString(),
                SiteGuid = siteGuid,
                TelNbr = data.Rows[0].Field<string>("TelNbr").GetString(),
                PostCode = data.Rows[0].Field<string>("PostCode").GetString(),
                BUGuid = data.Rows[0].Field<string>("BUGUID").GetString()
            };
        }
        public Model.Common.BaseResponse EditSite(Model.Site.SiteMast site)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (site == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                if (string.IsNullOrWhiteSpace(site.SiteGuid))
                    sql = "insert into [dbo].[tblSite](GUID,Code,CompNameCn,CompNameEn,BUGUID,Address,PostCode,TelNbr) "
                        + "values(newid(),@code,@namecn,@nameen,@buguid,@adr,@pcode,@tel)";
                else sql = "update [dbo].[tblSite] set Code=@code,CompNameCn=@namecn,CompNameEn=@nameen,BUGUID=@buguid,"
                        + "Address=@adr,PostCode=@pcode,TelNbr=@tel where GUID='" + site.SiteGuid + "'";
                System.Data.SqlClient.SqlParameter code =
                    new System.Data.SqlClient.SqlParameter("@code", SqlDbType.VarChar, 16);
                code.Value = site.SiteCode.GetString();
                System.Data.SqlClient.SqlParameter namecn =
                    new System.Data.SqlClient.SqlParameter("@namecn", SqlDbType.VarChar, 32);
                namecn.Value = site.NameCn.GetString();
                System.Data.SqlClient.SqlParameter nameen =
                    new System.Data.SqlClient.SqlParameter("@nameen", SqlDbType.VarChar, 64);
                nameen.Value = site.NameEn.GetString();
                System.Data.SqlClient.SqlParameter adr =
                     new System.Data.SqlClient.SqlParameter("@adr", SqlDbType.VarChar, 64);
                adr.Value = site.Address.GetString();
                System.Data.SqlClient.SqlParameter pcode =
                  new System.Data.SqlClient.SqlParameter("@pcode", SqlDbType.VarChar, 8);
                pcode.Value = site.PostCode.GetString();
                System.Data.SqlClient.SqlParameter tel =
                  new System.Data.SqlClient.SqlParameter("@tel", SqlDbType.VarChar, 32);
                tel.Value = site.TelNbr.GetString();
                System.Data.SqlClient.SqlParameter buguid =
                 new System.Data.SqlClient.SqlParameter("@buguid", SqlDbType.Char, 36);
                buguid.Value = site.BUGuid.GetString();
                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                          new System.Data.SqlClient.SqlParameter[] { code, namecn, nameen, pcode, tel, adr, buguid });
                if (count > 0)
                {
                    resp.Status = "ok";
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

        #endregion

        #region Supplier

        public List<Model.Supplier.SupplierMast> GetSupplierList(string language)
        {
            string sql = "select SupplierGUID,SupplierCode,case when 'zh'='zh' then CompNameCn else CompNameEn end "
               + "SupplierName from [dbo].[tblSupplier]  where Active=1";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Supplier.SupplierMast()
            {
                SupplierCode = dr.Field<string>("SupplierCode").GetString(),
                SupplierGuid = dr.Field<string>("SupplierGUID").GetString(),
                SupplierName = dr.Field<string>("SupplierName").GetString()
            }).ToList();
        }

        public List<Model.Supplier.SupplierMast> GetSupplierListByBU(string language, string BUGuid)
        {
            string sql = "select a1.SupplierGUID,a2.SupplierCode,case when 'zh'='zh' then a2.CompNameCn else a2.CompNameEn end "
                + "SupplierName from [dbo].[tblSupplierSite] a1,[dbo].[tblSupplier] a2 where a1.SupplierGUID=a2.SupplierGUID "
                + "and a2.Active=1 and (a1.BUGUID='" + BUGuid + "' or a1.SiteGUID in (select GUID from [dbo].[tblSite] "
                + "where BUGUID='" + BUGuid + "'))";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Supplier.SupplierMast()
            {
                SupplierCode = dr.Field<string>("SupplierCode").GetString(),
                SupplierGuid = dr.Field<string>("SupplierGUID").GetString(),
                SupplierName = dr.Field<string>("SupplierName").GetString()
            }).ToList();
        }

        public Model.Table.TableMast<Model.Supplier.SupplierMast> GetTableSupplierMastList(int status, string keyWords, string language)
        {
            Model.Table.TableMast<Model.Supplier.SupplierMast> tableMast = new Model.Table.TableMast<Model.Supplier.SupplierMast>();
            tableMast.draw = 1;
            tableMast.data = GetSupplierMastList(status, keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Supplier.SupplierMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Supplier.SupplierMast> GetSupplierMastList(int status, string keyWords, string language)
        {
            try
            {
                string sql = "select SupplierGUID,SupplierCode,CompNameCn,CompNameEn,"
                    + "ContactName,EmailAddress from [dbo].[tblSupplier] where Active=" + status
                    + " and (SupplierCode like @search or CompNameCn like @search or CompNameEn like @search) order by ID";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "SupplierMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.Supplier.SupplierMast()
                {
                    SupplierCode = dr.Field<string>("SupplierCode").GetString(),
                    SupplierGuid = dr.Field<string>("SupplierGUID").GetString(),
                    SupplierName = dr.Field<string>("CompNameCn").GetString(),
                    SupplierNameEn = dr.Field<string>("CompNameEn").GetString(),
                    ContactName = dr.Field<string>("ContactName").GetString(),
                    EmailAddress = dr.Field<string>("EmailAddress").GetString()
                }).ToList();
            }
            catch { return null; }
        }

        public Model.Supplier.SupplierMast GetSupplierMast(string supplierGuid)
        {
            try
            {
                string sql = "select SupplierCode,CompNameCn,CompNameEn,Address,PostCode,TelNbr,ContactName,"
                           + "EmailAddress,MobileNbr,Active from [dbo].[tblSupplier] where SupplierGUID='" + supplierGuid + "'";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count != 1) return null;
                return new Model.Supplier.SupplierMast()
                {
                    SupplierGuid = supplierGuid,
                    SupplierCode = data.Rows[0].Field<string>("SupplierCode").GetString(),
                    ContactName = data.Rows[0].Field<string>("ContactName").GetString(),
                    SupplierName = data.Rows[0].Field<string>("CompNameCn").GetString(),
                    SupplierNameEn = data.Rows[0].Field<string>("CompNameEn").GetString(),
                    Address = data.Rows[0].Field<string>("Address").GetString(),
                    PostCode = data.Rows[0].Field<string>("PostCode").GetString(),
                    TelNbr = data.Rows[0].Field<string>("TelNbr").GetString(),
                    EmailAddress = data.Rows[0].Field<string>("EmailAddress").GetString(),
                    MobileNbr = data.Rows[0].Field<string>("MobileNbr").GetString(),
                    Status = data.Rows[0].Field<bool?>("Active").BoolToInt()
                };
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse ModifySupplier(Model.Supplier.SupplierMast supplierMast, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (supplierMast == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                if (string.IsNullOrWhiteSpace(supplierMast.SupplierGuid))
                {
                    sql = "insert into [dbo].[tblSupplier](SupplierGUID,SupplierCode,CompNameCn,CompNameEn,Address,PostCode,"
                        + "TelNbr,ContactName,EmailAddress,MobileNbr,Active,CreateTime) "
                        + "values('" + Guid.NewGuid().ToString().ToUpper()
                        + "',@supcode,@namecn,@nameen,@adr,@postcode,@tel,@contact,@email,@mobile,@status,@date);";
                }
                else
                {
                    sql = "update [dbo].[tblSupplier] set SupplierCode=@supcode,CompNameCn=@namecn,CompNameEn=@nameen,"
                        + "Address=@adr,PostCode=@postcode,TelNbr=@tel,ContactName=@contact,EmailAddress=@email,"
                        + "MobileNbr=@mobile,Active=@status where SupplierGUID='" + supplierMast.SupplierGuid + "'";
                }
                #region 设置参数
                System.Data.SqlClient.SqlParameter supcode =
                    new System.Data.SqlClient.SqlParameter("@supcode", SqlDbType.VarChar, 16);
                supcode.Value = supplierMast.SupplierCode.GetString();
                System.Data.SqlClient.SqlParameter namecn =
                    new System.Data.SqlClient.SqlParameter("@namecn", SqlDbType.VarChar, 32);
                namecn.Value = supplierMast.SupplierName.GetString();
                System.Data.SqlClient.SqlParameter nameen =
                    new System.Data.SqlClient.SqlParameter("@nameen", SqlDbType.VarChar, 64);
                nameen.Value = supplierMast.SupplierNameEn.GetString();
                System.Data.SqlClient.SqlParameter adr =
                     new System.Data.SqlClient.SqlParameter("@adr", SqlDbType.VarChar, 64);
                adr.Value = supplierMast.Address.GetString();
                System.Data.SqlClient.SqlParameter postcode =
                  new System.Data.SqlClient.SqlParameter("@postcode", SqlDbType.VarChar, 8);
                postcode.Value = supplierMast.PostCode.GetString();
                System.Data.SqlClient.SqlParameter tel =
                  new System.Data.SqlClient.SqlParameter("@tel", SqlDbType.VarChar, 32);
                tel.Value = supplierMast.TelNbr.GetString();
                System.Data.SqlClient.SqlParameter mobile =
                  new System.Data.SqlClient.SqlParameter("@mobile", SqlDbType.VarChar, 16);
                mobile.Value = supplierMast.MobileNbr.GetString();
                System.Data.SqlClient.SqlParameter contact =
                  new System.Data.SqlClient.SqlParameter("@contact", SqlDbType.VarChar, 32);
                contact.Value = supplierMast.ContactName.GetString();
                System.Data.SqlClient.SqlParameter email =
                  new System.Data.SqlClient.SqlParameter("@email", SqlDbType.VarChar, 64);
                email.Value = supplierMast.EmailAddress.GetString();
                System.Data.SqlClient.SqlParameter status =
                  new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                status.Value = supplierMast.Status;
                System.Data.SqlClient.SqlParameter date =
                  new System.Data.SqlClient.SqlParameter("@date", SqlDbType.DateTime, 23);
                date.Value = DateTime.Now;
                #endregion

                int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                          new System.Data.SqlClient.SqlParameter[] { supcode, namecn, nameen, adr, postcode, tel, contact, email, mobile, status, date });
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = supplierMast.SupplierCode;
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

        public Model.Common.BaseResponse ModifySupplierSites(Model.Supplier.SupplierMast supplierMast, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (supplierMast == null) throw new Exception("Nothing to process.");
                if (string.IsNullOrWhiteSpace(supplierMast.SupplierGuid)) throw new Exception("Supplier Guid error");
                StringBuilder sql = new StringBuilder();
                if (supplierMast.Sites == null || supplierMast.Sites.Count == 0)
                {
                    if (supplierMast.IsDel.ToInt().Equals(1))
                        sql.Append("delete [dbo].[tblSupplierSite] where SupplierGuid='" + supplierMast.SupplierGuid + "'");
                }
                else
                {
                    string insertSql = "insert into [dbo].[tblSupplierSite](SupplierGUID,SiteGUID) values('"
                            + supplierMast.SupplierGuid + "','{0}')";
                    sql.Append("delete [dbo].[tblSupplierSite] where SupplierGuid='" + supplierMast.SupplierGuid + "';");
                    sql.Append(string.Join(";", supplierMast.Sites.Select(s => string.Format(insertSql, s.SiteGuid)).ToArray()));
                }
                if (sql.Length > 0)
                {
                    int count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql.ToString());
                    if (count > 0) resp.Status = "ok";
                    else
                    {
                        resp.Status = "error";
                        resp.Msg = "No record been modified.";
                    }
                }
            }
            catch (Exception e) { resp.Msg = e.Message; resp.Status = "error"; }
            return resp;
        }

        public List<Model.Supplier.SupplierSite> GetSupplierSiteList(string supplierGuid)
        {
            string sql = "select a1.SiteGUID,a2.Code from [dbo].[tblSupplierSite] a1 join [dbo].[tblSite] a2 on a1.SiteGUID=a2.GUID "
                + "where a1.SupplierGUID='" + supplierGuid + "'";
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Supplier.SupplierSite()
            {
                SiteCode = dr.Field<string>("Code").GetString(),
                SiteGuid = dr.Field<string>("SiteGUID").GetString()
            }).ToList();
        }

        #endregion

        #region Calendar
        public Model.Table.TableMast<Model.Calendar.CalendarMast> GetTableCalendarMastList(string keyWords, string language)
        {
            Model.Table.TableMast<Model.Calendar.CalendarMast> tableMast = new Model.Table.TableMast<Model.Calendar.CalendarMast>();
            tableMast.draw = 1;
            tableMast.data = GetCalendarList(keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Calendar.CalendarMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Calendar.CalendarMast> GetCalendarList(string keyWords, string language)
        {
            string sql = "select a1.ID,a1.Name,a2.Code as BUName,a3.Code as SiteCode,a1.StartDate,a1.EndDate,a1.Working "
                + "from [dbo].[tblCalendars] a1 left join [dbo].[tblBU] a2 on a1.BUGUID=a2.BUGUID left join [dbo].[tblSite] a3 "
                + "on a1.SiteGUID=a3.GUID where EndDate>=getdate() "
                + "and (a1.Name like @search or isnull(a2.Code,'') like @search or isnull(a3.Code,'') like @search) order by a1.ID";
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = dbHelper.GetDataTable(sql, "CalendarMast", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Calendar.CalendarMast()
            {
                ID = dr.Field<int>("ID"),
                Name = dr.Field<string>("Name").GetString(),
                BUName = dr.Field<string>("BUName").GetString(),
                SiteCode = dr.Field<string>("SiteCode").GetString(),
                StartDate = dr.Field<DateTime?>("StartDate").ToFormatDate("yyyy-MM-dd", "Min"),
                EndDate = dr.Field<DateTime?>("EndDate").ToFormatDate("yyyy-MM-dd", "Max"),
                Working = dr.Field<bool?>("Working").BoolToInt()
            }).ToList();
        }

        public Model.Calendar.CalendarMast GetCalendarMast(int calendarID)
        {
            string sql = "select a1.Name,a1.BUGUID,a2.Code as BUName,a1.SiteGUID,a3.Code as SiteCode,a1.StartDate,a1.EndDate, "
                + "isnull(a1.StartTime,null) StartTime,isnull(a1.EndTime,null) EndTime, "
                + "a1.Working from [dbo].[tblCalendars] a1 left join [dbo].[tblBU] a2 on a1.BUGUID=a2.BUGUID "
                + "left join [dbo].[tblSite] a3 on a1.SiteGUID=a3.GUID where a1.ID=" + calendarID;
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count != 1) return null;
            return new Model.Calendar.CalendarMast()
            {
                ID = calendarID,
                Name = data.Rows[0].Field<string>("Name").GetString(),
                BUGuid = data.Rows[0].Field<string>("BUGUID").GetString(),
                BUName = data.Rows[0].Field<string>("BUName").GetString(),
                SiteGuid = data.Rows[0].Field<string>("SiteGuid").GetString(),
                SiteCode = data.Rows[0].Field<string>("SiteCode").GetString(),
                StartDate = data.Rows[0].Field<DateTime?>("StartDate").ToFormatDate("yyyy-MM-dd", "Min"),
                EndDate = data.Rows[0].Field<DateTime?>("EndDate").ToFormatDate("yyyy-MM-dd", "Max"),
                startTime = data.Rows[0].Field<DateTime?>("StartTime").ToFormatDate("HH:mm", "Min"),
                endTime = data.Rows[0].Field<DateTime?>("EndTime").ToFormatDate("HH:mm", "Min"),
                Working = data.Rows[0].Field<bool?>("Working").BoolToInt()
            };
        }

        public Model.Common.BaseResponse ModifyCalendar(Model.Calendar.CalendarMast calendarMast)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (calendarMast == null) throw new Exception("Nothing to process.");
                int count = 0;
                if (calendarMast.IsDel.Equals(1) && !calendarMast.ID.Equals(0))
                {
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute("delete [dbo].[tblCalendars] where ID=" +
                        calendarMast.ID);
                }
                else
                {
                    if (DateTime.Parse(calendarMast.StartDate) > DateTime.Parse(calendarMast.EndDate))
                        throw new Exception("Date Range error");
                    string sql = string.Empty;
                    if (calendarMast.ID.Equals(0))
                        sql = "insert into [dbo].[tblCalendars](Name,BUGUID,SiteGUID,StartDate,EndDate,Working) "
                            + "values(@name,@buguid,@siteguid,@sdate,@edate,@working)";
                    else sql = "update [dbo].[tblCalendars] set Name=@name,BUGUID=@buguid,SiteGUID=@siteguid,StartDate=@sdate,"
                            + "EndDate=@edate,Working=@working where ID=" + calendarMast.ID;
                    System.Data.SqlClient.SqlParameter name = new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 50);
                    name.Value = calendarMast.Name.GetString();
                    System.Data.SqlClient.SqlParameter buguid = new System.Data.SqlClient.SqlParameter("@buguid", SqlDbType.Char, 36);
                    System.Data.SqlClient.SqlParameter siteguid = new System.Data.SqlClient.SqlParameter("@siteguid", SqlDbType.Char, 36);
                    if (!string.IsNullOrWhiteSpace(calendarMast.BUGuid))
                    {
                        buguid.Value = calendarMast.BUGuid;
                        siteguid.Value = DBNull.Value;
                    }
                    else
                    {
                        buguid.Value = DBNull.Value;
                        if (string.IsNullOrWhiteSpace(calendarMast.SiteGuid)) siteguid.Value = DBNull.Value;
                        else siteguid.Value = calendarMast.SiteGuid;
                    }
                    System.Data.SqlClient.SqlParameter sdate = new System.Data.SqlClient.SqlParameter("@sdate", SqlDbType.DateTime, 23);
                    sdate.Value = DateTime.Parse(calendarMast.StartDate);
                    System.Data.SqlClient.SqlParameter edate = new System.Data.SqlClient.SqlParameter("@edate", SqlDbType.DateTime, 23);
                    edate.Value = DateTime.Parse(calendarMast.EndDate);
                    System.Data.SqlClient.SqlParameter working = new System.Data.SqlClient.SqlParameter("@working", SqlDbType.Bit, 1);
                    working.Value = calendarMast.Working;

                    //System.Data.SqlClient.SqlParameter stime = new System.Data.SqlClient.SqlParameter("@stime", SqlDbType.DateTime, 23);
                    //stime.Value =  DateTime.Parse(calendarMast.StartDate).ToString("yyyy-MM-dd")+' '+ DateTime.Parse(calendarMast.startTime).ToString("HH:mm:ss.fff");
                    //System.Data.SqlClient.SqlParameter etime = new System.Data.SqlClient.SqlParameter("@etime", SqlDbType.DateTime, 23);
                    //etime.Value = DateTime.Parse(calendarMast.EndDate).ToString("yyyy-MM-dd") +' '+DateTime.Parse(calendarMast.endTime).ToString("HH:mm:ss.fff");

                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                              new System.Data.SqlClient.SqlParameter[] { name, buguid, siteguid, sdate, edate, working });
                }
                if (count > 0)
                {
                    resp.Status = "ok";
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

        #endregion

        #region POS基础报告 - Angel添加
        public Model.Table.TableMast<Model.MBSales.MBSalesMast> GetKeyTableMastList(string stallentity, string startdate, string enddate, string keyWord, string All, string Title, string language)
        {
            Model.Table.TableMast<Model.MBSales.MBSalesMast> tableMast = new Model.Table.TableMast<Model.MBSales.MBSalesMast>();
            tableMast.draw = 1;
            tableMast.data = GetKeyTable(stallentity, startdate, enddate, keyWord, Title, All, language);
            if (tableMast.data == null) tableMast.data = new List<Model.MBSales.MBSalesMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.MBSales.MBSalesMast> GetKeyTable(string stallentity, string startdate, string enddate, string keyWord, string Title, string All, string language)
        {
            string sql = "select " + All + " from (select a6.CounterNo,a6.CounterName,convert(varchar(4),year(a1.AccDate)) as Year,convert(varchar(4),year(a1.AccDate))+'/'+convert(varchar(2),month(a1.AccDate)) as Month,convert(varchar(10),a1.AccDate,23) 'operDate',convert(varchar(20),a1.OpenDeskDate,25) as Time,a1.flowno,a2.cardNo,a3.mbName,a5.TypeName,a1.pluno,a4.foodname,convert(decimal(18,2),a1.salePrice) as salePrice, "
                       + "convert(decimal(18,0),case a1.sellWay when 'A' then a1.saleQty when 'B' then -a1.saleQty end) as saleQty, "
                       + "convert(decimal(18,2),case a1.sellWay when 'A' then a1.saleAmt when 'B' then -a1.saleAmt end) as saleAmt, "
                       + "convert(decimal(18,2),a1.DiscAmt) as DiscAmt from [dbo].[pos_tranflow] a1 "
                       + "left join (select distinct flowno,cardNo from [dbo].[pos_tranpay]) a2 on a2.flowno=a1.flowno and isnull(a2.cardNo,'')<>'' left join [dbo].[pos_mbInfo] a3 "
                       + "on a3.mbNo=a2.cardNo left join [dbo].[tbl_foodinfo] a4 on a4.foodno=a1.pluno "
                       + "left join (select b1.mbno, b1.CardTypeID,b2.TypeName from [dbo].[pos_mbCard] b1 "
                       + "left join [dbo].[pos_mbtype] b2 on b2.TypeID=b1.CardTypeID) a5 on a5.mbno=a2.cardNo "
                       + "left join [dbo].[tbl_CounterInfo] a6 on a6.CounterNo=a1.operId where "
                       + "(isnull(a2.cardNo,'')+isnull(a3.mbName,'')+isnull(a5.TypeName,'')+isnull(a1.pluno,'')+isnull(a4.foodname,'')) like @search and a6.CounterName "
                       + "like @stall and a1.AccDate between @startdate and @enddate) H " + Title;
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@stall", "%" + stallentity.GetString() + "%");
            System.Data.SqlClient.SqlParameter p2 = new System.Data.SqlClient.SqlParameter("@startdate", DateTime.Parse(startdate).ToString("yyyy-MM-dd"));
            System.Data.SqlClient.SqlParameter p3 = new System.Data.SqlClient.SqlParameter("@enddate", DateTime.Parse(enddate).ToString("yyyy-MM-dd"));
            System.Data.SqlClient.SqlParameter p4 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWord.GetString() + "%");

            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn2);
            DataTable data = dbHelper.GetDataTable(sql, "KeyTable", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1, p2, p3, p4 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.MBSales.MBSalesMast()
            {
                CounterNo = dr.Field<string>("CounterNo").GetString(),
                CounterName = dr.Field<string>("CounterName").GetString(),
                cardNo = dr.Field<string>("cardNo").GetString(),
                mbName = dr.Field<string>("mbName").GetString(),
                TypeName = dr.Field<string>("TypeName").GetString(),
                Year = dr.Field<string>("Year").GetString(),
                Month = dr.Field<string>("Month").GetString(),
                operDate = dr.Field<string>("operDate").GetString(),
                Time = dr.Field<string>("Time").GetString(),
                pluno = dr.Field<string>("pluno").GetString(),
                foodname = dr.Field<string>("foodname").GetString(),
                saleQty = dr.Field<decimal?>("saleQty").GetString(),
                saleAmt = dr.Field<decimal?>("saleAmt").GetString(),
                DiscAmt = dr.Field<decimal?>("DiscAmt").GetString(),

            }).ToList();
        }

        #endregion
        #region Stall
        public Model.Table.TableMast<Model.StallSales.StallSalesMast> GetTableStallMastList(string keyWord, string startdate, string enddate, string language)
        {
            Model.Table.TableMast<Model.StallSales.StallSalesMast> tableMast = new Model.Table.TableMast<Model.StallSales.StallSalesMast>();
            tableMast.draw = 1;
            tableMast.data = GetStallMastList(keyWord, startdate, enddate, language);
            if (tableMast.data == null) tableMast.data = new List<Model.StallSales.StallSalesMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public List<Model.StallSales.StallSalesMast> GetStallMastList(string keyWords, string startdate, string enddate, string language)
        {
            string sql = "select H.CounterNo,H.CounterName,convert(decimal(18,2),sum(H.SalesTO)) 'GrossAmt',convert(decimal(18,2),sum(H.Discount)) 'DiscountAmt',convert(decimal(18,2),sum(H.SalesAmt)) 'SalesAmt',convert(decimal(18,2),sum(H.Qty)) 'TransactionQty',sum(H.TranQty) 'DealQty',convert(decimal(18,2),sum(H.Qty)+sum(H.QtyBack)) 'SalesQty' from "
            + "(select a1.CounterNo,a2.CounterName,a3.TypeName,a1.see,a1.AccDate, "
            + "case a1.see when 'EA' then sum(a1.GrosalAMT) else -sum(a1.GrosalAMT) end 'SalesTO', "
            + "case a1.see when 'EA' then sum(a1.DiscAmt) else -sum(a1.DiscAmt) end 'Discount', "
            + "case a1.see when 'EA' then sum(a1.SaleAmt) else -sum(a1.SaleAmt) end 'SalesAmt', "
            + "case a1.see when 'EA' then sum(a1.saleQty) else -sum(a1.saleQty) end 'Qty', "
            + "case a1.see when 'EA' then count(a1.Count) else -count(a1.Count) end 'TranQty', "
            + "case a1.see when 'CB' then sum(a1.saleQty) else 0 end 'QtyBack', "
            + "case a1.see when 'CB' then sum(a1.SaleAmt) when 'EB' then sum(a1.SaleAmt) else 0 end 'AmtBack' from "
            + "(select b1.payWay+b1.sellWay 'see',case b1.payWay+b1.sellWay when 'EA' then 1 when 'EB' then -1 end as Count, "
            + "b1.* from [dbo].[pos_tranpay] b1) a1 join [dbo].[tbl_CounterInfo] a2 on a1.CounterNo=a2.CounterNo "
            + "left join (select a1.CardID,a2.TypeID,a2.TypeName from [dbo].[pos_mbCard] a1 left join [dbo].[pos_mbtype] a2 "
            + "on a2.TypeID=a1.CardTypeID) a3 on a3.CardID=a1.cardNo group by a1.CounterNo,a2.CounterName,a1.sellWay,a1.see,a3.TypeName,a1.AccDate) H "
            + "where H.CounterName like @search and H.AccDate between @startdate and @enddate "
            + "group by H.CounterNo,H.CounterName";

            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
            System.Data.SqlClient.SqlParameter p2 = new System.Data.SqlClient.SqlParameter("@startdate", startdate.GetString());
            System.Data.SqlClient.SqlParameter p3 = new System.Data.SqlClient.SqlParameter("@enddate", enddate.GetString());
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn2);
            DataTable data = dbHelper.GetDataTable(sql, "StallMastList", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1, p2, p3 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.StallSales.StallSalesMast()
            {
                CounterNo = dr.Field<string>("CounterNo").GetString(),
                CounterName = dr.Field<string>("CounterName").GetString(),
                GrossAmt = dr.Field<Decimal>("GrossAmt").GetString(),
                DiscountAmt = dr.Field<Decimal>("DiscountAmt").GetString(),
                SalesAmt = dr.Field<Decimal>("SalesAmt").GetString(),
                SalesQty = dr.Field<Decimal>("SalesQty").GetString(),
                TransactionQty = dr.Field<Decimal>("TransactionQty").GetString(),
                DealQty = dr.Field<Int32>("DealQty").GetString()

            }).ToList();
        }
        public List<Model.StallSales.StallSalesEntity> GetSallEntity(string language)
        {
            string sql = "select '(All)' as CounterNo,''as CounterName union select CounterNo,CounterName from [dbo].[tbl_CounterInfo]";
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn2);
            DataTable data = dbHelper.GetDataTable(sql, "StallEntity");
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.StallSales.StallSalesEntity()
            {
                CounterNo = dr.Field<string>("CounterNo").GetString(),
                CounterName = dr.Field<string>("CounterName").GetString()
            }).ToList();
        }

        #endregion

        #region SiteTimeSet
        public Model.Table.TableMast<Model.Calendar.CalendarMast> GetTableSiteTimeSetMastList(string keyWords, string language)
        {
            Model.Table.TableMast<Model.Calendar.CalendarMast> tableMast = new Model.Table.TableMast<Model.Calendar.CalendarMast>();
            tableMast.draw = 1;
            tableMast.data = GetSiteTimeSetList(keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.Calendar.CalendarMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.Calendar.CalendarMast> GetSiteTimeSetList(string keyWords, string language)
        {
            string sql = "select a2.CompNameCn,a2.CompNameEn,a1.* from tblCalendars a1 left join tblSite a2 "
                + "on a1.SiteGUID = a2.GUID where "
                + "isnull(a1.startdate, a1.enddate) is null and isnull(a1.starttime, a1.endtime) is not null and a1.SiteGUID=@search";
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", keyWords.GetString());
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = dbHelper.GetDataTable(sql, "CalendarMast", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Calendar.CalendarMast()
            {
                ID = dr.Field<int>("ID"),
                compNameCn=dr.Field<string>("CompNameCn").GetString(),
                compNameEn=dr.Field<string>("CompNameEn").GetString(),
                SiteGuid = dr.Field<string>("SiteGUID").GetString(),
                startTime = dr.Field<DateTime?>("StartTime").ToFormatDate("HH:mm:ss", "Min"),
                endTime = dr.Field<DateTime?>("EndTime").ToFormatDate("HH:mm:ss", "Max"),
                Working = dr.Field<bool?>("Working").BoolToInt()
            }).ToList();
        }

        public Model.Calendar.CalendarMast GetSiteTimeSetMast(int calendarID,string siteGUID)
        {
            string sql = "select a1.Name,a1.BUGUID,a2.Code as BUName,a1.SiteGUID,a3.Code as SiteCode,a1.StartDate,a1.EndDate, "
                + "isnull(a1.StartTime,null) StartTime,isnull(a1.EndTime,null) EndTime, "
                + "a1.Working from [dbo].[tblCalendars] a1 left join [dbo].[tblBU] a2 on a1.BUGUID=a2.BUGUID "
                + "left join [dbo].[tblSite] a3 on a1.SiteGUID=a3.GUID where a1.SiteGUID='"+siteGUID+"' or a1.ID=" + calendarID;
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null) return null; //|| data.Rows.Count != 1
            return new Model.Calendar.CalendarMast()
            {
                ID = calendarID,
                Name = data.Rows[0].Field<string>("Name").GetString(),
                BUGuid = data.Rows[0].Field<string>("BUGUID").GetString(),
                BUName = data.Rows[0].Field<string>("BUName").GetString(),
                SiteGuid = data.Rows[0].Field<string>("SiteGuid").GetString(),
                SiteCode = data.Rows[0].Field<string>("SiteCode").GetString(),
                StartDate = data.Rows[0].Field<DateTime?>("StartDate").ToFormatDate("yyyy-MM-dd", "Min"),
                EndDate = data.Rows[0].Field<DateTime?>("EndDate").ToFormatDate("yyyy-MM-dd", "Max"),
                startTime = data.Rows[0].Field<DateTime?>("StartTime").ToFormatDate("HH:mm", "Min"),
                endTime = data.Rows[0].Field<DateTime?>("EndTime").ToFormatDate("HH:mm", "Min"),
                Working = data.Rows[0].Field<bool?>("Working").BoolToInt()
            };
        }



        public Model.Common.BaseResponse ModifySiteTime(Model.Calendar.CalendarMast calendarMast)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (calendarMast == null) throw new Exception("Nothing to process.");
                int count = 0;
                if (calendarMast.IsDel.Equals(1) && !calendarMast.ID.Equals(0))
                {
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute("delete [dbo].[tblCalendars] where ID=" +
                        calendarMast.ID);
                }
                else
                {
                    if (DateTime.Parse(calendarMast.startTime) > DateTime.Parse(calendarMast.endTime))
                        throw new Exception("Date Range error");
                    string sql = string.Empty;
                    if (calendarMast.ID.Equals(0))
                        sql = "insert into [dbo].[tblCalendars](SiteGUID,StartTime,EndTime,Working) "
                            + "values(@siteguid,@stime,@etime,@working)";
                    else sql = "update [dbo].[tblCalendars] set SiteGUID=@siteguid,StartTime=@stime,"
                            + "EndTime=@etime,Working=@working where ID=" + calendarMast.ID;
                   
                    System.Data.SqlClient.SqlParameter siteguid = new System.Data.SqlClient.SqlParameter("@siteguid", SqlDbType.Char, 36);
                    
                       if (string.IsNullOrWhiteSpace(calendarMast.SiteGuid)) siteguid.Value = DBNull.Value;
                        else siteguid.Value = calendarMast.SiteGuid;
                    
                    System.Data.SqlClient.SqlParameter stime = new System.Data.SqlClient.SqlParameter("@stime", SqlDbType.DateTime, 23);
                    stime.Value = DateTime.Parse(calendarMast.startTime);
                    System.Data.SqlClient.SqlParameter etime = new System.Data.SqlClient.SqlParameter("@etime", SqlDbType.DateTime, 23);
                    etime.Value = DateTime.Parse(calendarMast.endTime);
                    System.Data.SqlClient.SqlParameter working = new System.Data.SqlClient.SqlParameter("@working", SqlDbType.Bit, 1);
                    working.Value = calendarMast.Working;

                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                              new System.Data.SqlClient.SqlParameter[] { siteguid, stime, etime, working });
                }
                if (count > 0)
                {
                    resp.Status = "ok";
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
        #endregion

        public List<Model.SPD.LogEntity> GetEmpCode(string userKey)
        {
            string sql = "select distinct a1.empCode,b1.Code siteCode,a2.UserName from tblSiteAdmin a2 left join tblSite b1 on a2.SiteGUID=b1.GUID "
                      + "left join tblManagerSite a1 on a1.EmpName = a2.UserName where a2.id = '" + userKey + "'";
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = helper.GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.SPD.LogEntity()
            {
                empCode = dr.Field<string>("empCode")==null?"":dr.Field<string>("empCode").GetString(),
                empName = dr.Field<string>("UserName").GetString(),
                siteCode = dr.Field<string>("siteCode") ==null?"":dr.Field<string>("siteCode").GetString(),
            }).ToList();
        }

        public List<Model.Site.SiteMast> GetSite(string userKey)
        {
            string empCode = GetEmpCode(userKey).First().empCode.ToString();
            string siteCode= GetEmpCode(userKey).First().siteCode.ToString();
            string filter = "";
            string filterSite = "";
            if (!string.IsNullOrWhiteSpace(empCode))
            {
                filter = string.Format("where a8.EmpCode='{0}'", empCode);
                filterSite = " '' as Code,'(All)' as Name union select ";
            }
            else if (string.IsNullOrWhiteSpace(empCode) && !string.IsNullOrWhiteSpace(siteCode))
            {
                filter = string.Format("where a22.Code='{0}'", siteCode);
                
            }

            string sql = "select distinct "+filterSite+" a1.Code as Code,a1.Code as Name from tblSite a22 left join tblManagerSite a8 "
                + "on isnull(a8.BUGUID,'')=isnull(a22.BUGUID,'') left join tblsite a1 on isnull(a8.SiteGUID,a22.GUID)=a1.GUID "
                + filter;
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = helper.GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.Site.SiteMast
            {
                SiteCode = dr.Field<string>("Code").GetString(),
                SiteName = dr.Field<string>("Name").GetString()
            }).ToList();
        }

        #region SPD Report
        public Model.Table.TableMast<Model.SPD.SPDMast> GetSPD(string empCode, string startdate, string enddate, string all, string group, string getsite, string language)
        {
            Model.Table.TableMast<Model.SPD.SPDMast> tableMast = new Model.Table.TableMast<Model.SPD.SPDMast>();
            tableMast.draw = 1;
            tableMast.data = GetSPDReport(empCode, startdate, enddate, all, group, getsite, language);
            if (tableMast.data == null) tableMast.data = new List<Model.SPD.SPDMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public List<Model.SPD.SPDMast> GetSPDReport(string empCode, string startdate, string enddate, string all, string group, string getsite, string language)
        {

            string filter = "";
            if (!string.IsNullOrWhiteSpace(empCode)) filter = string.Format(" and a8.EmpCode='{0}' ", empCode);
            else if(string.IsNullOrWhiteSpace(empCode) && !string.IsNullOrWhiteSpace(getsite)) filter = string.Format(" and a22.Code='{0}' ", getsite);
            string sql = "select " + all + " from (select a1.code,convert(varchar(4),year(a2.RequiredDate)) as Year,convert(varchar(4), year(a2.RequiredDate)) + '/' + convert(varchar(2), month(a2.RequiredDate)) as Month, "
                    + "convert(varchar(10), a2.RequiredDate, 23) as Date,convert(varchar(20), a2.RequiredDate, 25) as Time,a11.CompNameCn as Supplier, a5.FirstName as Consumer, "
                    + "a4.ItemCode, a4.ItemName,convert(decimal(18,2),sum(a3.Qty)) as Qty,sum(a3.Qty * a3.Price) as Turnover,sum(a3.Qty * a3.Price) - sum(a9.CouponPerLine) as NetTurnover, "
                    + "-(sum(a6.Amt) + sum(a4.OtherCost * a3.Qty)) as Cost,sum(a3.Qty * a3.Price) - sum(a6.Amt) - sum(a4.OtherCost) * sum(a3.Qty) as GM, "
                    + "sum(a3.Qty * a3.Price) - sum(a6.Amt) - sum(a4.OtherCost) * sum(a3.Qty) - sum(a9.CouponPerLine) as NetGM from tblManagerSite a8 "
                    + "left join tblSite a22 on isnull(a8.BUGUID, '') = isnull(a22.BUGUID, '') left join tblsite a1 on isnull(a8.SiteGUID, a22.GUID) = a1.GUID "
                    + "left join tblSupplierSite a10 on a10.SiteGUID = a1.GUID left join tblSupplier a11 on a11.SupplierGUID = a10.SupplierGUID, tblsaleorder a2, "
                    + "tblsaleorderitem a3,tblitem a4,tbluser a5,(select c2.SODetailGUID, cast(sum(c1.Price * c2.UsedQty) as decimal(10, 2)) as Amt "
                    + "from tblMRPOrderItem c1 join tblMRPSODetail c2 on c1.GUID = c2.MRPLineGUID group by c2.SODetailGUID) a6,(select b1.SiteGUID,count(b1.WechatID) as Total,(count(b2.WechatID) - count(b3.WechatID)) as ExtraLW from tbluser b1 "
                    + "left join tbluser b2 on b1.WechatID = b2.WechatID and datediff(week, convert(varchar(10), b2.createdate, 23), getdate()) = 1 "
                    + "left join tbluser b3 on b1.WechatID = b3.WechatID and datediff(week, convert(varchar(10), b2.createdate, 23), getdate()) = 0 "
                    + "group by b1.SiteGUID) a7,(select a1.OrderCode,sum(a1.CouponAmount) / sum(a2.Qty) as Coupon,sum(a1.CouponAmount) / sum(a2.Qty) / sum(a2.Qty) as CouponPerLine "
                    + "from tblSaleOrder a1 join tblSaleOrderitem a2 on a1.GUID = a2.SOGUID group by a1.OrderCode) a9 where a2.IsPaid = 1 and a2.SiteGUID = a1.GUID and "
                    + "a3.SOGUID = a2.GUID and a4.GUID = a3.ItemGUID and a2.UserID = a5.UserID and a3.GUID = a6.SODetailGUID and a7.SiteGUID = a1.GUID and a9.OrderCode = a2.OrderCode and "
                    + "convert(varchar(10), a2.RequiredDate, 23) >= dateadd(day, 0, '" + DateTime.Parse(startdate).ToString("yyyy-MM-dd") + "') and convert(varchar(10), a2.RequiredDate, 23) <= dateadd(day, 0, '" + DateTime.Parse(enddate).ToString("yyyy-MM-dd") + "') " +filter 
                    + "group by a8.EmpCode,a1.Code,convert(varchar(10), a2.RequiredDate, 23),a5.FirstName,a4.ItemName,a4.ItemCode,a11.CompNameCn,a2.RequiredDate "
                    + "union select a1.code,convert(varchar(4), year(a2.RequiredDate)) as Year,convert(varchar(4), year(a2.RequiredDate)) + '/' + convert(varchar(2), month(a2.RequiredDate)) as Month, "
                    + "convert(varchar(10), a2.RequiredDate, 23) as Date,convert(varchar(20), a2.RequiredDate, 25) as Time,a11.CompNameCn as Supplier,a5.FirstName as Consumer, a4.ItemCode, a4.ItemName,convert(decimal(18, 2), sum(a3.Qty)) as Qty,sum(a3.Qty * a3.Price) as Turnover, "
                    + "sum(a3.Qty * a3.Price) - sum(a9.CouponPerLine) as NetTurnover, -(sum(a4.OtherCost * a3.Qty)) as Cost,sum(a3.Qty * a3.Price) - sum(a4.OtherCost) * sum(a3.Qty) as GM,sum(a3.Qty * a3.Price) - sum(a4.OtherCost) * sum(a3.Qty) - sum(a9.CouponPerLine) as NetGM from tblManagerSite a8 "
                    + "left join tblSite a22 on isnull(a8.BUGUID, '') = isnull(a22.BUGUID, '') left join tblsite a1 on isnull(a8.SiteGUID, a22.GUID) = a1.GUID left join tblSupplierSite a10 on a10.SiteGUID = a1.GUID left join tblSupplier a11 on a11.SupplierGUID = a10.SupplierGUID, "
                    + "tblsaleorder a2, tblsaleorderitem a3,tblitem a4, tbluser a5,(select b1.SiteGUID,count(b1.WechatID) as Total,(count(b2.WechatID) - count(b3.WechatID)) as ExtraLW from tbluser b1 left join tbluser b2 "
                    + "on b1.WechatID = b2.WechatID and datediff(week, convert(varchar(10), b2.createdate, 23), getdate()) = 1 "
                    + "left join tbluser b3 on b1.WechatID = b3.WechatID and datediff(week, convert(varchar(10), b2.createdate, 23), getdate()) = 0 "
                    + "group by b1.SiteGUID) a7,(select a1.OrderCode,sum(a1.CouponAmount) / sum(a2.Qty) as Coupon,sum(a1.CouponAmount) / sum(a2.Qty) / sum(a2.Qty) as CouponPerLine from tblSaleOrder a1 join tblSaleOrderitem a2 on a1.GUID = a2.SOGUID "
                    + "group by a1.OrderCode) a9 where a2.IsPaid = 1 and a2.SiteGUID = a1.GUID and a3.SOGUID = a2.GUID and a4.GUID = a3.ItemGUID and a2.UserID = a5.UserID and a7.SiteGUID = a1.GUID and a9.OrderCode = a2.OrderCode "
                    + "and convert(varchar(10), a2.RequiredDate, 23) >= dateadd(day, 0, '" + DateTime.Parse(startdate).ToString("yyyy-MM-dd") + "') and convert(varchar(10), a2.RequiredDate, 23) <= dateadd(day, 0, '" + DateTime.Parse(enddate).ToString("yyyy-MM-dd") + "') "+filter
                    + "group by a8.EmpCode,a1.Code,convert(varchar(10), a2.RequiredDate, 23),a5.FirstName,a4.ItemName,a4.ItemCode,a11.CompNameCn,a2.RequiredDate "
                    + ") H where H.code like'%" + getsite + "%'" + group;
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = helper.GetDataTable(sql);
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.SPD.SPDMast()
            {
                code = dr.Field<string>("code").GetString(),
                Year = dr.Field<string>("Year").GetString(),
                Month = dr.Field<string>("Month").GetString(),
                Date = dr.Field<string>("Date").GetString(),
                Supplier = dr.Field<string>("Supplier").GetString(),
                Consumer = dr.Field<string>("Consumer").GetString(),
                ItemCode = dr.Field<string>("ItemCode").GetString(),
                ItemName = dr.Field<string>("ItemName").GetString(),
                Qty = dr.Field<decimal?>("Qty").GetString(),
                Turnover = dr.Field<decimal?>("Turnover").GetString(),
                NetTurnover = dr.Field<decimal?>("NetTurnover").GetString(),
                Cost = dr.Field<decimal?>("Cost").GetString(),
                GM = dr.Field<decimal?>("GM").GetString(),
                NetGM = dr.Field<decimal?>("NetGM").GetString(),
                Sql = sql
            }).ToList();
        }

        #endregion
    }
}
