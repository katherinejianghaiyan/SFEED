using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFEEDDataSync
{
    public class LabelMessage
    {
        public string DisplayText { get; set; }
        public string ErrorText { get; set; }
        public bool ShowProcessBar { get; set; }
        public bool ShowRestartBtn { get; set; }
        public bool IsFinish { get; set; }
    }
}
