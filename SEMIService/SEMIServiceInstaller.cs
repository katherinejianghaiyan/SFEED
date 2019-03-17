using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.Threading.Tasks;
using System.ServiceProcess;

namespace SEMIService
{
    [RunInstaller(true)]
    public partial class SEMIServiceInstaller : System.Configuration.Install.Installer
    {
        private ServiceInstaller serviceInstaller;
        private ServiceProcessInstaller processInstaller;
        public SEMIServiceInstaller()
        {
            InitializeComponent();
            serviceInstaller = new ServiceInstaller();
            processInstaller = new ServiceProcessInstaller();
            processInstaller.Account = ServiceAccount.LocalSystem;
            //processInstaller.Username = null;
            //processInstaller.Password = null;
            serviceInstaller.ServiceName = "SEMIService";
            serviceInstaller.Description = "SEMIService";
            serviceInstaller.DisplayName = "SEMIService";
            serviceInstaller.AfterInstall += new InstallEventHandler(serviceInstaller_AfterInstall);
            serviceInstaller.BeforeUninstall += new InstallEventHandler(serviceInstaller_BeforeUninstall);
            Installers.Add(serviceInstaller);
            Installers.Add(processInstaller);
        }
        void serviceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            try
            {
                ServiceController con = new ServiceController("SEMIService");
                if (con.Status != ServiceControllerStatus.Stopped) con.Stop();
            }
            catch { }
        }

        void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
                ServiceController con = new ServiceController(serviceInstaller.ServiceName);
                con.Start();
            }
            catch { }
        }
    }
}
