using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utils.Common;

namespace SFEEDSiteClient
{
    public partial class Update : Form
    {
        public Update()
        {
            InitializeComponent();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
            backgroundWorker.RunWorkerAsync();
        }

        private bool UpdateProgram()
        {
            try
            {
                string siteGuid = SEMI.Service.ClientServiceHelper.GetInstance().GetSiteGuid();
                if (string.IsNullOrWhiteSpace(siteGuid)) return false;
                string version = ConfigurationManager.AppSettings["Version"].GetString();
                string url = ConfigurationManager.AppSettings["UpdateUrl"].GetString();
                if (string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(siteGuid)
                    || string.IsNullOrWhiteSpace(url)) return false;
                string ticks = DateTime.Now.Ticks.ToString();
                return SEMI.UpdateApp.UpdateAppHelper.GetInstance().UpdateFiles(url + "?timestamp=" + ticks + "&key="
                    + Utils.Common.EncyptHelper.Encypt(version + "/" + siteGuid, ticks.Substring(ticks.Length - 8, 8)),
                    AppDomain.CurrentDomain.BaseDirectory);
            }
            catch (Exception e)
            {
                try
                {
                    SEMI.Log.LogHelper.GetInstance().WriteTxtLog("Date:"
                        + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ",Message:" + e.Message,
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs/Update.txt"));
                }
                catch { }
                return false;
            }
        }

        void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage.Equals(0)) this.DisplayLabel.Text = "请稍等,正在检查系统更新...";
            if (e.ProgressPercentage.Equals(50))
            {
                this.DisplayLabel.Text = "系统更新完成,点击确定重新启动";
                this.closeBtn.Visible = true;
            }
            if (e.ProgressPercentage.Equals(80))
            {
                this.DisplayLabel.Text = "没有可执行程序,请联系公司";
                this.closeBtn.Visible = true;
            }
            if (e.ProgressPercentage.Equals(90)) this.Visible = false;
            if (e.ProgressPercentage.Equals(100)) System.Environment.Exit(0);
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker.ReportProgress(0);
            string exePath = ConfigurationManager.AppSettings["ExePath"].GetString();
            if (UpdateProgram()) backgroundWorker.ReportProgress(50);
            else
            {
                if (string.IsNullOrWhiteSpace(exePath)) backgroundWorker.ReportProgress(80);
                else
                {
                    try
                    {
                        backgroundWorker.ReportProgress(90);
                        System.Diagnostics.Process process = System.Diagnostics.Process.Start(exePath);
                        process.WaitForExit();
                    }
                    catch { }
                    finally { backgroundWorker.ReportProgress(100); }
                }
            }
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}
