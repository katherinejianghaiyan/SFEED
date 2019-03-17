using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEMI.Schdule
{
    public class MRPTask: Model.Interface.ISchduleTask
    {
        public void Run()
        {
            SEMI.MRP.MRPHelper.GetInstance().Run();
        }
    }
}
