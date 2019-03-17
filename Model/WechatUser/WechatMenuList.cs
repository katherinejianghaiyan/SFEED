using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.WechatUser
{
    public class WechatMenuList: Table.TableData
    {
        public string menuName { get; set; }
        public string menuAction { get; set; }
        public string menuIconClassName { get; set; }
        public string menuIconImg { get; set; }
        public string siteGUID { get; set; }
    }
}
