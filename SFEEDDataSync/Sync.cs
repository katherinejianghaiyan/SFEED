using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.ServiceModel;
using Utils.Common;

namespace SFEEDDataSync
{
    public partial class Sync : Form
    {
        private string siteGuid = string.Empty;
        public Sync()
        {
            InitializeComponent();
            siteGuid = SEMI.Service.ClientServiceHelper.GetInstance().GetSiteGuid();
            if (string.IsNullOrWhiteSpace(siteGuid))
            {
                this.DisplayLabel.Text = "获取营运点信息失败,请联系公司";
            }
            else
            {
                backgroundWorker.WorkerReportsProgress = true;
                backgroundWorker.DoWork += new DoWorkEventHandler(backgroundWorker_DoWork);
                backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(backgroundWorker_ProgressChanged);
                backgroundWorker.RunWorkerAsync();
            }
        }

        void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            LabelMessage message = new LabelMessage();
            message.DisplayText = string.Empty;
            message.ErrorText = string.Empty;
            message.ShowProcessBar = false;
            message.ShowRestartBtn = false;
            message.IsFinish = false;
            try
            {
                string serverUrl = ConfigurationManager.AppSettings["ServerUrl"].GetString();
                if (string.IsNullOrWhiteSpace(serverUrl) || string.IsNullOrWhiteSpace(siteGuid)) return;
                message.ShowProcessBar = true;
                using (ChannelFactory<Model.Interface.ISEMIService> cf =
                    new ChannelFactory<Model.Interface.ISEMIService>("SEMIClientService"))
                {
                    cf.Endpoint.Address = new EndpointAddress(serverUrl);
                    Model.Interface.ISEMIService instance = cf.CreateChannel();
                    int processInt = 0;
                    string ticks = DateTime.Now.Ticks.ToString();
                    SEMI.Service.ServerServiceHelper helper = new SEMI.Service.ServerServiceHelper(ticks,
                        SEMI.Service.ServerServiceHelper._identity);
                    bool status = SyncSiteUsers(instance, message, ticks, helper, ref processInt);
                    if (!status) return;
                    status = SyncSaleOrders(instance, message, ticks, helper, ref processInt);
                    if (!status) return;
                    status = SyncItems(instance, message, ticks, helper, ref processInt);
                    if (!status) return;
                    status = SyncBOMs(instance, message, ticks, helper, ref processInt);
                    if (!status) return;
                    status = SyncUOMs(instance, message, ticks, helper, ref processInt);
                    if (status)
                    {
                        message.IsFinish = true;
                        backgroundWorker.ReportProgress(100, message);
                    }
                }
            }
            catch
            {
                message.DisplayText = "同步数据失败,请检查网络";
                backgroundWorker.ReportProgress(0, message);
            }
        }

