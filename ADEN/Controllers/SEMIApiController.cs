using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Data;
using System.Configuration;
using System.Threading.Tasks;
using System.IO;
using Utils.Common;
using Utils.Excel;


namespace ADEN.Controllers
{
    public class SEMIApiController : ApiController
    {
        #region MastData

        /// <summary>
        /// 获取用户加密的Key
        /// </summary>
        /// <param name="loginUser"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Account.LoginResponse GetUserKey([FromBody]Model.Account.LoginUser loginUser)
        {
            string key = SEMI.Account.SiteAdminHelper.GetInstance().GetEncyptUserKey(loginUser.UserID, loginUser.Password);
            return new Model.Account.LoginResponse()
            {
                Status = string.IsNullOrWhiteSpace(key) ? "error" : "ok",
                Key = key
            };
        }

        /// <summary>
        /// 获取用户主数据
        /// </summary>
        /// <param name="userKey"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Account.LoginUserMast GetUserMast([FromBody]string userKey, string language)
        {
            return SEMI.Account.SiteAdminHelper.GetInstance().GetUserMastData(userKey, language);
        }

        /// <summary>
        /// 获取用户菜单主数据
        /// </summary>
        /// <param name="userKey"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public IList<Model.Menu.MenuMast> GetUserMenuMast([FromBody]string userKey, string language)
        {
            try
            {
                return SEMI.Account.SiteAdminHelper.GetInstance().GetUserMenuMastList(userKey, language);
            }
            catch { return null; }
        }

        [HttpPost]
        public IList<Model.Site.SiteMast> GetSiteMastList([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(language, request.SiteGuid, request.BUGuid);
        }

        #endregion

        #region Item主数据
        [HttpPost]
        public Model.Table.TableMast<Model.Item.RMMast> GetTableRMDatas([FromBody] Model.Item.ItemRequest request, string language)
        {
            string type = request.Type.GetString();
            if (type.Equals("BOM")) return SEMI.MastData.ItemDataHelper.GetInstance().GetTableRMList(request.Status, request.KeyWords, false, language);
            else return SEMI.MastData.ItemDataHelper.GetInstance().GetTableRMList(request.Status, request.KeyWords, true, language);
        }

        [HttpPost]
        public Model.Table.TableMast<Model.Item.FGMast> GetTableFGDatas([FromBody] Model.Item.ItemRequest request, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().GetTableFGList(request.Status, request.KeyWords, language);
        }

        [HttpPost]
        public IList<Model.Item.FGMast> GetFGDatas([FromBody] Model.Item.ItemRequest request, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().GetFGList(request.Status, request.KeyWords, language);
        }

        [HttpPost]
        public IList<Model.Item.RMMast> GetRMDatas([FromBody] Model.Item.ItemRequest request, string language)
        {
            string type = request.Type.GetString();
            if (type.Equals("BOM")) return SEMI.MastData.ItemDataHelper.GetInstance().GetRMList(request.Status, request.KeyWords, false, language);
            else return SEMI.MastData.ItemDataHelper.GetInstance().GetRMList(request.Status, request.KeyWords, true, language);
        }

        /// <summary>
        /// 编辑或者新增Item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Common.BaseResponse EditFG([FromBody] Model.Item.FGMast item, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().ModifyFG(item, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditRM([FromBody] Model.Item.RMMast item, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().ModifyRM(item, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditFMItemClass([FromBody] Model.Table.TableMast<Model.Item.ItemClass> classData,
            string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().ModifyItemClass(classData.data);
        }

        #endregion

        #region BUItem成本价格
        /// <summary>
        /// 按公司显示成品成本价格数据
        /// </summary>
        /// <param name="request"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Table.TableMast<Model.Item.FGCostPrice> GetTableFGCostPriceDatas([FromBody] Model.Item.FGCostPriceRequest request,
            string language)
        {
            return SEMI.Cost.ItemCostHelper.GetInstance().GetTableFGCostPriceList(request.BUGuid, request.KeyWords, language);
        }

