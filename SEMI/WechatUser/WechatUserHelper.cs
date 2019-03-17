using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.WechatUser
{
    public class WechatUserHelper : Common.BaseDataHelper
    {
        private static WechatUserHelper instance = new WechatUserHelper();
        private WechatUserHelper() { }
        public static WechatUserHelper GetInstance() { return instance; }

        public Model.Table.TableMast<Model.WechatUser.WechatMenuList> GetTableWechatMenuList(string wechatID)
        {
            Model.Table.TableMast<Model.WechatUser.WechatMenuList> tableMast =
                new Model.Table.TableMast<Model.WechatUser.WechatMenuList>();
            tableMast.errMsg = "";
            tableMast.draw = 1;
            tableMast.data = GetWechatMenuList(wechatID);
            if (tableMast.data == null) tableMast.data = new List<Model.WechatUser.WechatMenuList>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }
        public dynamic GetWechatMenuList(string wechatId)
        {
            try
            {
                string sql = string.Format("select a3.id,a1.MenuNameCn,a1.MenuAction,a1.menuiconclassname,a1.menuiconimg," +
                    "a3.SiteGUID,ISNULL(a4.readshippmentfromdb,0) readshippmentfromdb "
                    +"from tblmenu (nolock)a1,tbladminmenu(nolock)  a2, tblsiteadmin(nolock)  a3,tblsite a4 "
                    + "where a1.menuguid = a2.menuguid and a2.userid = a3.ID and a3.siteguid=a4.guid and " +
                    "a1.menutype = 'Wechat' and a3.WechatId = '{0}' and (a1.siteneedwork = 0 or a4.NeedWork =1)", wechatId);
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "WechatUserMast");

                var list = data.AsEnumerable().Select(dr => new
                {
                    menuName = dr.Field<string>("menuNameCn").GetString(),
                    menuAction = dr.Field<string>("menuAction").GetString(),
                    menuIconClassName = dr.Field<string>("menuiconclassname").GetString(),
                    menuIconImg = dr.Field<string>("menuiconimg").GetString(),
                    readShippmentFromDB = dr.Field<bool>("readshippmentfromdb"),
                    userId = dr.Field<int>("id"),
                    siteGUID = dr.Field<string>("siteGUID")
                }).ToList();

                if (!list.Any()) throw new Exception("没有权限");

                return list.GroupBy(q => q.siteGUID).Select(q => new
                {
                    siteGuid = q.Key,
                    userId = q.FirstOrDefault().userId,
                    readShippmentFromDB = q.FirstOrDefault().readShippmentFromDB,
                    menus = q.Select(p => new { p.menuName, p.menuAction, p.menuIconClassName, p.menuIconImg })
                }).ToList() ;
            }
            catch(Exception e)
            { throw e; }
        }

        public bool CheckWechatLoginUser(string wechatId)
        {
            try
            {
                string sql = string.Format("select top 1 1 from tblSiteAdmin(nolock) where wechatId='{0}'",wechatId);
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return false;
                return true;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public bool BindWechatLoginUser(string wechatId,string username,string password)
        {
            try
            {
                if (CheckWechatLoginUser(wechatId)) throw new Exception("微信号已绑定");
                
                string sql = string.Format("update tblSiteAdmin set wechatId='{0}' " +
                    "where isnull(wechatId,'') ='' and lower(username)='{1}' and lower(password)='{2}'",
                    wechatId,username.ToLower(),password.ToLower());
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);

                return dbHelper.Execute(sql) == 1;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
