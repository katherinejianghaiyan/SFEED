using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Data;

namespace Utils.Common
{
    public static class Functions
    {
        /// <summary>
        /// 重复字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="repleteNumber"></param>
        /// <returns></returns>
        public static string StringReplete(this string str, int repleteNumber)
        {
            if (str == null) return string.Empty;
            StringBuilder returnBuilder = new StringBuilder(str);
            for (int i = 0; i < repleteNumber; i++)
                returnBuilder.Append(str);
            return returnBuilder.ToString();
        }
        
        /// <summary>
        /// 清理Sql语句中的特殊字符
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SqlEscapeString(this string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return string.Empty;
            return str.Replace("'", "''").Replace("--","");
        }
        /// <summary>
        /// 将Null的对象转换成string.Empty
        /// </summary>
        /// <param name="nullObject">对象</param>
        /// <returns>字符串</returns>
        public static string GetString(this object obj)
        {
            if (obj == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(obj.ToString())) return string.Empty;
            else return obj.ToString().Trim();
        }
        /// <summary>
        /// 将对象转换成Double类型,空值返回0
        /// </summary>
        /// <param name="o">对象</param>
        /// <param name="digtial">保留小数位数</param>
        /// <returns>四舍五入后的对象值</returns>
        public static double ToDouble(this object obj, int decimals)
        {
            if (obj == null) return 0;
            if (string.IsNullOrWhiteSpace(obj.ToString())) return 0;
            double result = 0;
            if (double.TryParse(obj.ToString().Trim(), out result)) return Math.Round(result, decimals, MidpointRounding.AwayFromZero);
            else return 0;
        }
        /// <summary>
        /// 将对象转换成Double类型,空值或转换失败根据输入值返回
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="decimals"></param>
        /// <param name="retVal">如果为空或转换失败,则返回retVal</param>
        /// <returns></returns>
        public static double ToDouble(this object obj, int decimals, double retVal)
        {
            if (obj == null) return retVal;
            if (string.IsNullOrWhiteSpace(obj.ToString())) return retVal;
            double result = 0;
            if (double.TryParse(obj.ToString().Trim(), out result)) return Math.Round(result, decimals, MidpointRounding.AwayFromZero);
            else return retVal;
        }

        public static int ToInt(this object obj)
        {
            if (obj == null) return 0;
            if (string.IsNullOrWhiteSpace(obj.ToString())) return 0;
            int result = 0;
            if (int.TryParse(obj.ToString().Trim(), out result)) return result;
            else return 0;
        }

        public static string ToFormatDate(this DateTime? obj, string format, string emptyType)
        {
            try
            {
                if (obj == null)
                {
                    if (emptyType.Equals("Max")) return DateTime.Parse("2299-12-31").ToString(format);
                    if (emptyType.Equals("Min")) return DateTime.Parse("1900-01-01").ToString(format);
                    return string.Empty;
                }
                return ((DateTime)obj).ToString(format);
            }
            catch { return string.Empty; }
            
        }

        public static int BoolToInt(this bool? obj)
        {
            if (obj == null) return 0;
            if ((bool)obj) return 1;
            else return 0;
        }

        public static void InitDateRange(ref string sDate, ref string eDate)
        {
            try
            {
                DateTime _s = DateTime.Parse("1900-01-01");
                DateTime _e = DateTime.Parse("2099-12-31");
                if (!string.IsNullOrWhiteSpace(sDate)) _s = DateTime.Parse(sDate);
                if (!string.IsNullOrWhiteSpace(eDate)) _e = DateTime.Parse(eDate);
                if (_s > _e)
                {
                    DateTime _t = _s;
                    _s = _e;
                    _e = _t;
                }
                sDate = _s.ToString("yyyyMMdd");
                eDate = _e.ToString("yyyyMMdd");
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Date"></param>
        /// <param name="format"></param>
        /// <param name="emptyType">Max,Min</param>
        /// <returns></returns>
        public static string ToDateString(this int? Date, string format, string emptyType)
        {
            try
            {
                string retDate = string.Empty;
                if (emptyType.Equals("Max")) retDate = DateTime.Parse("2099-12-31").ToString(format);
                if (emptyType.Equals("Min")) retDate = DateTime.Parse("1900-01-01").ToString(format);
                string d = Date.ToString();
                if (d.Length != 8) return retDate;
                DateTime tmpDate = DateTime.Now;
                if (DateTime.TryParse(d.Substring(0, 4) + "-" + d.Substring(4, 2) + "-" + d.Substring(6, 2), out tmpDate))
                    retDate = tmpDate.ToString(format);
                return retDate;
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// 根据列名列表,创建所有字段都是String的DataTabe结构
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static DataTable CreateTableStructAsString(List<string> columnNames, string tableName)
        {
            DataTable data = new DataTable();
            data.TableName = tableName;
            DataColumn dc = null;
            foreach (string columnName in columnNames)
            {
                dc = new DataColumn();
                dc.ColumnName = columnName;
                dc.DataType = typeof(string);
                data.Columns.Add(dc);
            }
            return data;
        }

        public static DataTable CreateTableStruct(Dictionary<string, Type> columns, string tableName)
        {
            DataTable data = new DataTable();
            data.TableName = tableName;
            DataColumn dc = null;
            foreach (var column in columns)
            {
                dc = new DataColumn();
                dc.ColumnName = column.Key;
                dc.DataType = column.Value;
                data.Columns.Add(dc);
            }
            return data;
        }

        public static void AddDataRow(DataTable data, params object[] dataValues)
        {
            data.Rows.Add(dataValues);
        }
    }
}