        [HttpPost]
        public string ExportFGCostPriceDatas([FromBody] Model.Table.TableMast<Model.Item.FGCostPrice> tableData, string language)
        {
            string returnUrl = string.Empty;
            if (tableData != null && tableData.data.Count > 0)
            {
                Dictionary<string, Type> columns = new Dictionary<string, Type>();
                columns.Add("ItemCode", typeof(string));
                columns.Add("ItemName", typeof(string));
                columns.Add("Price", typeof(double));
                columns.Add("PromotionPrice", typeof(double));
                columns.Add("Cost", typeof(double));
                columns.Add("LastCost", typeof(double));
                columns.Add("OtherCost", typeof(double));
                columns.Add("GMRate", typeof(double));
                columns.Add("NewGMRate", typeof(double));

                //创建DataTable
                DataTable data = Utils.Common.Functions.CreateTableStruct(columns, "FGCostPrice");
                foreach (Model.Item.FGCostPrice fgcp in tableData.data)
                {
                    data.Rows.Add(fgcp.ItemCode, fgcp.ItemName, fgcp.ItemPrice, fgcp.ItemPromotionPrice, fgcp.ItemPreviousActCost,
                        fgcp.ItemActCost, fgcp.OtherCost, fgcp.ItemPreviousActGMRate, fgcp.ItemActGMRate);
                }
                string demoFile = HttpContext.Current.Server.MapPath("~/Demos/fgcostprice.xlsx");
                string downloadPath = HttpContext.Current.Server.MapPath("~/Downloads");
                returnUrl = Utils.Excel.ExcelHelper.GetInstance().SaveDemoDataTable(data, demoFile, downloadPath, ".xlsx");
            }
            return returnUrl;
        }

        #endregion

        #region 供应商数据

        [HttpPost]
        public IList<Model.Supplier.SupplierMast> GetReceiptSupplierList([FromBody] Model.Supplier.SupplierRequest supRequest, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().GetReceiptSupplierList(language, supRequest.ReceiptDate, supRequest.SiteGuid);
        }

