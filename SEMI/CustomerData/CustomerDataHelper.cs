using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Utils.Common;

namespace SEMI.CustomerData
{
    public class CustomerDataHelper: Common.BaseDataHelper
    {
        private static CustomerDataHelper instance = new CustomerDataHelper();
        private CustomerDataHelper() { }
        public static CustomerDataHelper GetInstance() { return instance; }

        public void Run(int customerDataID, string processSql, string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(processSql)) throw new Exception("SQL is null.");
                Utils.Database.SqlServer.DBHelper helper = new Utils.Database.SqlServer.DBHelper(_conn);
                DataTable data = helper.GetDataTable(processSql);
                if (data == null || data.Rows.Count == 0) WriteResult(customerDataID, string.Empty, helper);
                else
                {
                    string fileName = Utils.Excel.ExcelHelper.GetInstance().SaveDataTable(data, filePath, ".xlsx");
                    WriteResult(customerDataID, fileName, helper);
                } 
            }
            catch (Exception e) { Log.LogHelper.GetInstance().WriteDBLog("CustomerData", "处理失败", customerDataID.ToString(), e.Message); }
        }

        private void WriteResult(int customerDataID, string fileName, Utils.Database.SqlServer.DBHelper helper)
        {
            string sql = "insert into [dbo].[tblCustomerDataResult](CustomerDataID,FileName,CreateDate) values("
                + customerDataID + ",@fname,'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "');";
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@fname", SqlDbType.VarChar, 100);
            p1.Value = fileName;
            int c = helper.Execute(sql, CommandType.Text, new System.Data.SqlClient.SqlParameter[] { p1 });
            if (c <= 0) throw new Exception("Insert tblCustomerDataResult failed");
        }

        public Model.Table.TableMast<Model.CustomerData.CustomerDataMast> GetTableCustomerDatas(int status, string keyWords, string language)
        {
            Model.Table.TableMast<Model.CustomerData.CustomerDataMast> tableMast =
               new Model.Table.TableMast<Model.CustomerData.CustomerDataMast>();
            tableMast.draw = 1;
            tableMast.data = GetCustomerDatas(status, keyWords, language);
            if (tableMast.data == null) tableMast.data = new List<Model.CustomerData.CustomerDataMast>();
            tableMast.recordsTotal = tableMast.data.Count;
            tableMast.recordsFiltered = tableMast.recordsTotal;
            return tableMast;
        }

        public List<Model.CustomerData.CustomerDataMast> GetCustomerDatas(int status, string keyWords, string language)
        {
            string sql = "select ID,Name,Instruction,SQL,RunWeek from [dbo].[tblCustomerData] where Status=" + status
                + " and name like @search order by ID";
            System.Data.SqlClient.SqlParameter p1 = new System.Data.SqlClient.SqlParameter("@search", "%" + keyWords.GetString() + "%");
            Utils.Database.SqlServer.DBHelper dbHelper = new Utils.Database.SqlServer.DBHelper(_conn);
            DataTable data = dbHelper.GetDataTable(sql, "CustomerDataMast", CommandType.Text,
                new System.Data.SqlClient.SqlParameter[] { p1 });
            if (data == null || data.Rows.Count == 0) return null;
            return data.AsEnumerable().Select(dr => new Model.CustomerData.CustomerDataMast()
            {
                ID = dr.Field<int>("ID"),
                Name = dr.Field<string>("Name").GetString(),
                Instruction = dr.Field<string>("Instruction").GetString(),
                RunWeek = dr.Field<int?>("RunWeek").ToInt()
            }).ToList();
        }

        public Model.CustomerData.CustomerDataMast GetCustomerDataMast(int customerDataID)
        {
            string sql = "select Name,Instruction,SQL,RunWeek,Status,Remark from [dbo].[tblCustomerData] where ID=" + customerDataID;
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count != 1) return null;
            return new Model.CustomerData.CustomerDataMast()
            {
                ID = customerDataID,
                Name = data.Rows[0].Field<string>("Name").GetString(),
                Instruction = data.Rows[0].Field<string>("Instruction").GetString(),
                SQL = data.Rows[0].Field<string>("SQL").GetString(),
                RunWeek = data.Rows[0].Field<int?>("RunWeek").ToInt(),
                Status = data.Rows[0].Field<bool?>("Status").BoolToInt(),
                Remark = data.Rows[0].Field<string>("Remark").GetString()
            };
        }

        public Model.Common.BaseResponse ModifyCustomerDataData(Model.CustomerData.CustomerDataMast data, string language)
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
                        sql = "delete [dbo].[tblCustomerData] where ID='" + data.ID + "'";
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql);
                }
                else
                {
                    if (data.ID.Equals(0))
                    {
                        sql = "insert into [dbo].[tblCustomerData](Name,Instruction,SQL,RunWeek,Status,Remark) "
                            + "values(@name,@inst,@sql,@runweek,@status,@remark)";
                    }
                    else
                    {
                        sql = "update [dbo].[tblCustomerData] set Name=@name,Instruction=@inst,SQL=@sql,RunWeek=@runweek,"
                            + "Status=@status,Remark=@remark where ID=" + data.ID;
                    }
                    System.Data.SqlClient.SqlParameter name = new System.Data.SqlClient.SqlParameter("@name", SqlDbType.VarChar, 20);
                    name.Value = data.Name.GetString();
                    System.Data.SqlClient.SqlParameter dsql = new System.Data.SqlClient.SqlParameter("@sql", SqlDbType.VarChar, 5000);
                    dsql.Value = data.SQL.GetString();
                    System.Data.SqlClient.SqlParameter inst = new System.Data.SqlClient.SqlParameter("@inst", SqlDbType.VarChar, 60);
                    inst.Value = data.Instruction.GetString();
                    System.Data.SqlClient.SqlParameter status = new System.Data.SqlClient.SqlParameter("@status", SqlDbType.Bit, 1);
                    status.Value = data.Status;
                    System.Data.SqlClient.SqlParameter runweek = new System.Data.SqlClient.SqlParameter("@runweek", SqlDbType.Int, 5);
                    runweek.Value = data.RunWeek;
                    System.Data.SqlClient.SqlParameter remark = new System.Data.SqlClient.SqlParameter("@remark", SqlDbType.VarChar, 500);
                    remark.Value = data.Remark.GetString();
                    count = new Utils.Database.SqlServer.DBHelper(_conn).Execute(sql, CommandType.Text,
                              new System.Data.SqlClient.SqlParameter[] { name, dsql, inst, runweek, status, remark });
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

        public Model.CustomerData.CustomerDataLog GetCustomerDataResult(int customerDataID)
        {
            string sql = "select FileName,CreateDate from [dbo].[tblCustomerDataResult] a1 join "
                + "(select CustomerDataID,max(ID) as ID from [dbo].[tblCustomerDataResult] group by CustomerDataID) a2 "
                + "on a1.CustomerDataID=a2.CustomerDataID and a1.ID=a2.ID where a1.CustomerDataID=" + customerDataID;
            DataTable data = new Utils.Database.SqlServer.DBHelper(_conn).GetDataTable(sql);
            if (data == null || data.Rows.Count != 1) return null;
            return new Model.CustomerData.CustomerDataLog()
            {
                CreateDate = data.Rows[0].Field<DateTime?>("CreateDate").ToFormatDate("yyyy-MM-dd HH:mm:ss", string.Empty),
                FileName = data.Rows[0].Field<string>("FileName").GetString()
            };
        }
    }
}
