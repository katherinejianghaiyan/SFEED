using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Configuration;
using Utils.Common;

namespace SEMI.Schdule
{
    public class WeChatSqlMessageTask : Common.BaseDataHelper, Model.Interface.ISchduleTask
    {
        private bool processStatus = false;
        public void Run()
        {
            if (processStatus) return;
            processStatus = true;
            try
            {
                string jobSql = "select ID,SQLGUID,DataSql,KeyFields,ParameterFields,EmployeeIDField,RunWeek,DailyStartTime "
                    + "from [dbo].[tblWeChatReportJob] where Status=1";
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable jobData = helper.GetDataTable(jobSql);
                if (jobData == null || jobData.Rows.Count == 0) return;
                List<Model.WeChatSqlMessage.WeChatSqlMessageJob> jobList = jobData.AsEnumerable()
                    .Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageJob()
                    {
                        ID = dr.Field<int>("ID"),
                        SQLGUID = dr.Field<string>("SQLGUID").GetString(),
                        DataSQL = dr.Field<string>("DataSql").GetString(),
                        KeyFields = dr.Field<string>("KeyFields").GetString(),
                        ParameterFields = dr.Field<string>("ParameterFields").GetString(),
                        RunWeek = dr.Field<int?>("RunWeek").ToInt(),
                        EmployeeIDField = dr.Field<string>("EmployeeIDField").GetString(),
                        DailyStartTime = dr.Field<string>("DailyStartTime").GetString()
                    }).ToList();

                int agentId = ConfigurationManager.AppSettings["SEMIAgentID"].ToInt();

                foreach (var job in jobList)
                {
                    if (!job.RunWeek.Equals(0) && !(DateTime.Now.DayOfWeek + 1).Equals(job.RunWeek)) continue;
                    if (!string.IsNullOrWhiteSpace(job.DailyStartTime) &&
                        DateTime.Now < DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + job.DailyStartTime)) continue;
                    try
                    {
                        Process(job.ID, job.SQLGUID, job.DataSQL, job.KeyFields, job.ParameterFields, job.EmployeeIDField, 
                            helper, agentId, string.Empty);
                    }
                    catch (Exception ex) { Log.LogHelper.GetInstance().WriteDBLog("SchduleJob", "处理Job失败", "JobID:" + job.ID, ex.Message); }
                }
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("SchduleJob", "运行错误", "WeChatSqlMessageTask.Run()", e.Message); }
            finally { processStatus = false; }
        }

        public List<string> RunByJobID(int jobID, string employeeID)
        {
            string jobSql = "select ID,SQLGUID,DataSql,KeyFields,ParameterFields,EmployeeIDField,RunWeek,DailyStartTime "
                   + "from [dbo].[tblWeChatReportJob] where ID=" + jobID;
            Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = helper.GetDataTable(jobSql);
            if (data == null || data.Rows.Count != 1) throw new Exception("Job ID not found.");
            int agentId = ConfigurationManager.AppSettings["SEMIAgentID"].ToInt();

            Model.WeChatSqlMessage.WeChatSqlMessageJob job = new Model.WeChatSqlMessage.WeChatSqlMessageJob()
            {
                ID = data.Rows[0].Field<int>("ID"),
                SQLGUID = data.Rows[0].Field<string>("SQLGUID").GetString(),
                DataSQL = data.Rows[0].Field<string>("DataSql").GetString(),
                KeyFields = data.Rows[0].Field<string>("KeyFields").GetString(),
                ParameterFields = data.Rows[0].Field<string>("ParameterFields").GetString(),
                RunWeek = data.Rows[0].Field<int?>("RunWeek").ToInt(),
                EmployeeIDField = data.Rows[0].Field<string>("EmployeeIDField").GetString(),
                DailyStartTime = data.Rows[0].Field<string>("DailyStartTime").GetString()
            };
            return ProcessByJobID(job.ID, job.SQLGUID, job.DataSQL, job.ParameterFields, employeeID, helper, agentId, string.Empty);
        }

        private List<string> ProcessByJobID(int jobID, string sqlGUID, string dataSql, string parameterFields, string employeeID,
            Utils.Database.SqlServer.DBHelper helper, int agentId, string partyIds)
        {
            if (string.IsNullOrWhiteSpace(dataSql)) throw new Exception("没有为Job设置查询语句");
            DataTable data = helper.GetDataTable(dataSql);
            if (data == null || data.Rows.Count == 0) throw new Exception("没有查询到执行数据");        
            List<Model.WeChatSqlMessage.WeChatSqlMessageJobDetail> jobDetails =
                data.AsEnumerable().Select(dr => new
                {
                    ParametersValues = GetCodeValues(dr, parameterFields)
                }).GroupBy(dg => new
                {
                    PValues = dg.ParametersValues
                }).Select(g => new Model.WeChatSqlMessage.WeChatSqlMessageJobDetail()
                {
                    ParametersValues = g.Key.PValues,
                    EmployeeID = employeeID
                }).Distinct().ToList();
            string querySql = "select GUID,ContentSQL,TitleSQL,SpaceNumber,LinkName from [dbo].[tblWeChatReportSQL] "
                            + "where Status=1 and GUID='{0}' union select GUID,NULL,NULL,NULL,LinkName "
                            + "from [dbo].[tblWeChatReportSQL] where Status=1 and ParentGUID='{0}'";
            DataTable qData = helper.GetDataTable(string.Format(querySql, sqlGUID));
            if (qData == null || qData.Rows.Count == 0) throw new Exception("SqlGUID没有找到对应的查询");
            Model.WeChatSqlMessage.WeChatSqlMessageData main = qData.AsEnumerable()
                .Where(dr => dr.Field<string>("GUID").GetString().Equals(sqlGUID))
                .Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageData()
                {
                    GUID = sqlGUID,
                    SpaceNumber = dr.Field<int?>("SpaceNumber").ToInt(),
                    ContentSQL = dr.Field<string>("ContentSql").GetString(),
                    TitleSQL = dr.Field<string>("TitleSql").GetString()
                }).First();
            List<Model.WeChatSqlMessage.WeChatSqlMessageData> childs = null;
            var cq = qData.AsEnumerable().Where(dr => !dr.Field<string>("GUID").GetString().Equals(sqlGUID));
            if (cq.Any())
            {
                childs = cq.Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageData()
                {
                    GUID = dr.Field<string>("GUID").GetString(),
                    LinkName = dr.Field<string>("LinkName").GetString()
                }).ToList();
            }
            List<string> retSqls = new List<string>();
            foreach (var q in jobDetails)
            {
                retSqls.Add(string.Format(main.ContentSQL, q.ParametersValues.Split(',', ';')));
                SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().SendMessage(
                    employeeID, main, childs, q.ParametersValues, agentId, partyIds, (code, msg) =>
                    {
                        if (!code.Equals("0") && !msg.Equals("ok")) throw new Exception("error:" + msg);
                    });
            }
            return retSqls;
        }

        private void Process(int jobID, string sqlGUID, string dataSql, string keyFields, string parameterFields, 
            string employeeIDField, Utils.Database.SqlServer.DBHelper helper, int agentId, string partyIds) 
        {
            if (string.IsNullOrWhiteSpace(dataSql)) throw new Exception("没有为Job设置查询语句");
            if (string.IsNullOrWhiteSpace(keyFields)) throw new Exception("没有为Job设置KeyCode");
            if (string.IsNullOrWhiteSpace(employeeIDField)) throw new Exception("没有为Job设置发送人员字段");
            DataTable data = helper.GetDataTable(dataSql);
            if (data == null || data.Rows.Count == 0) throw new Exception("没有查询到执行数据");
            DataTable logData = null;
            Task t = new Task(() =>
            {
                string jobLogSql = "select KeyCode,RunDate from [dbo].[tblWeChatReportJobLog] where JobID={0} and RunDate={1}";
                logData = helper.GetDataTable(string.Format(jobLogSql, jobID, DateTime.Now.ToString("yyyyMMdd")));
                if (logData == null) logData = new DataTable();
            });
            t.Start();
            List<Model.WeChatSqlMessage.WeChatSqlMessageJobDetail> jobDetails =
                data.AsEnumerable().Select(dr => new
                {
                    EmployeeID = dr.Field<string>(employeeIDField).GetString(),
                    ParametersValues = GetCodeValues(dr, parameterFields),
                    KeyValues = GetCodeValues(dr, keyFields)
                }).GroupBy(dg => new
                {
                    PValues = dg.ParametersValues,
                    KValues = dg.KeyValues
                }).Select(g => new Model.WeChatSqlMessage.WeChatSqlMessageJobDetail()
                {
                    ParametersValues = g.Key.PValues,
                    KeyValues = g.Key.KValues,
                    EmployeeID = string.Join("|", g.Select(gg => gg.EmployeeID).ToArray())
                }).ToList();
            t.Wait();
            var query = (from j in jobDetails
                         join l in logData.AsEnumerable()
                         on j.KeyValues equals l.Field<string>("KeyCode").GetString() into ldata
                         from ld in ldata.DefaultIfEmpty()
                         where !string.IsNullOrWhiteSpace(j.EmployeeID) && ld == null
                         select j).ToList();
            if (!query.Any()) return;
            string querySql = "select GUID,ContentSQL,TitleSQL,SpaceNumber,LinkName from [dbo].[tblWeChatReportSQL] "
                            + "where Status=1 and GUID='{0}' union select GUID,NULL,NULL,NULL,LinkName "
                            + "from [dbo].[tblWeChatReportSQL] where Status=1 and ParentGUID='{0}'";
            DataTable qData = helper.GetDataTable(string.Format(querySql, sqlGUID));
            if (qData == null || qData.Rows.Count == 0) throw new Exception("SqlGUID没有找到对应的查询");
            Model.WeChatSqlMessage.WeChatSqlMessageData main = qData.AsEnumerable()
                .Where(dr => dr.Field<string>("GUID").GetString().Equals(sqlGUID))
                .Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageData()
                {
                    GUID = sqlGUID,
                    SpaceNumber = dr.Field<int?>("SpaceNumber").ToInt(),
                    ContentSQL = dr.Field<string>("ContentSql").GetString(),
                    TitleSQL = dr.Field<string>("TitleSql").GetString()
                }).First();
            List<Model.WeChatSqlMessage.WeChatSqlMessageData> childs = null;
            var cq = qData.AsEnumerable().Where(dr => !dr.Field<string>("GUID").GetString().Equals(sqlGUID));
            if (cq.Any())
            {
                childs = cq.Select(dr => new Model.WeChatSqlMessage.WeChatSqlMessageData()
                {
                    GUID = dr.Field<string>("GUID").GetString(),
                    LinkName = dr.Field<string>("LinkName").GetString()
                }).ToList();
            }
            foreach (var q in query)
            {
                SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().SendMessage(
                    q.EmployeeID, main, childs, q.ParametersValues, agentId, partyIds, (code, msg) =>
                    {
                        if (code.Equals("0") || msg.Equals("ok")) SetLog(jobID, q.KeyValues, helper);
                        else Log.LogHelper.GetInstance().WriteDBLog("SchduleJob", "微信调用失败", "JobID:" + jobID, msg);
                    });
            }
        }

        private string GetCodeValues(DataRow row, string fields)
        {
            try
            {
                string[] keys = fields.Split(',', ';');
                string[] values = new string[keys.Length];
                for (int i = 0; i < keys.Length; i++)
                    values[i] = row[keys[i]].ToString();
                return string.Join(",", values.ToArray());
            }
            catch (Exception e) { throw new Exception("[Fields:" + fields + "]:" + e.Message); }
        }

        private void SetLog(int jobId, string keyCode, Utils.Database.SqlServer.DBHelper helper)
        {
            try
            {
                string sql = "insert into [dbo].[tblWeChatReportJobLog](KeyCode,JobID,RunDate) values('" + keyCode + "',"
                           + jobId + "," + DateTime.Now.ToString("yyyyMMdd") + ")";
                helper.Execute(sql);
            }
            catch { }
        }
    }
}