        [HttpPost]
        public Model.Table.TableMast<Model.Supplier.SupplierMast> GetTableSupplierMastDatas([FromBody] Model.Supplier.SupplierRequest supRequest, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetTableSupplierMastList(supRequest.Status, supRequest.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditSupplier([FromBody] Model.Supplier.SupplierMast supplierMast, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().ModifySupplier(supplierMast, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditSupplierSites([FromBody] Model.Supplier.SupplierMast supplierMast, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().ModifySupplierSites(supplierMast, language);
        }

        #endregion

        #region BU

        [HttpPost]
        public Model.Table.TableMast<Model.BU.BUMast> GetTableBUMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetTableBUList(request.KeyWords, language);
        }

        [HttpPost]
        public IList<Model.BU.BUMast> GetBUMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            if (string.IsNullOrWhiteSpace(request.BUGuid)) return SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
            else return SEMI.MastData.MastDataHelper.GetInstance().GetBUList(new List<string>() { request.BUGuid }, false);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditBU([FromBody] Model.BU.BUMast data, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().EditBU(data);
        }
        #endregion

        #region 营运点
        [HttpPost]
        public Model.Table.TableMast<Model.Site.SiteMast> GetTableSiteMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetTableSiteList(request.BUGuid, request.KeyWords, language);
        }

        [HttpPost]
        public List<Model.Site.SiteMast> GetSiteMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList();
        }

        [HttpPost]
        public Model.Common.BaseResponse EditSite([FromBody] Model.Site.SiteMast site, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().EditSite(site);
        }
        #endregion

        #region MRP

        [HttpPost]
        public Model.Table.TableMast<Model.MRP.MRPOrderItem> GetTableMRPDatas([FromBody] Model.MRP.MRPRequest request, string language)
        {
            return SEMI.MRP.MRPHelper.GetInstance().GetMRPDatas(request.SiteGuid, request.RequiredDate, language);
        }
        #endregion

        #region 采购订单
        [HttpPost]
        public Model.Table.TableMast<Model.Order.PurchaseOrder> GetTablePODatas([FromBody] Model.Order.OrderRequest request, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().GetPODatas(request.SiteGuid, request.orderDate, language);
        }
        #endregion

        #region 销售订单
        [HttpPost]
        public Model.Table.TableMast<Model.Order.SaleOrder> GetTableSODatas([FromBody] Model.Order.OrderRequest request, string language)
        {
           return SEMI.Order.OrderDataHelper.GetInstance().GetSODatas(request.maxOrderId,request.SiteGuid, request.orderDate,request.orderStatus,request.orderCode, language); 
        }

        #endregion
        //订单发货
        [HttpPost]
        public Model.Common.BaseResponse DeliverSO([FromBody] Model.Table.TableMast<Model.Order.SaleOrder> request, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().UpdateSOStatus(request.data,request.orderStatus, request.user, language);
        }
        public Model.Common.BaseResponse UpdateSOStatus([FromBody] Model.Order.OrderRequest request, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().UpdateSOStatus(request.headGuid , request.orderStatus, request.user, language);
        }

        #region 采购收货

        [HttpPost]
        public Model.Table.TableMast<Model.Order.ReceiptMast> GetTableReceiptMastDatas([FromBody] Model.Order.ReceiptRequest request, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().GetReceiptMastDatas(language, request.SiteGuid, request.SupplierGuid, request.ReceiptDate);
        }

        [HttpPost]
        public Model.Common.BaseResponse ProcessReceiptMastDatas([FromBody] Model.Table.TableMast<Model.Order.ReceiptMast> receiptMastDatas, string language)
        {
            return SEMI.Order.OrderDataHelper.GetInstance().ProcessReceiptMastDatas(receiptMastDatas.data, receiptMastDatas.user, language);
        }

        #endregion

        #region 采购价目表
        [HttpPost]
        public Model.Table.TableMast<Model.Item.RMPrice> GetTablePurchasePriceList([FromBody] Model.Item.ItemPriceRequest request, string language)
        {
            return SEMI.Price.ItemPriceHelper.GetInstance().GetTablePurchasePriceList(request.SupplierGuid, request.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditPurchasePrice([FromBody] Model.Item.RMPrice itemPrice, string language)
        {
            return SEMI.Price.ItemPriceHelper.GetInstance().EditPurchasePrice(itemPrice, language);
        }

        #endregion

        #region 销售价目表
        [HttpPost]
        public Model.Table.TableMast<Model.Item.FGPrice> GetTableSalesPriceList([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.Price.ItemPriceHelper.GetInstance().GetTableSalesPriceList(request.BUGuid, request.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditSalesPrice([FromBody]Model.Item.FGPrice itemPrice, string language)
        {
            return SEMI.Price.ItemPriceHelper.GetInstance().ModifySalesPrice(itemPrice, language);
        }
        
        //根据所选BU列示营运点GUID
        [HttpPost]
        public dynamic setPriceListSite(string language,string siteGuid,string buGuid)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(language,"",buGuid);
        }

        #endregion

        #region 促销

        [HttpPost]
        public Model.Table.TableMast<Model.Promotion.PromotionMast> GetTablePromotionDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.Promotion.ItemPromotionHelper.GetInstance().GetTablePromotionDatas(request.BUGuid, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditPromotionMast([FromBody] Model.Promotion.PromotionMast data, string language)
        {
            return SEMI.Promotion.ItemPromotionHelper.GetInstance().ModifyPromotionMast(data);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditPromotionItem([FromBody] Model.Table.TableMast<Model.Promotion.PromotionItem> data,
            string language)
        {
            return SEMI.Promotion.ItemPromotionHelper.GetInstance().ModifyPromotionItems(data.data);
        }

        #endregion

        #region BOM
        [HttpPost]
        public Model.Common.BaseResponse EditBOM([FromBody]Model.BOM.BOMMast bomMast, string language)
        {
            return SEMI.BOM.BOMHelper.GetInstance().EditBOM(bomMast, language);
        }

        #endregion

        #region 微信报表
        [HttpPost]
        public Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageData> GetTableWeChatReportSqlMastDatas(
            [FromBody] Model.WeChatSqlMessage.WeChatSqlRequest request, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().GetTableWeChatReportSqlMastList(request.Status, request.KeyWords, language);
        }

        [HttpPost]
        public Model.Table.TableMast<Model.WeChatSqlMessage.WeChatSqlMessageJob> GetTableWeChatReportJobMastDatas(
            [FromBody] Model.WeChatSqlMessage.WeChatSqlRequest request, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance()
                .GetTableWeChatReportJobMastList(request.Status, request.KeyWords, language);
        }

        [HttpPost]
        public List<Model.WeChatSqlMessage.WeChatSqlMessageData> GetWeChatReportSqlMastDatas(
            [FromBody] Model.WeChatSqlMessage.WeChatSqlRequest request, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance()
                .GetWeChatReportSqlMastList(request.Status, request.KeyWords, request.GUID, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditWeChatReportSql([FromBody] Model.WeChatSqlMessage.WeChatSqlMessageData data, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().ModifyWeChatSql(data, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditWeChatReportJob([FromBody] Model.WeChatSqlMessage.WeChatSqlMessageJob data, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().ModifyWeChatJob(data, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse WeChatReportRunJob([FromBody] Model.WeChatSqlMessage.WeChatSqlMessageJobRun rdata, string language)
        {
            return SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().RunWeChatJob(rdata.JobID, rdata.EmployeeID);
        }

        #endregion

        #region Calendar
        [HttpPost]
        public Model.Table.TableMast<Model.Calendar.CalendarMast> GetTableCalendarMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetTableCalendarMastList(request.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditCalendar([FromBody] Model.Calendar.CalendarMast calendarMast, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().ModifyCalendar(calendarMast);
        }
        #endregion

        #region CustomerData
        [HttpPost]
        public Model.Table.TableMast<Model.CustomerData.CustomerDataMast> GetTableCustomerDatas(Model.Common.BaseRequest request, string language)
        {
            return SEMI.CustomerData.CustomerDataHelper.GetInstance().GetTableCustomerDatas(request.Status, request.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditCustomerData(Model.CustomerData.CustomerDataMast data, string language)
        {
            return SEMI.CustomerData.CustomerDataHelper.GetInstance().ModifyCustomerDataData(data, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse RunCustomerData(Model.CustomerData.CustomerDataMast data, string language)
        {
            Model.Common.BaseResponse resp = new Model.Common.BaseResponse();
            try { new SEMI.Schdule.CustomerDataTask().RunByCustomerDataID(data.ID); resp.Status = "ok"; }
            catch (Exception e)
            {
                resp.Status = "error";
                resp.Msg = e.Message;
            }
            return resp;
        }
        #endregion

        #region 上传
        public async Task<Model.Upload.UploadResponse> UploadExcelData(string type, string language)
        {
            Model.Upload.UploadResponse resp = new Model.Upload.UploadResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(type)) throw new Exception("Upload Type not found.");
                if (!Request.Content.IsMimeMultipartContent()) throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                string filePath = HttpContext.Current.Server.MapPath("~/Uploads");
                ADEN.Models.ReadMultipartFormDataStreamProvider
                    provider = new ADEN.Models.ReadMultipartFormDataStreamProvider(filePath);
                await Request.Content.ReadAsMultipartAsync(provider);
                if (provider.FileData.Count != 1) throw new HttpResponseException(HttpStatusCode.Forbidden);
                //处理Excel文件
                string resultUrl = SEMI.Upload.UploadDatasHelper.GetInstance().Process(type, provider.FileData[0].LocalFileName);
                resp.ResultUrl = resultUrl;
                resp.Status = "ok";
                return resp;
            }
            catch (Exception e)
            {
                resp.Status = "error";
                resp.Msg = e.Message;
                return resp;
            }
        }

        /// <summary>
        /// 上传单一图片,返回图片名
        /// </summary>
        /// <param name="picFile">图片主文件名</param>
        /// <param name="tag">图片子文件名</param>
        /// <param name="language"></param>
        /// <returns></returns>
        public async Task<Model.Upload.UploadResponse> UploadFGImageData(string picFile, string tag, string action,
            string field, string language)
        {
            Model.Upload.UploadResponse resp = new Model.Upload.UploadResponse();
            try
            {
                if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(picFile))
                    throw new Exception("Params error, tag or picFile");
                if (!Request.Content.IsMimeMultipartContent()) throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
                string filePath = ConfigurationManager.AppSettings["PictureRoot"].GetString();
                
                if (string.IsNullOrWhiteSpace(filePath)) throw new Exception("Image path is null");
                if (picFile.Contains("/"))
                {
                    string parFile = picFile.Substring(0, picFile.IndexOf("/"));


                    string subFile = picFile.Substring(picFile.IndexOf("/")+1);

                    filePath = System.IO.Path.Combine(filePath, parFile,subFile, tag);
                }
                else
                {
                    filePath = (System.IO.Path.Combine(filePath, picFile, tag)).Replace('\\','/');
                }
                if (!System.IO.Directory.Exists(filePath)) throw new Exception("Path not found in server.");
                bool setFileNames = action.Equals("file") ? true : false;
                ADEN.Models.ReadMultipartFormDataStreamProvider
                    provider = new ADEN.Models.ReadMultipartFormDataStreamProvider(filePath, setFileNames);
                await Request.Content.ReadAsMultipartAsync(provider);
                //目前一个个提交处理
                if (provider.FileData.Count != 1) throw new HttpResponseException(HttpStatusCode.Forbidden);
                if (action.Equals("url")) resp.FileName = System.IO.Path.GetFileName(provider.FileData[0].LocalFileName);
                if (action.Equals("file")) 
                    SEMI.MastData.ItemDataHelper.GetInstance().ModifyFGPics(picFile, tag, field, provider.GetFormDataFileNames().First());
                resp.Status = "ok";
                return resp;
            }
            catch (Exception e)
            {
                resp.Status = "error";
                resp.Msg = e.Message;
                return resp;
            }
        }

        #endregion

        #region POS销售报告
        [HttpPost]
        public Model.Table.TableMast<Model.MBSales.MBSalesMast> GetKeyTableMastDatas([FromBody] Model.MBSales.MBSalesRequest supRequest, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetKeyTableMastList(supRequest.StallEntity, supRequest.StartDate, supRequest.EndDate, supRequest.KeyWords, supRequest.All, supRequest.Title, language);
        }

        #endregion

        #region POS会员消费
        [HttpPost]
        public string ExportMB([FromBody] Model.Table.TableMast<Model.MBSales.MBSalesMast> tableData, string language)
        {
            string returnUrl = string.Empty;
            if (tableData != null && tableData.data.Count > 0)
            {
                Dictionary<string, Type> columns = new Dictionary<string, Type>();
                columns.Add("CounterNo", typeof(string));
                columns.Add("CounterName", typeof(string));
                columns.Add("cardNo", typeof(string));
                columns.Add("mbName", typeof(string));
                columns.Add("TypeName", typeof(string));
                columns.Add("Year", typeof(string));
                columns.Add("Month", typeof(string));
                columns.Add("operDate", typeof(string));
                columns.Add("Time", typeof(string));
                columns.Add("pluno", typeof(string));
                columns.Add("foodname", typeof(string));
                columns.Add("salePrice", typeof(decimal));
                columns.Add("saleQty", typeof(decimal));
                columns.Add("saleAmt", typeof(decimal));
                columns.Add("DiscAmt", typeof(decimal));
                DataTable data = Utils.Common.Functions.CreateTableStruct(columns, "MBConsumption");
                foreach (Model.MBSales.MBSalesMast mb in tableData.data)
                {
                    data.Rows.Add(mb.CounterNo, mb.CounterName, mb.cardNo, mb.mbName, mb.TypeName, mb.Year, mb.Month, mb.operDate, mb.Time, mb.pluno, mb.foodname,
                        mb.salePrice, mb.saleQty, mb.saleAmt, mb.DiscAmt);
                }
                string demoFile = HttpContext.Current.Server.MapPath("~/Demos/mbconsumption.xlsx");
                string downloadPath = HttpContext.Current.Server.MapPath("~/Downloads");
                returnUrl = Utils.Excel.ExcelHelper.GetInstance().SaveDemoDataTable(data, demoFile, downloadPath, ".xlsx");
            }
            return returnUrl;
        }
        #endregion
        [HttpPost]
        public Model.Table.TableMast<Model.Calendar.CalendarMast> GetTableSiteTimeSetMastDatas([FromBody] Model.Common.BaseRequest request, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetTableSiteTimeSetMastList(request.KeyWords, language);
        }

        [HttpPost]
        public Model.Common.BaseResponse EditSiteTime([FromBody] Model.Calendar.CalendarMast calendarMast, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().ModifySiteTime(calendarMast);
        }

        #region 微信小程序
        [HttpPost]
        //wechatId, userName, password
        public dynamic BindWechatLoginUser([FromBody]Dictionary<string, string> user)
        {
            try
            {
                bool rst = SEMI.WechatUser.WechatUserHelper.GetInstance().BindWechatLoginUser(
                    user["wechatId"], user["userName"], user["password"]);

                if (!rst) throw new Exception("用户名/密码错误");

               return new
               {
                   errMsg = "",
                   data = SEMI.WechatUser.WechatUserHelper.GetInstance().GetWechatMenuList(user["wechatId"])
               };
            }
            catch(Exception e)
            {
                return new { errMsg = e.Message };
            }
        }

        [HttpPost]
        //wechatId
        public dynamic CheckWechatLoginUser([FromBody]Dictionary<string, string> user)
        {
            try
            {
                bool rst = SEMI.WechatUser.WechatUserHelper.GetInstance().CheckWechatLoginUser(user["wechatId"]);
                
                if (!rst) throw new Exception("登录失败");

                return new
                {
                    errMsg = "",
                    data = SEMI.WechatUser.WechatUserHelper.GetInstance().GetWechatMenuList(user["wechatId"])
                };
            }
            catch (Exception e)
            {
                return new { errMsg = e.Message };
            }
        }

        [HttpPost]
        public dynamic CheckSOStatus([FromBody] Model.Order.OrderRequest request, string language)
        {
            try
            {
                string Msg = "";

                List<Model.Order.SaleOrder> data = SEMI.Order.OrderDataHelper.GetInstance().GetSOList("", "", "", request.orderStatus, request.orderCode, 
                    language);

                if (data == null || data.Count == 0)
                    Msg = "无订单或订单已完成";
                else
                {
                   
                    DateTime now = DateTime.Now;
                    int i = (DateTime.Compare(now,DateTime.Parse(data[0].RequiredDate)));

                        if (DateTime.Compare(now, DateTime.Parse(data[0].RequiredDate)) < 0)
                            Msg = "未到发货时间";
                        else if (now.Date > DateTime.Parse(data[0].RequiredDate).Date || 
                            (!string.IsNullOrWhiteSpace(data[0].DeliveryEndTime) && 
                            now.TimeOfDay > DateTime.Parse(data[0].DeliveryEndTime).TimeOfDay))
                            Msg = "订单已过期";
                }
                return new
                {
                    msg = Msg,
                    content = data
                };
            }
            catch(Exception ex)
            {
                return new { msg = ex.Message };
            }
        }

        #endregion

        #region SPD Report
        [HttpPost]
        public Model.Table.TableMast<Model.SPD.SPDMast> GetSPD([FromBody] Model.SPD.SPDRequest supRequest, string language)
        {
            return SEMI.MastData.MastDataHelper.GetInstance().GetSPD(supRequest.empCode, supRequest.startdate, supRequest.enddate, supRequest.all, supRequest.group, supRequest.getsite, language);
        }
        #endregion
        
        #region ExportSPD
        [HttpPost]
        public string ExportSPD([FromBody] Model.Table.TableMast<Model.SPD.SPDMast> tableData, string language)
        {
            string returnUrl = string.Empty;
            if (tableData != null && tableData.data.Count > 0)
            {
                Dictionary<string, Type> columns = new Dictionary<string, Type>();
                columns.Add("code", typeof(string));
                columns.Add("Year", typeof(string));
                columns.Add("Month", typeof(string));
                columns.Add("Date", typeof(string));
                columns.Add("Supplier", typeof(string));
                columns.Add("Consumer", typeof(string));
                columns.Add("ItemCode", typeof(string));
                columns.Add("ItemName", typeof(string));
                columns.Add("Qty", typeof(string));
                columns.Add("Turnover", typeof(string));
                columns.Add("NetTurnover", typeof(string));
                columns.Add("Cost", typeof(string));
                columns.Add("GM", typeof(string));
                columns.Add("NetGM", typeof(string));
                DataTable data = Utils.Common.Functions.CreateTableStruct(columns, "SPD");
                foreach (Model.SPD.SPDMast spd in tableData.data)
                {
                    data.Rows.Add(spd.code, spd.Year, spd.Month, spd.Date, spd.Supplier, spd.Consumer, spd.ItemCode,
                        spd.ItemName, spd.Qty, spd.Turnover, spd.NetTurnover, spd.Cost, spd.GM, spd.NetGM);
                }
                string demoFile = HttpContext.Current.Server.MapPath("~/Demos/SPD.xlsx");
                string downloadPath = HttpContext.Current.Server.MapPath("~/Downloads");
                returnUrl = Utils.Excel.ExcelHelper.GetInstance().SaveDemoDataTable(data, demoFile, downloadPath, ".xlsx");
            }
            return returnUrl;

        }
        #endregion

        #region ExportSiteSPD
        [HttpPost]
        public string ExportSiteSPD([FromBody] Model.Table.TableMast<Model.SPD.SPDMast> tableData, string language)
        {
            string returnUrl = string.Empty;
            if (tableData != null && tableData.data.Count > 0)
            {
                Dictionary<string, Type> columns = new Dictionary<string, Type>();
                columns.Add("code", typeof(string));
                columns.Add("Date", typeof(string));
                columns.Add("Consumer", typeof(string));
                columns.Add("ItemCode", typeof(string));
                columns.Add("ItemName", typeof(string));
                columns.Add("Qty", typeof(string));
                columns.Add("Turnover", typeof(string));

                DataTable data = Utils.Common.Functions.CreateTableStruct(columns, "SPD");
                foreach (Model.SPD.SPDMast spd in tableData.data)
                {
                    data.Rows.Add(spd.code, spd.Date, spd.Consumer, spd.ItemCode,
                        spd.ItemName, spd.Qty, spd.Turnover);
                }
                string demoFile = HttpContext.Current.Server.MapPath("~/Demos/SiteReport.xlsx");
                string downloadPath = HttpContext.Current.Server.MapPath("~/Downloads");
                returnUrl = Utils.Excel.ExcelHelper.GetInstance().SaveDemoDataTable(data, demoFile, downloadPath, ".xlsx");
            }
            return returnUrl;

        }
        #endregion
        [HttpPost]
        public Model.Table.TableMast<Model.Item.FGMast> GetTableMenuDatas([FromBody] Model.Item.ItemRequest request, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().GetTableMenuList(request.itemClass, request.startDate, request.weekDay, request.KeyWords,request.SiteGuid, language);
            
        }
        /// <summary>
        /// 编辑或者新增Item
        /// </summary>
        /// <param name="item"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Common.BaseResponse EditMenuItem([FromBody] Model.Item.FGMast item, string language)
        {
            return SEMI.MastData.ItemDataHelper.GetInstance().ModifyMenuItem(item, language);
        }
    }
}