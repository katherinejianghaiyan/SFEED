using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Configuration;
using System.Threading.Tasks;
using Utils.Common;

namespace SEMI.Schdule
{
    public class CustomerDataTask : Common.BaseDataHelper, Model.Interface.ISchduleTask
    {
        private bool processStatus = false;
        private int processDate = 0;

        public void Run()
        {
            if (processStatus) return;
            processStatus = true;
            try
            {
                DateTime now = DateTime.Now;
                if (processDate.Equals(now.ToString("yyyyMMdd").ToInt())) return; //1天只执行1次
                string startTime = ConfigurationManager.AppSettings["CustomerDataStartTime"].GetString();
                if (string.IsNullOrWhiteSpace(startTime)) throw new Exception("CustomerDataStartTime not found in app.config");
                if (DateTime.Now < DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd") + " " + startTime)) return;
                string sql = "select ID,SQL,RunWeek from [dbo].[tblCustomerData] where Status=1";
                DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
                if (data == null || data.Rows.Count == 0) return;
                int runWeek = 0;
                string filePath = ConfigurationManager.AppSettings["CustomerDataPath"].GetString();
                if (string.IsNullOrWhiteSpace(filePath) || !System.IO.Directory.Exists(filePath))
                    throw new Exception("CustomerDataPath not found.");
                foreach (DataRow dr in data.Rows)
                {
                    runWeek = dr.Field<int?>("RunWeek").ToInt();
                    if (!runWeek.Equals(0) && !(now.DayOfWeek + 1).Equals(runWeek)) continue;
                    SEMI.CustomerData.CustomerDataHelper.GetInstance().Run(dr.Field<int>("ID"), dr.Field<string>("SQL").GetString(), filePath);
                }
                processDate = now.ToString("yyyyMMdd").ToInt();
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("CustomerDataTask", "运行错误", "CustomerDataTask.Run()", e.Message); }
            finally { processStatus = false; }
        }

        public void RunByCustomerDataID(int customerDataID)
        {
            string sql = "select SQL from [dbo].[tblCustomerData] where ID=" + customerDataID;
            string data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataScalar(sql).ToString();
            string filePath = ConfigurationManager.AppSettings["CustomerDataPath"].GetString();
            SEMI.CustomerData.CustomerDataHelper.GetInstance().Run(customerDataID, data, filePath);
        }
    }
}
