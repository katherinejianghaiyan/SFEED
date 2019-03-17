using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils.Database.SqlServer
{
    public class DBQueryDic
    {
        public string TableName { get; set; }
        public string Sql { get; set; }
        public System.Data.SqlClient.SqlParameter[] Parameters { get; set; }
    }
}
