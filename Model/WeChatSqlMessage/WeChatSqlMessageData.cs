using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.WeChatSqlMessage
{
    public class WeChatSqlMessageData: Table.TableData
    {
        public string GUID { get; set; }
        public string ParentGUID { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string ContentSQL { get; set; }
        public string TitleSQL { get; set; }
        public string DisplayType { get; set; }
        public string LinkName { get; set; }
        public string LinkField { get; set; }
        public int SpaceNumber { get; set; }
        public int Status { get; set; }
        public int IsDel { get; set; }
    }
}
