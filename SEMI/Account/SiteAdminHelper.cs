using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Account
{
    public class SiteAdminHelper: Common.BaseDataHelper
    {
        private static SiteAdminHelper instance = new SiteAdminHelper();

        private SiteAdminHelper() { }
        public static SiteAdminHelper GetInstance() { return instance; }

        /// <summary>
        /// 根据登录用户生成加密后的用户唯一标识
        /// </summary>
        /// <param name="userID">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>加密后的标识</returns>
        public string GetEncyptUserKey(string userID, string password)
        {
            try
            {
                string sql = "select top 1 ID from [dbo].[tblSiteAdmin] where UserName=@userId and Password=@password";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@userId",SqlDbType.VarChar, 32);
                p1.Value = userID.GetString();
                System.Data.SqlClient.SqlParameter p2 = new System.Data.SqlClient.SqlParameter("@password", SqlDbType.VarChar, 32);
                p2.Value = password.GetString();
                System.Data.SqlClient.SqlParameter[] parameters = new System.Data.SqlClient.SqlParameter[] { p1, p2 };
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                string key = helper.GetDataScalar(sql, CommandType.Text, parameters).GetString();
                if (string.IsNullOrWhiteSpace(key)) return string.Empty;
                else return EncyptHelper.Encypt(key);
            }
            catch { return string.Empty; }   
        }

        /// <summary>
        /// 获取用户主数据
        /// </summary>
        /// <param name="encyptID">加密的用户ID</param>
        /// <param name="language">语言</param>
        /// <returns>单行数据</returns>
        public Model.Account.LoginUserMast GetUserMastData(string encyptID, string language)
        {
            try
            {
                string sql = "select top 1 t1.UserName,t2.Code as SiteCode,case when 'zh'='" + language 
                    + "' then CompnameCn else t2.CompnameEn end SiteName,t1.SiteGUID,t2.BUGUID as SiteBUGUID,"
                    + "t1.BUGUID,t3.Code as BUName,t3.ERPCode as BUCode "
                    + "from [dbo].[tblSiteAdmin] t1 left join [dbo].[tblSite] t2 "
                    + "on t1.SiteGUID=t2.GUID left join [dbo].[tblBU] t3 on t1.BUGUID=t3.BUGUID "
                    + "where t1.ID='" + EncyptHelper.DesEncypt(encyptID) + "'";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = helper.GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return null;
                return new Model.Account.LoginUserMast()
                {
                    UserName = data.Rows[0].Field<string>("UserName").GetString(),
                    SiteCode = data.Rows[0].Field<string>("SiteCode").GetString(),
                    SiteName = data.Rows[0].Field<string>("SiteName").GetString(),
                    SiteGuid = data.Rows[0].Field<string>("SiteGUID").GetString(),
                    SiteBUGuid = data.Rows[0].Field<string>("SiteBUGUID").GetString(),
                    BUGuid = data.Rows[0].Field<string>("BUGUID").GetString(),
                    BUCode = data.Rows[0].Field<string>("BUCode").GetString(),
                    BUName = data.Rows[0].Field<string>("BUName").GetString()
                };
            }
            catch { return null; }
        }

        public List<Model.Menu.MenuMast> GetUserMenuMastList(string encyptID, string language)
        {
            try
            {
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                string sqlUserMenu = "select MenuGUID,Permission from [dbo].[tblAdminMenu] where status=1 and UserID='"
                    + EncyptHelper.DesEncypt(encyptID) + "'";
                DataTable dtUserMenu = helper.GetDataTable(sqlUserMenu);
                if (dtUserMenu == null || dtUserMenu.Rows.Count == 0) return null;
                #region 根据权限MenuGUID获取父级,子级
                string sqlMenu = "with tb1(MenuParentGUID,MenuChildGUID,Sort,MenuName,MenuAction,MenuIconClassName) "
                    + "as (select MenuParentGUID,MenuGUID as MenuChildGUID,Sort,case when 'zh'='{0}' then "
                    + "MenuNameCn else MenuNameEn end as MenuName,MenuAction,MenuIconClassName from tblMenu "
                    + "where MenuType='Web' and Status=1 and MenuGUID in ({1}) union all select m1.MenuParentGUID,m1.MenuGUID,"
                    + "m1.Sort,case when 'zh'='{0}' then m1.MenuNameCn else m1.MenuNameEn end,m1.MenuAction,"
                    + "m1.MenuIconClassName from tblMenu m1 join tb1 on m1.MenuGUID=tb1.MenuParentGUID),"
                    + "tb2(MenuParentGUID,MenuChildGUID,Sort,MenuName,MenuAction,MenuIconClassName) "
                    + "as (select MenuParentGUID,MenuGUID as MenuChildGUID,Sort,case when 'zh'='{0}' then "
                    + "MenuNameCn else MenuNameEn end as MenuName,MenuAction,MenuIconClassName from tblMenu "
                    + "where Status=1 and MenuType='Web' and MenuGUID in ({1}) union all select m1.MenuParentGUID,m1.MenuGUID,"
                    + "m1.Sort,case when 'zh'='{0}' then m1.MenuNameCn else m1.MenuNameEn end,m1.MenuAction,"
                    + "m1.MenuIconClassName from tblMenu m1 join tb2 on m1.MenuParentGUID=tb2.MenuChildGUID where m1.status=1 and m1.MenuType='Web') "  //无效的子项 by Steve 2017-1-4
                    + "select distinct MenuParentGUID,MenuChildGUID,Sort,MenuName,MenuAction,MenuIconClassName from tb1 "
                    + "union select distinct MenuParentGUID,MenuChildGUID,Sort,MenuName,MenuAction,MenuIconClassName from tb2 "
                    + "order by Sort";
                #endregion
                DataTable data = helper.GetDataTable(string.Format(sqlMenu, language, string.Join(",",
                    dtUserMenu.AsEnumerable().Select(dr => "'" + dr.Field<string>("MenuGUID").GetString() + "'").ToArray())));
                if (data == null || data.Rows.Count == 0) return null;
                //获取最外层菜单
                var topMenus = (from dr1 in data.AsEnumerable()
                                join dr2 in data.AsEnumerable()
                                on dr1.Field<string>("MenuParentGUID").GetString()
                                equals dr2.Field<string>("MenuChildGUID").GetString() into leftData
                                from dr in leftData.DefaultIfEmpty()
                                where dr == null
                                select new
                                {
                                    MenuGUID = dr1.Field<string>("MenuChildGUID").GetString(),
                                    MenuName = dr1.Field<string>("MenuName").GetString(),
                                    MenuAction = dr1.Field<string>("MenuAction").GetString(),
                                    MenuIcon = dr1.Field<string>("MenuIconClassName").GetString()
                                }).ToList();
                List<Model.Menu.MenuMast> menuMastList = new List<Model.Menu.MenuMast>();
                //循环最外层,将所有子层添加
                foreach (var topMenu in topMenus)
                {
                    Model.Menu.MenuMast menu = new Model.Menu.MenuMast();
                    menu.MenuAction = topMenu.MenuAction;
                    menu.MenuName = topMenu.MenuName;
                    menu.MenuIconClassName = topMenu.MenuIcon;
                    //menu.MenuPermission = "";
                    menu.ChildMenus = GetChildMenuMast(topMenu.MenuGUID, data,dtUserMenu);
                    menuMastList.Add(menu);
                }
                return menuMastList;
            }
            catch { return null; }
        }

        private List<Model.Menu.MenuMast> GetChildMenuMast(string parentGUID, DataTable data, DataTable permission)
        {
            try
            {
                var query = data.AsEnumerable().Where(dr => dr.Field<string>("MenuParentGUID").GetString().Equals(parentGUID));
                if (query.Any())
                {
                    return query.GroupJoin(permission.AsEnumerable(),u=>u.Field<string>("MenuChildGUID").GetString(),d=>d.Field<string>("MenuGUID").GetString(),
                        (u,d)=>new Model.Menu.MenuMast()
                        //query.Select(u=> new Model.Menu.MenuMast()
                    {
                        MenuIconClassName = u.Field<string>("MenuIconClassName").GetString(),
                        MenuName = u.Field<string>("MenuName").GetString(),
                        MenuAction = u.Field<string>("MenuAction").GetString(),
                        MenuPermission =  (d == null || d.Count()==0 ? "Full" : d.FirstOrDefault().Field<string>("Permission").GetString()),
                        ChildMenus = GetChildMenuMast(u.Field<string>("MenuChildGUID").GetString(), data,permission)
                    }).Select(q=>q).ToList();
                }
                else return null;
            }
            catch(Exception e) { return null; }
        }
    }
}
