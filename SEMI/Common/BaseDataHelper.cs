using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Utils.Common;

namespace SEMI.Common
{
    public class BaseDataHelper
    {
        public static string _conn = ConfigurationManager.ConnectionStrings["SEMI"].GetString();
        public static string _conn2 = ConfigurationManager.ConnectionStrings["POS"].GetString();
        //Utils.Common.EncyptHelper.DesEncypt(ConfigurationManager.ConnectionStrings["SEMI"].GetString());
    }
}
