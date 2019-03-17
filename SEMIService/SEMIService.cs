using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;

namespace SEMIService
{
    public partial class SEMIService : ServiceBase
    {
        private System.Threading.Timer timer;
        private SEMI.Wcf.SEMIWcfHosting hosting;


        public SEMIService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartWcfHosting(); //开启Wcf监听
            StartSchduleTasks(); //开启定时执行任务
        }

        protected override void OnStop()
        {
            if (timer != null) try { timer.Dispose(); }
                catch { }
            if (hosting != null) hosting.Stop();
        }

        private void StartSchduleTasks()
        {
            try
            {
                string period = ConfigurationManager.AppSettings["SchdulePeriod"].ToString();
                if (!string.IsNullOrWhiteSpace(period))
                {
                    int minute = 0;
                    if (int.TryParse(period, out minute))
                    {
                        SEMI.Schdule.Handler handler = SEMI.Schdule.Handler.GetInstance();
                        handler.SendTask(new SEMI.Schdule.MRPTask()); //MRP任务
                       // handler.SendTask(new SEMI.Schdule.WeChatSqlMessageTask()); //发送微信Job任务
                        handler.SendTask(new SEMI.Schdule.CustomerDataTask()); //系统检查
                        timer = new System.Threading.Timer(new System.Threading.TimerCallback((obj) =>
                        {
                            try 
                            {
                                EventLog.WriteEntry("[SchduleTaskProcess]:Started", EventLogEntryType.Information);
                                handler.ProcessTaskQueue();
                                EventLog.WriteEntry("[SchduleTaskProcess]:Finished", EventLogEntryType.Information);
                            }
                            catch (Exception ex) { EventLog.WriteEntry("[SchduleTaskProcess]:" + ex.Message, EventLogEntryType.Error); }
                        }), null, 5000, minute * 60 * 1000); //延时5秒启动
                    }
                }
                else throw new Exception("SchdulePeriod not found. please check app.config file");
            }
            catch (Exception e) { EventLog.WriteEntry("[SchduleTask]:" + e.Message, EventLogEntryType.Warning); }
        }

        private void StartWcfHosting()
        {
            hosting = new SEMI.Wcf.SEMIWcfHosting();
            try { hosting.Start(typeof(SEMI.Service.SEMIService)); }
            catch (Exception e) { this.EventLog.WriteEntry("[WcfHosting]:" + e.Message, EventLogEntryType.Error); }
            this.EventLog.WriteEntry("SEMIService Start,IP:" + hosting.GetIp() + ",PORT:" + hosting.GetPort(),
                EventLogEntryType.Information);
        }
    }
}
