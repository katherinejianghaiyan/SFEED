using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SEMIService
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            //SEMI.MRP.MRPHelper.GetInstance().Run();
            //return;
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new SEMIService() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
