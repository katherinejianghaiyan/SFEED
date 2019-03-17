using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Menu
{
    public class MenuMast
    {
        public string MenuName { get; set; }
        public string MenuIconClassName { get; set; }
        public string MenuAction { get; set; }
        public string MenuPermission { get; set; }
        public List<MenuMast> ChildMenus { get; set; }
    }
}