        void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            LabelMessage message = (LabelMessage)e.UserState;
            if (message.IsFinish) this.Close();
            if (message.ShowProcessBar)
            {
                this.progressBar.Visible = true;
                this.progressBar.Value = e.ProgressPercentage;
            }
            if (message.ShowRestartBtn) this.restartBtn.Visible = true;
            this.DisplayLabel.Text = message.DisplayText;
            this.errorLabel.Text = message.ErrorText;
        }

        private bool SyncItems(Model.Interface.ISEMIService instance, LabelMessage message,
            string ticks, SEMI.Service.ServerServiceHelper helper, ref int processInt)
        {
            try
            {
                message.DisplayText = "请勿关闭,正在同步Item数据...";
                backgroundWorker.ReportProgress(processInt, message);
                string data = instance.GetItems(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    if (data.StartsWith("SyncFailed"))
                    {
                        message.DisplayText = "Item数据同步错误,同步停止";
                        message.ErrorText = "服务端错误:" + data.Replace("SyncFailed:", "");
                        backgroundWorker.ReportProgress(processInt, message);
                        return false;
                    }
                    else
                    {
                        List<Model.Item.ItemMast> itemList =
                            Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.Item.ItemMast>>(
                            Utils.Common.EncyptHelper.DesEncypt(data, helper.key));
                        SEMI.Service.ClientServiceHelper.GetInstance().SyncItems(itemList);
                    }
                }
                message.DisplayText = "Item数据同步完成.";
                processInt += 20;
                backgroundWorker.ReportProgress(processInt, message);
                return true;
            }
            catch (Exception e)
            {
                message.DisplayText = "同步Item数据异常,请检查后重试";
                message.ErrorText = e.Message;
                backgroundWorker.ReportProgress(processInt, message);
                return false;
            }
        }

        private bool SyncBOMs(Model.Interface.ISEMIService instance, LabelMessage message,
            string ticks, SEMI.Service.ServerServiceHelper helper, ref int processInt)
        {
            try
            {
                message.DisplayText = "请勿关闭,正在同步BOM数据...";
                backgroundWorker.ReportProgress(processInt, message);
                string data = instance.GetBOMs(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    if (data.StartsWith("SyncFailed"))
                    {
                        message.DisplayText = "BOM数据同步错误,同步停止";
                        message.ErrorText = "服务端错误:" + data.Replace("SyncFailed:", "");
                        backgroundWorker.ReportProgress(processInt, message);
                        return false;
                    }
                    else
                    {
                        List<Model.BOM.BOMMast> bomList =
                            Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.BOM.BOMMast>>(
                            Utils.Common.EncyptHelper.DesEncypt(data, helper.key));
                        SEMI.Service.ClientServiceHelper.GetInstance().SyncBOMs(bomList);
                    }
                }
                message.DisplayText = "BOM数据同步完成.";
                processInt += 20;
                backgroundWorker.ReportProgress(processInt, message);
                return true;
            }
            catch (Exception e)
            {
                message.DisplayText = "同步BOM数据异常,请检查后重试";
                message.ErrorText = e.Message;
                backgroundWorker.ReportProgress(processInt, message);
                return false;
            }
        }

        private bool SyncUOMs(Model.Interface.ISEMIService instance, LabelMessage message,
            string ticks, SEMI.Service.ServerServiceHelper helper, ref int processInt)
        {
            try
            {
                message.DisplayText = "请勿关闭,正在同步UOM数据...";
                backgroundWorker.ReportProgress(processInt, message);
                string data = instance.GetUOMs(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    if (data.StartsWith("SyncFailed"))
                    {
                        message.DisplayText = "UOM数据同步错误,同步停止";
                        message.ErrorText = "服务端错误:" + data.Replace("SyncFailed:", "");
                        backgroundWorker.ReportProgress(processInt, message);
                        return false;
                    }
                    else
                    {
                        List<Model.UOM.UOMMast> uomList =
                            Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.UOM.UOMMast>>(
                            Utils.Common.EncyptHelper.DesEncypt(data, helper.key));
                        SEMI.Service.ClientServiceHelper.GetInstance().SyncUOMs(uomList);
                    }
                }
                message.DisplayText = "UOM数据同步完成.";
                processInt += 20;
                backgroundWorker.ReportProgress(processInt, message);
                return true;
            }
            catch (Exception e)
            {
                message.DisplayText = "同步UOM数据异常,请检查后重试";
                message.ErrorText = e.Message;
                backgroundWorker.ReportProgress(processInt, message);
                return false;
            }
        }

        private bool SyncSiteUsers(Model.Interface.ISEMIService instance, LabelMessage message, string ticks,
            SEMI.Service.ServerServiceHelper helper, ref int processInt)
        {
            try
            {
                message.DisplayText = "请勿关闭,正在同步用户信息数据...";
                backgroundWorker.ReportProgress(processInt, message);
                string data = instance.GetSiteUsers(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid);

                if (!string.IsNullOrWhiteSpace(data))
                {
                    if (data.StartsWith("SyncFailed"))
                    {
                        message.DisplayText = "用户数据同步错误,同步停止";
                        message.ErrorText = "服务端错误:" + data.Replace("SyncFailed:", "");
                        backgroundWorker.ReportProgress(processInt, message);
                        return false;
                    }
                    else
                    {
                        List<Model.Account.SiteUser> userList =
                            Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.Account.SiteUser>>(
                            Utils.Common.EncyptHelper.DesEncypt(data, helper.key));
                        //List<string> returnIds = 
                        SEMI.Service.ClientServiceHelper.GetInstance().SyncSiteUsers(siteGuid, userList);
                        //if (returnIds != null && returnIds.Count > 0)
                        //{
                        //    instance.UpdateSiteUsers(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid,
                        //        Utils.Common.EncyptHelper.Encypt(Utils.Common.JsonHelper.SerializeJsonString(returnIds), helper.key));
                        //}
                    }
                }
                message.DisplayText = "用户数据同步完成.";
                processInt += 20;
                backgroundWorker.ReportProgress(processInt, message);
                return true;
            }
            catch (Exception e)
            {
                message.DisplayText = "同步用户信息异常,请检查后重试";
                message.ErrorText = e.Message;
                backgroundWorker.ReportProgress(processInt, message);
                return false;
            }
        }

        private bool SyncSaleOrders(Model.Interface.ISEMIService instance, LabelMessage message, string ticks,
            SEMI.Service.ServerServiceHelper helper, ref int processInt)
        {
            try
            {
                message.DisplayText = "请勿关闭,正在同步销售订单数据...";
                backgroundWorker.ReportProgress(processInt, message);
                string data = instance.GetSalesOrders(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid);
                if (!string.IsNullOrWhiteSpace(data))
                {
                    if (data.StartsWith("SyncFailed"))
                    {
                        message.DisplayText = "销售订单数据同步错误,同步停止";
                        message.ErrorText = "服务端错误:" + data.Replace("SyncFailed:", "");
                        backgroundWorker.ReportProgress(processInt, message);
                        return false;
                    }
                    else
                    {
                        List<Model.Order.SaleOrder> orderList =
                            Utils.Common.JsonHelper.DeSerializerJsonString<List<Model.Order.SaleOrder>>(
                            Utils.Common.EncyptHelper.DesEncypt(data, helper.key));
                        orderList = SEMI.Service.ClientServiceHelper.GetInstance().SyncSaleOrders(siteGuid, orderList);
                        if (orderList != null && orderList.Count > 0)
                            instance.UpdateSaleOrders(ticks, SEMI.Service.ServerServiceHelper._identity, siteGuid,
                                Utils.Common.EncyptHelper.Encypt(Utils.Common.JsonHelper.SerializeJsonString(orderList), helper.key));
                    }
                }
                message.DisplayText = "销售订单数据同步完成.";
                processInt += 20;
                backgroundWorker.ReportProgress(processInt, message);
                return true;
            }
            catch (Exception e)
            {
                message.DisplayText = "同步销售订单数据异常,请检查后重试";
                message.ErrorText = e.Message;
                backgroundWorker.ReportProgress(processInt, message);
                return false;
            }
        }
        private void restartBtn_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
    }
}
