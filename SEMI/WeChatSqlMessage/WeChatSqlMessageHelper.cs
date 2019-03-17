using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.WeChatSqlMessage
{
    public class WeChatSqlMessageHelper : Common.BaseDataHelper
    {
        private static WeChatSqlMessageHelper instance = new WeChatSqlMessageHelper();

        private WeChatSqlMessageHelper() { }

        public static WeChatSqlMessageHelper GetInstance() { return instance; }

        public void SendMessage(string receiver, Model.WeChatSqlMessage.WeChatSqlMessageData queryData,
            List<Model.WeChatSqlMessage.WeChatSqlMessageData> linkQueryDatas, string parameters, int appId,
            string partyIds, Action<string, string> action)
        {
            try
            {
                StringBuilder message = new StringBuilder(GenerateMessage(queryData.ContentSQL, queryData.TitleSQL,
                    parameters, queryData.SpaceNumber));

                if (linkQueryDatas != null && linkQueryDatas.Count > 0)
                {
                    string linkUrl = ConfigurationManager.AppSettings["LinkedUrl"].GetString();
                    message.Append("\n\n点击下列链接,查看详情\n\n").Append(string.Join("\n\n", linkQueryDatas
                        .Select(qr => "<a href=\"" + linkUrl + "/SEMI/WeChatReport/?p=" + Utils.Common.EncyptHelper.Encypt(
                            qr.GUID + "/" + parameters) + "\">" + qr.LinkName + "</a>").ToArray()));
                }
                WeChat.Service.CorpSendMessageHelper.GetInstance().SendTextMessage(receiver, partyIds,
                    string.Empty, message.ToString(), appId, (res) => { action(res.errcode, res.errmsg); });
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("WeChatMessage", "微信发送失败", "查询GUID:" + queryData.GUID, e.Message); }
        }

        private string GenerateMessage(string contentSql, string titleSql, string parameters, int spaceNumber)
        {
            DataSet ds = GetSqlQueryDatas(contentSql, titleSql, parameters);
            DataTable contentData = ds.Tables["Content"];
            DataTable titleData = ds.Tables["Title"];
            if (contentData == null || contentData.Rows.Count != 1) throw new Exception("内容Sql查询不到数据");
            if (titleData == null || titleData.Rows.Count != 1) throw new Exception("标题Sql查询不到数据");
            if (!contentData.Columns.Count.Equals(titleData.Columns.Count)) throw new Exception("标题列数量与内容列数量不一致");
            List<string> replays = new List<string>(); //第一次生成的文本
            for (int i = 0; i < titleData.Columns.Count; i++)
            {
                if (titleData.Rows[0][i].GetString().Equals(string.Empty)) continue;
                replays.Add(titleData.Rows[0][i].ToString() + " ".StringReplete(spaceNumber) + contentData.Rows[0][i].ToString());
            }
                
            return string.Join("\n", replays.ToArray());
        }

        private DataSet GetSqlQueryDatas(string contentSql, string titleSql, string parameters)
        {
            List<Utils.Database.SqlServer.DBQueryDic> sqlDics = new List<Utils.Database.SqlServer.DBQueryDic>();
            sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
            {
                Sql = string.Format(contentSql, parameters.Split(',', ';')),
                TableName = "Content"
            });
            sqlDics.Add(new Utils.Database.SqlServer.DBQueryDic()
            {
                Sql = string.Format(titleSql, parameters.Split(',', ';')),
                TableName = "Title"
            });
            return new Utils.Database.SqlServer.DBHelper(_conn).GetDataSet(sqlDics);
        }

        public Model.WeChatSqlMessage.WeChatReportDisplayData GetWeChatDisplayReportData(string reportGuid, string parameters)
        {
            try
            {
                string sql = "select GUID,ContentSQL,TitleSQL,ParentGUID,DisplayType,LinkField "
                    + "from [dbo].[tblWeChatReportSQL] where Status=1";
                DataTable rptData = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (rptData == null || rptData.Rows.Count == 0) return null;
                DataRow drThis = rptData.AsEnumerable().Where(dr => dr.Field<string>("GUID").GetString().Equals(reportGuid)).First();
                string contentSql = drThis.Field<string>("ContentSQL").GetString();
                if (string.IsNullOrWhiteSpace(contentSql)) return null;
                string titleSql = drThis.Field<string>("TitleSQL").GetString();
                if (string.IsNullOrWhiteSpace(titleSql)) return null;
                DataSet ds = GetSqlQueryDatas(contentSql, titleSql, parameters);
                if (ds == null || ds.Tables["Content"] == null || ds.Tables["Content"].Rows.Count == 0) return null;
                if (ds.Tables["Title"] == null || ds.Tables["Title"].Rows.Count != 1) return null;
                List<string> titleList = new List<string>();
                foreach (DataColumn column in ds.Tables["Title"].Columns)
                    titleList.Add(ds.Tables["Title"].Rows[0][column].ToString());
                Model.WeChatSqlMessage.WeChatReportDisplayData returnData = new Model.WeChatSqlMessage.WeChatReportDisplayData();
                returnData.Data = ds.Tables["Content"];
                returnData.TitleList = titleList;
                returnData.Type = drThis.Field<string>("DisplayType").GetString();
                returnData.LinkedField = drThis.Field<string>("LinkField").GetString();
                var q = rptData.AsEnumerable().Where(dr => dr.Field<string>("ParentGUID").GetString().Equals(reportGuid));
                if (q.Any()) returnData.ChildGuid = q.First().Field<string>("GUID").GetString();
                return returnData;
            }
            catch { return null; }
        }

        public Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageData> GetTableWeChatReportSqlMastList(int status,
            string keyWords, string language)
        {
            Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageData> tableMast =
                new Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageData>();
            tableMast.draw = 1;
            tableMast.data = GetWeChatReportSqlMastList(status, keyWords, string.Empty, language);
            if (tableMast.data == null) tableMast.data = new List<Model.WeChatSqlMessage.WeChatSqlMessageData>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.WeChatSqlMessage.WeChatSqlMessageData> GetWeChatReportSqlMastList(int status,
            string keyWords, string exceptGuid, string language)
        {
            try
            {
                string sql = "select Name,GUID,ParentGUID,DisplayType,LinkName,LinkField,SpaceNumber from [dbo].[tblWeChatReportSQL] "
                    + "where Status=" + status + (string.IsNullOrWhiteSpace(exceptGuid) ? "" : " and GUID<>'" + exceptGuid + "'")
                    + " and Name like @search order by ID";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "WeChatReportSqlMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return (from d1 in data.AsEnumerable()
                        join d2 in data.AsEnumerable()
                        on d1.Field<string>("ParentGUID").GetString() equals d2.Field<string>("GUID").GetString() into ldata
                        from ld in ldata.DefaultIfEmpty()
                        select new Model.WeChatSqlMessage.WeChatSqlMessageData()
                        {
                            GUID = d1.Field<string>("GUID").GetString(),
                            ParentName = ld == null ? string.Empty : ld.Field<string>("Name").GetString(),
                            DisplayType = d1.Field<string>("DisplayType").GetString(),
                            LinkName = d1.Field<string>("LinkName").GetString(),
                            LinkField = d1.Field<string>("LinkField").GetString(),
                            SpaceNumber = d1.Field<int?>("SpaceNumber").ToInt(),
                            Name = d1.Field<string>("Name").GetString()
                        }).ToList();
            }
            catch { return null; }
        }

        public Model.WeChatSqlMessage.WeChatSqlMessageData GetWeChatSqlMessageData(string GUID)
        {
            try
            {
                string sql = "select a1.Name,a1.ParentGUID,a1.ContentSQL,a1.TitleSQL,a1.LinkName,a1.LinkField,a1.SpaceNumber,"
                    + "a1.Status,a2.Name as ParentName,a1.DisplayType from [dbo].[tblWeChatReportSQL] a1 left join "
                    + "[dbo].[tblWeChatReportSQL] a2 on a1.ParentGUID=a2.GUID where a1.GUID='" + GUID + "'";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count != 1) return null;
                return new Model.WeChatSqlMessage.WeChatSqlMessageData()
                {
                    GUID = GUID,
                    ContentSQL = data.Rows[0].Field<string>("ContentSQL").GetString(),
                    TitleSQL = data.Rows[0].Field<string>("TitleSQL").GetString(),
                    LinkName = data.Rows[0].Field<string>("LinkName").GetString(),
                    LinkField = data.Rows[0].Field<string>("LinkField").GetString(),
                    SpaceNumber = data.Rows[0].Field<int?>("SpaceNumber").ToInt(),
                    DisplayType = data.Rows[0].Field<string>("DisplayType").GetString(),
                    Name = data.Rows[0].Field<string>("Name").GetString(),
                    ParentName = data.Rows[0].Field<string>("ParentName").GetString(),
                    ParentGUID = data.Rows[0].Field<string>("ParentGUID").GetString(),
                    Status = data.Rows[0].Field<bool?>("Status").BoolToInt()
                };
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse ModifyWeChatSql(Model.WeChatSqlMessage.WeChatSqlMessageData data, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (data == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                int count = 0;
                int isDel = data.IsDel.ToInt();
                if (isDel.Equals(1))
                {
                    if (!string.IsNullOrWhiteSpace(data.GUID))
                        sql = "delete [dbo].[tblWeChatReportSQL] where GUID='" + data.GUID + "'";
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(data.GUID))
                    {
                        sql = "insert into [dbo].[tblWeChatReportSQL](GUID,Name,ContentSQL,TitleSQL,ParentGUID,DisplayType,"
                            + "LinkName,LinkField,SpaceNumber,Status) values(newid(),@name,@csql,@tsql,@pguid,@distype,@lname,"
                            + "@lfield,@spcnum,@status);";
                    }
                    else
                    {
                        sql = "update [dbo].[tblWeChatReportSQL] set Name=@name,ContentSQL=@csql,TitleSQL=@tsql,ParentGUID=@pguid,"
                            + "DisplayType=@distype,LinkName=@lname,LinkField=@lfield,SpaceNumber=@spcnum,Status=@status "
                            + "where GUID='" + data.GUID + "';";
                    }
                    System.Data.SqlClient.SqlParameter name = new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 60);
                    name.Value = data.Name.GetString();
                    System.Data.SqlClient.SqlParameter csql = new System.Data.SqlClient.SqlParameter("@csql", SqlDbType.VarChar, 5000);
                    csql.Value = data.ContentSQL.GetString();
                    System.Data.SqlClient.SqlParameter tsql = new System.Data.SqlClient.SqlParameter("@tsql", SqlDbType.VarChar, 1000);
                    tsql.Value = data.TitleSQL.GetString();
                    System.Data.SqlClient.SqlParameter pguid = new System.Data.SqlClient.SqlParameter("@pguid", SqlDbType.Char, 36);
                    if (string.IsNullOrWhiteSpace(data.ParentGUID)) pguid.Value = DBNull.Value;
                    else pguid.Value = data.ParentGUID;
                    System.Data.SqlClient.SqlParameter status = new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                    status.Value = data.Status;
                    System.Data.SqlClient.SqlParameter distype = new System.Data.SqlClient.SqlParameter("@distype", SqlDbType.VarChar, 20);
                    distype.Value = data.DisplayType.GetString();
                    System.Data.SqlClient.SqlParameter lname = new System.Data.SqlClient.SqlParameter("@lname", SqlDbType.VarChar, 24);
                    lname.Value = data.LinkName.GetString();
                    System.Data.SqlClient.SqlParameter lfield = new System.Data.SqlClient.SqlParameter("@lfield", SqlDbType.VarChar, 20);
                    lfield.Value = data.LinkField.GetString();
                    System.Data.SqlClient.SqlParameter spcnum = new System.Data.SqlClient.SqlParameter("@spcnum", SqlDbType.Int, 5);
                    spcnum.Value = data.SpaceNumber;
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                              new System.Data.SqlClient.SqlParameter[] { name, csql, tsql, pguid, status, distype, lname, lfield, spcnum });
                }
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = isDel.Equals(1) ? "" : data.Name;
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

        public Model.Common.BaseResponse ModifyWeChatJob(Model.WeChatSqlMessage.WeChatSqlMessageJob data, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (data == null) throw new Exception("Nothing to process.");
                string sql = string.Empty;
                int count = 0;
                int isDel = data.IsDel.ToInt();
                if (isDel.Equals(1))
                {
                    if (!data.ID.Equals(0))
                        sql = "delete [dbo].[tblWeChatReportJob] where ID='" + data.ID + "'";
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql);
                }
                else
                {
                    if (data.ID.Equals(0))
                    {
                        sql = "insert into [dbo].[tblWeChatReportJob](JobName,SQLGUID,DataSql,KeyFields,ParameterFields,"
                            + "EmployeeIDField,RunWeek,DailyStartTime,Status) values(@jobname,@sqlguid,@datasql,@kfields,"
                            + "@pfields,@efield,@runweek,@dstime,@status)";
                    }
                    else
                    {
                        sql = "update [dbo].[tblWeChatReportJob] set JobName=@jobname,SQLGUID=@sqlguid,DataSql=@datasql,"
                            + "KeyFields=@kfields,ParameterFields=@pfields,EmployeeIDField=@efield,RunWeek=@runweek,"
                            + "DailyStartTime=@dstime,Status=@status where ID=" + data.ID;
                    }
                    System.Data.SqlClient.SqlParameter jobname = new System.Data.SqlClient.SqlParameter("@jobname", SqlDbType.VarChar, 50);
                    jobname.Value = data.JobName.GetString();
                    System.Data.SqlClient.SqlParameter datasql = new System.Data.SqlClient.SqlParameter("@datasql", SqlDbType.VarChar, 5000);
                    datasql.Value = data.DataSQL.GetString();
                    System.Data.SqlClient.SqlParameter sqlguid = new System.Data.SqlClient.SqlParameter("@sqlguid", SqlDbType.Char, 36);
                    if (string.IsNullOrWhiteSpace(data.SQLGUID)) sqlguid.Value = DBNull.Value;
                    else sqlguid.Value = data.SQLGUID;
                    System.Data.SqlClient.SqlParameter status = new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                    status.Value = data.Status;
                    System.Data.SqlClient.SqlParameter kfields = new System.Data.SqlClient.SqlParameter("@kfields", SqlDbType.VarChar, 200);
                    kfields.Value = data.KeyFields.GetString();
                    System.Data.SqlClient.SqlParameter pfields = new System.Data.SqlClient.SqlParameter("@pfields", SqlDbType.VarChar, 200);
                    pfields.Value = data.ParameterFields.GetString();
                    System.Data.SqlClient.SqlParameter efield = new System.Data.SqlClient.SqlParameter("@efield", SqlDbType.VarChar, 20);
                    efield.Value = data.EmployeeIDField.GetString();
                    System.Data.SqlClient.SqlParameter runweek = new System.Data.SqlClient.SqlParameter("@runweek", SqlDbType.Int, 5);
                    runweek.Value = data.RunWeek;
                    System.Data.SqlClient.SqlParameter dstime = new System.Data.SqlClient.SqlParameter("@dstime", SqlDbType.Char, 5);
                    dstime.Value = data.DailyStartTime.GetString();
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                              new System.Data.SqlClient.SqlParameter[] { jobname, sqlguid, datasql, status, kfields, pfields, efield, runweek, dstime });
                }
                if (count > 0)
                {
                    resp.Status = "ok";
                    resp.Msg = isDel.Equals(1) ? "" : data.JobName;
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

        public Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageJob> GetTableWeChatReportJobMastList(int status,
        string keyWords, string language)
        {
            Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageJob> tableMast =
                new Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageJob>();
            tableMast.draw = 1;
            tableMast.data = GetWeChatReportJobMastList(status, keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.WeChatSqlMessage.WeChatSqlMessageJob>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.WeChatSqlMessage.WeChatSqlMessageJob> GetWeChatReportJobMastList(int status,
            string keyWords, string language)
        {
            try
            {
                string sql = "select a1.ID,a1.JobName,a2.Name,a1.RunWeek,a1.DailyStartTime,a1.KeyFields,a1.ParameterFields "
                    + "from [dbo].[tblWeChatReportJob] a1 left join [dbo].[tblWeChatReportSQL] a2 "
                    + "on a1.SQLGUID=a2.GUID where a1.Status=" + status + " and a1.JobName like @search order by a1.ID";
                System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
                Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = dbHelper.GetDataTable(sql, "WeChatReportJobMast", CommandType.Text,
                    new System.Data.SqlClient.SqlParameter[] { p1 });
                if (data == null || data.Rows.Count == 0) return null;
                return data.AsEnumerable().Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageJob()
                {
                    ID = dr.Field<int>("ID"),
                    SQLName = dr.Field<string>("Name").GetString(),
                    JobName = dr.Field<string>("JobName").GetString(),
                    RunWeek = dr.Field<int?>("RunWeek").ToInt(),
                    KeyFields = dr.Field<string>("KeyFields").GetString(),
                    ParameterFields = dr.Field<string>("ParameterFields").GetString(),
                    DailyStartTime = dr.Field<string>("DailyStartTime").GetString()
                }).ToList();
            }
            catch { return null; }
        }

        public Model.WeChatSqlMessage.WeChatSqlMessageJob GetWeChatReportJobData(int ID)
        {
            try
            {
                string sql = "select a1.JobName,a1.SQLGUID,a2.Name,a1.RunWeek,a1.DailyStartTime,a1.KeyFields,a1.DataSql,"
                    + "a1.ParameterFields,a1.Status,a1.EmployeeIDField from [dbo].[tblWeChatReportJob] a1 "
                    + "left join [dbo].[tblWeChatReportSQL] a2 on a1.SQLGUID=a2.GUID where a1.ID=" + ID;
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count != 1) return null;
                return new Model.WeChatSqlMessage.WeChatSqlMessageJob()
                {
                    ID = ID,
                    DataSQL = data.Rows[0].Field<string>("DataSql").GetString(),
                    ParameterFields = data.Rows[0].Field<string>("ParameterFields").GetString(),
                    KeyFields = data.Rows[0].Field<string>("KeyFields").GetString(),
                    SQLName = data.Rows[0].Field<string>("Name").GetString(),
                    JobName = data.Rows[0].Field<string>("JobName").GetString(),
                    SQLGUID = data.Rows[0].Field<string>("SQLGUID").GetString(),
                    RunWeek = data.Rows[0].Field<int?>("RunWeek").ToInt(),
                    DailyStartTime = data.Rows[0].Field<string>("DailyStartTime").GetString(),
                    EmployeeIDField = data.Rows[0].Field<string>("EmployeeIDField").GetString(),
                    Status = data.Rows[0].Field<bool?>("Status").BoolToInt()
                };
            }
            catch { return null; }
        }

        public Model.Common.BaseResponse RunWeChatJob(int jobID, string employeeID)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try
            {
                if (jobID.Equals(0) || string.IsNullOrWhiteSpace(employeeID)) throw new Exception("Job ID or EmployeeID not found");
                List<string> retSqls = new SEMI.Schdule.WeChatSqlMessageTask().RunByJobID(jobID, employeeID);
                resp.Status = "ok";
                resp.Msg = string.Join("\n\n", retSqls.ToArray());
            }
            catch (Exception e) 
            {
                resp.Status = "error";
                resp.Msg = e.Message;
            }
            return resp;
        }

    }
}
