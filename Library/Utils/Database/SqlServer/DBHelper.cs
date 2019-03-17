using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Utils.Common;

namespace Utils.Database.SqlServer
{
    public class DBHelper
    {
        private static string _conn = "Data Source={0};Initial Catalog={1};User ID={2};Password={3};Persist Security Info=True;Connection Timeout=10";

        private string conn = string.Empty;

        public DBHelper(string ipAddress, string dataBaseName, string userID, string password)
        {
            this.conn = string.Format(_conn, ipAddress, dataBaseName, userID, password);
        }

        public DBHelper(string configString)
        {
            this.conn = configString;
        }

        /// <summary>
        /// 获取数据表
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <param name="tableName">表名</param>
        /// <returns>数据表</returns>
        public DataTable GetDataTable(string sql, string tableName)
        {
            return this.GetDataTable(sql, tableName, CommandType.Text, null);
        }

        /// <summary>
        /// 获取数据表
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <returns>数据表</returns>
        public DataTable GetDataTable(string sql)
        {
            return this.GetDataTable(sql, string.Empty, CommandType.Text, null);
        }

        /// <summary>
        /// 获取数据表
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <param name="commandType">查询类型,文本,存储过程...</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>数据表</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        public DataTable GetDataTable(string sql, string tableName, CommandType commandType, SqlParameter[] parameters)
        {
            if (sql.GetString().Equals(string.Empty)) return null;
            DataTable data = null;
            using (SqlConnection connection = new SqlConnection(this.conn))
            {
                using (SqlCommand command = new SqlCommand(sql.GetString().Trim(), connection))
                {
                    command.CommandType = commandType;
                    command.CommandTimeout = 600;
                    //如果传入参数@parameter, 则添加参数
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (SqlParameter parameter in parameters)
                            command.Parameters.Add(parameter);
                    }
                    connection.Open();
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        data = new DataTable();
                        data.TableName = tableName.GetString().Equals(string.Empty) ? "table" : tableName.GetString().Trim();
                        adapter.Fill(data);
                    }
                }
            }
            return data;
        }

        /// <summary>
        /// 获取数据集
        /// </summary>
        /// <param name="queryList">查询对象</param>
        /// <returns>数据集</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        public DataSet GetDataSet(IList<DBQueryDic> queryList)
        {
            if (queryList == null || queryList.Count == 0) return null;
            DataSet ds = null;
            using (SqlConnection connection = new SqlConnection(this.conn))
            {
                connection.Open();
                foreach (var query in queryList)
                {
                    if (query.Sql.GetString().Equals(string.Empty)) continue;
                    using (SqlCommand command = new SqlCommand(query.Sql.GetString().Trim(), connection))
                    {
                        //如果传入参数@parameter, 则添加参数
                        if (query.Parameters != null && query.Parameters.Length > 0)
                        {
                            foreach (SqlParameter parameter in query.Parameters)
                                command.Parameters.Add(parameter);
                        }
                        command.CommandTimeout = 600;
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            DataTable data = new DataTable();
                            data.TableName = query.TableName.GetString().Trim();
                            adapter.Fill(data);
                            if (ds == null) ds = new DataSet();
                            ds.Tables.Add(data);
                        }
                    }
                }
            }
            return ds;
        }

        public SqlDataReader GetDataReader(string sql)
        {
            return this.GetDataReader(sql, CommandType.Text, null);
        }

        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <param name="commandType">查询类型,文本,存储过程</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>SqlDataReader对象</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        public SqlDataReader GetDataReader(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            if (sql.GetString().Equals(string.Empty)) return null;
            using (SqlConnection connection = new SqlConnection(this.conn))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = commandType;
                    command.CommandTimeout = 600;
                    //如果传入参数@parameter, 则添加参数
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (SqlParameter parameter in parameters)
                            command.Parameters.Add(parameter);
                    }
                    connection.Open();
                    return command.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
        }

        /// <summary>
        /// 获取第一行第一列数据
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <returns>单一数据</returns>
        public object GetDataScalar(string sql)
        {
            return this.GetDataScalar(sql, CommandType.Text, null);
        }

        /// <summary>
        /// 获取第一行第一列数据
        /// </summary>
        /// <param name="sql">单一执行的Sql查询语句</param>
        /// <param name="commandType">查询类型,文本,存储过程</param>
        /// <param name="parameters">查询参数</param>
        /// <returns>单一数据</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        public object GetDataScalar(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            if (sql.GetString().Equals(string.Empty)) return null;
            using (SqlConnection connection = new SqlConnection(this.conn))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = commandType;
                    command.CommandTimeout = 600;
                    //如果传入参数@parameter, 则添加参数
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (SqlParameter parameter in parameters)
                            command.Parameters.Add(parameter);
                    }
                    connection.Open();
                    return command.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// 增,删,改数据库操作
        /// </summary>
        /// <param name="sql">操作语句</param>
        /// <returns>执行成功记录数</returns>
        public int Execute(string sql)
        {
            return this.Execute(sql, CommandType.Text, null);
        }

        /// <summary>
        /// 增,删,改数据库操作
        /// </summary>
        /// <param name="sql">操作语句</param>
        /// <param name="commandType">操作类型,文本,存储过程</param>
        /// <param name="parameters">操作参数</param>
        /// <returns>执行成功记录数</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:检查 SQL 查询是否存在安全漏洞")]
        public int Execute(string sql, CommandType commandType, SqlParameter[] parameters)
        {
            if (sql.GetString().Equals(string.Empty)) return 0;
            using (SqlConnection connection = new SqlConnection(this.conn))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                using (SqlCommand command = new SqlCommand(sql.GetString().Trim(), connection, transaction))
                {
                    command.CommandType = commandType;
                    command.CommandTimeout = 600;
                    //如果传入参数@parameter, 则添加参数
                    if (parameters != null && parameters.Length > 0)
                    {
                        foreach (SqlParameter parameter in parameters)
                            command.Parameters.Add(parameter);
                    }
                    try { int r = command.ExecuteNonQuery(); transaction.Commit(); return r; }
                    catch { transaction.Rollback(); return 0; }
                }
            }
        }

    }
}