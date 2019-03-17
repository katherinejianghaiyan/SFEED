using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.Mvc;
using Utils.Common;
using System.Media;
using System.IO;
using System.Text;
using System.Data;


namespace ADEN.Controllers
{
    public class SEMIController : Controller
    {
        private const string usrCookie = "userkey";

        /// <summary>
        /// 传入语言为空,则获取客户端语言,如获取失败,则返回默认en
        /// </summary>
        /// <param name="language">传入语言代码</param>
        /// <returns>小写语言代码</returns>
        [NonAction]
        private string GetClientLanguage(string language)
        {
            if (!string.IsNullOrWhiteSpace(language))
            {
                if (language.ToLower().Contains("zh")) return "zh";
                else return "en";
            }
            else
            {
                if (Request.UserLanguages.Length > 0)
                {
                    if (Request.UserLanguages[0].GetString().ToLower().Contains("zh"))
                        return "zh";
                    else return "en";
                }
                else return "en";
            }
        }

        /// <summary>
        /// 创建客户端Cookie
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        [NonAction]
        private HttpCookie GetCookie(string name, string value, int seconds)
        {
            HttpCookie cookie = new HttpCookie(name);
            cookie.Value = value;
            cookie.Expires = DateTime.Now.AddSeconds(seconds);
            return cookie;
        }

        /// <summary>
        /// 获取错误对象
        /// </summary>
        /// <param name="type">对象类型, HTML, TEXT, MODAL</param>
        /// <param name="useLayout">是否继承Shared中的_Layout.cshtml</param>
        /// <param name="code">错误代码</param>
        /// <param name="title">错误内容标题</param>
        /// <param name="msg">错误内容</param>
        /// <returns></returns>
        [NonAction]
        private Model.Common.ErrorMast GetErrorMast(string type, string code, string title, string msg)
        {
            return new Model.Common.ErrorMast()
            {
                ErrorType = type.ToUpper(),
                ErrorMsg = msg,
                ErrorCode = code,
                ErrorTitle = title
            };
        }

        /// <summary>
        /// 页面错误控制器,传递ErrorMast对象并返回Shared中的Error视图
        /// </summary>
        /// <returns>View</returns>
        public ActionResult PageError()
        {
            Model.Common.ErrorMast error = new Model.Common.ErrorMast();
            error.ErrorCode = Request.QueryString["code"].GetString();
            error.ErrorType = "HTML";
            error.ErrorTitle = "Oops! " + (error.ErrorCode.Equals("404") ? "页面没有找到. Page not found." : "系统错误. Something went wrong.");
            error.ErrorMsg = "<p>请刷新页面重新尝试,如仍旧提示错误,请报告管理员.</p><p>"
                  + "Please refresh the page again, if it still get wrong, please report to administrator.</p>";
            return View("Error", error);
        }

        public void WeChatCallBack(string langauge)
        {
            string token = ConfigurationManager.AppSettings["SEMIToken"].GetString();
            string aesKey = ConfigurationManager.AppSettings["SEMIAESKey"].GetString();
            WeChat.Entrance.CorpEntrance.GetInstance().CallBackResponse(HttpContext, token, aesKey,
                SEMI.Service.WeChatService.GetInstance());
        }

        public void UpdateClient(string language)
        {
            try
            {
                string ticks = Request["timestamp"].GetString();
                string key = Request["key"].GetString();
                if (string.IsNullOrWhiteSpace(ticks) || string.IsNullOrWhiteSpace(key)) return;
                string filePath = Server.MapPath("~/UpdateClient");
                if (string.IsNullOrWhiteSpace(filePath)) return;
                key = Utils.Common.EncyptHelper.DesEncypt(key, ticks.Substring(ticks.Length - 8, 8));
                string[] arr = key.Split('/');
                if (arr.Length != 2) return;
                int version = arr[0].ToInt();
                if (version.Equals(0)) return;
                key = arr[1];
                if (key.Equals(string.Empty)) return;
                int clientVersion = SEMI.MastData.MastDataHelper.GetInstance().GetSiteClientVersion(key);
                if (clientVersion.Equals(0) || clientVersion <= version) return;
                filePath = System.IO.Path.Combine(filePath, "SFEEDClient_" + clientVersion + ".data");
                if (!System.IO.File.Exists(filePath)) return;
                Response.BufferOutput = true;
                Response.Clear();
                Response.ContentType = "application/download";
                string downFile = System.IO.Path.GetFileName(filePath);
                string EncodeFileName = HttpUtility.UrlEncode(downFile, System.Text.Encoding.UTF8);
                Response.AddHeader("Content-Disposition", "attachment;filename=" + EncodeFileName + ";");
                Response.BinaryWrite(System.IO.File.ReadAllBytes(filePath));
                Response.Flush();
            }
            catch { }
        }

        /// <summary>
        /// 系统登录页面Action,仅允许Get请求方式获取该页面
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>SEMI中的Login视图</returns>
        [HttpGet]
        public ActionResult Login(string language)
        {
            try
            {
                #region 设置页面标签内容(zh,en)

                ViewBag.BodyClass = "hold-transition login-page";
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.Title = mananger.GetString(thisLanguage + "_LabelLoginTitle");
                ViewBag.LabelAdmin = mananger.GetString(thisLanguage + "_LabelAdmin");
                ViewBag.LabelUserID = mananger.GetString(thisLanguage + "_LabelUserID");
                ViewBag.LabelPassword = mananger.GetString(thisLanguage + "_LabelPassword");
                ViewBag.LabelLogin = mananger.GetString(thisLanguage + "_LabelLogin");
                ViewBag.LabelForgotPassword = mananger.GetString(thisLanguage + "_LabelForgotPassword");
                ViewBag.AlertUserID = mananger.GetString(thisLanguage + "_AlertUserID");
                ViewBag.AlertPassword = mananger.GetString(thisLanguage + "_AlertPassword");
                ViewBag.AlertUserIDPassword = mananger.GetString(thisLanguage + "_AlertUserIDPassword");
                ViewBag.AlertAjaxError = mananger.GetString(thisLanguage + "_AlertAjaxError");
                if (thisLanguage.Equals("zh"))
                    ViewBag.LanguageOptions = "<option value=\"zh\" selected>中文</option>"
                        + "<option value=\"en\">English</option>";
                else ViewBag.LanguageOptions = "<option value=\"en\" selected>English</option>"
                    + "<option value=\"zh\">中文</option>";

                #endregion

                return View();
            }
            catch (Exception e) { return View("Error", GetErrorMast("HTML", "500", "Login Page", e.Message)); }
        }

        /// <summary>
        /// 主界面,登录成功后跳转此界面
        /// </summary>
        /// <param name="language">语言代码</param>
        /// <returns>SEMI目录中的Main视图</returns>
        public ActionResult Main(string language)
        {
            string thisLanguage = GetClientLanguage(language);
            try
            {
                string userKey = Request.Form["userKey"].GetString();
                if (userKey.Equals(string.Empty))
                {
                    if (Request.Cookies.Get(usrCookie) != null && !string.IsNullOrWhiteSpace(Request.Cookies.Get(usrCookie).Value))
                        userKey = Request.Cookies.Get(usrCookie).Value.GetString();
                    else return RedirectToAction("Login", new { language = thisLanguage });
                }
                Model.Account.LoginUserMast userMast = SEMI.Account.SiteAdminHelper.GetInstance().GetUserMastData(userKey, language);
                if (Request.Cookies.Get(usrCookie) != null)
                {
                    Response.Cookies.Remove(usrCookie);
                    Request.Cookies.Remove(usrCookie);
                }
                if (userMast == null) return RedirectToAction("Login", new { language = thisLanguage });
                else
                {
                    Response.Cookies.Add(GetCookie(usrCookie, userKey, 86400));
                    ViewBag.BodyClass = "hold-transition skin-red fixed sidebar-mini"; //sidebar-collapse  
                    ViewBag.Title = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelMainTitle");
                    ViewBag.LabelInfo = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelInfo");
                    ViewBag.LabelClose = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelClose");
                    ViewBag.LabelYes = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelYes");
                    ViewBag.LabelNo = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelNo");
                    ViewBag.LabelConfirmed = ADEN.Properties.Resources.ResourceManager.GetString(thisLanguage + "_LabelConfirmed");
                    ViewBag.Language = thisLanguage;
                    ViewBag.UserKey = userKey;
                    ViewBag.MenuList = SEMI.Account.SiteAdminHelper.GetInstance().GetUserMenuMastList(userKey, thisLanguage);
                    return View(userMast);
                }
            }
            catch { return RedirectToAction("Login", new { language = thisLanguage }); } //Main页面错误直接返回登录页面
        }


        #region Ajax请求页面Html,默认传递3个加密参数,UserKey,SiteKey,BUKey,可以通过3个参数进行2次验证

        #region 主数据维护:RM,FG Item
        #region 主数据维护:RM,FG Item
        /// <summary>
        /// 获取成品维护数据界面
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult FGItem(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.Type = "FG";
                ViewBag.Permission = Request.Form["Permission"].GetString();

                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                string UserID = Utils.Common.EncyptHelper.DesEncypt(Request.Form["UserKey"].GetString());

                Boolean siteUser = false;
                if (!string.IsNullOrWhiteSpace(siteGuid))
                {
                    siteUser = true;
                }

                ViewBag.siteUser = siteUser;
                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelDishSize = mananger.GetString(thisLanguage + "_LabelDishSize");
                ViewBag.LabelContainer = mananger.GetString(thisLanguage + "_LabelContainer");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelWeight = mananger.GetString(thisLanguage + "_LabelWeight");
                ViewBag.LabelCreateTime = mananger.GetString(thisLanguage + "_LabelCreateTime");
                ViewBag.LabelBOMUpload = mananger.GetString(thisLanguage + "_LabelBOMUpload");
                ViewBag.LabelPicUpload = mananger.GetString(thisLanguage + "_LabelPicUpload");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelOtherCost = mananger.GetString(thisLanguage + "_LabelOtherCost");


                #endregion

                return PartialView("Item");
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:FGItem", e.Message)); }
        }

        [HttpPost]
        public ActionResult FGItemClass(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.ClassList = SEMI.MastData.ItemDataHelper.GetInstance().GetItemClass("");
                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelAdd = mananger.GetString(thisLanguage + "_LabelAdd");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("TEXT", "500", "Controller:FGItemClass", e.Message)); }
        }

        /// <summary>
        /// 获取原材料维护数据界面
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RMItem(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.Type = "RM";

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelLoss = mananger.GetString(thisLanguage + "_LabelLoss");
                ViewBag.LabelSpec = mananger.GetString(thisLanguage + "_LabelSpec");
                ViewBag.LabelPurchaseUnit = mananger.GetString(thisLanguage + "_LabelPurchaseUnit");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelCreateTime = mananger.GetString(thisLanguage + "_LabelCreateTime");
                ViewBag.LabelPurchasePolicy = mananger.GetString(thisLanguage + "_LabelPurchasePolicy");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelOnDemand = mananger.GetString(thisLanguage + "_LabelOnDemand");
                ViewBag.LabelNoPurchase = mananger.GetString(thisLanguage + "_LabelNoPurchase");


                #endregion

                return PartialView("Item");
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:RMItem", e.Message)); }
        }

        /// <summary>
        /// 新增,修改Item界面
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult RMItemDetail(string language)
        {
            try
            {
                string type = Request.Form["Type"].GetString();
                string guid = Request.Form["ItemGuid"].GetString();
                if (string.IsNullOrWhiteSpace(type) || !type.Equals("RM"))
                    throw new Exception("Get ItemType failed");
                Model.Item.RMMast itemMast = null;
                string thisLanguage = GetClientLanguage(language);
                if (!guid.Equals(string.Empty))
                {
                    itemMast = SEMI.MastData.ItemDataHelper.GetInstance().GetRM(thisLanguage, guid);
                    if (itemMast == null) throw new Exception("get Item failed,Guid=" + guid);
                }
                ViewBag.ItemMast = itemMast;
                List<Model.UOM.UOMMast> uomMast = SEMI.MastData.MastDataHelper.GetInstance()
                    .GetItemUOMList(language, itemMast == null ? "" : itemMast.ItemUnitGuid);
                if (uomMast == null || uomMast.Count == 0) throw new Exception("get UOM failed");
                ViewBag.Language = thisLanguage;
                ViewBag.Type = type;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelSell = mananger.GetString(thisLanguage + "_LabelSell");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelLoss = mananger.GetString(thisLanguage + "_LabelLoss");
                ViewBag.LabelSpec = mananger.GetString(thisLanguage + "_LabelSpec");
                ViewBag.LabelPurchaseUnit = mananger.GetString(thisLanguage + "_LabelPurchaseUnit");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelPurchasePolicy = mananger.GetString(thisLanguage + "_LabelPurchasePolicy");
                ViewBag.LabelOnDemand = mananger.GetString(thisLanguage + "_LabelOnDemand");
                ViewBag.LabelNoPurchase = mananger.GetString(thisLanguage + "_LabelNoPurchase");

                #endregion

                return PartialView(uomMast);
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:ItemDetail", e.Message)); }
        }

        [HttpPost]
        public ActionResult FGItemDetail(string language)
        {
            try
            {
                ViewBag.Permission = Request.Form["Permission"].GetString();
                string type = Request.Form["Type"].GetString();
                string guid = Request.Form["ItemGuid"].GetString();
                if (string.IsNullOrWhiteSpace(type) || !type.Equals("FG")) throw new Exception("Get ItemType or ItemAction failed");
                Model.Item.FGMast itemMast = null;
                string thisLanguage = GetClientLanguage(language);
                if (!guid.Equals(string.Empty))
                {
                    itemMast = SEMI.MastData.ItemDataHelper.GetInstance().GetFG(thisLanguage, guid);
                    if (itemMast == null) throw new Exception("Get Item failed,Guid=" + guid);
                }
                ViewBag.ItemMast = itemMast;
                List<Model.Dict.DictGroup> itemDicts = SEMI.MastData.ItemDataHelper.GetInstance().GetItemDict((itemMast == null ? null : itemMast.ItemProperies));
                if (itemDicts == null) itemDicts = new List<Model.Dict.DictGroup>();
                List<Model.Item.ItemClass> itemClasses = SEMI.MastData.ItemDataHelper.GetInstance().GetItemClass("");
                if (itemClasses == null) itemClasses = new List<Model.Item.ItemClass>();
                ViewBag.ItemClasses = itemClasses;
                ViewBag.Language = thisLanguage;
                ViewBag.Type = type;
                ViewBag.PicUrl = ConfigurationManager.AppSettings["PictureUrl"].GetString();
                ViewBag.PicFile = ConfigurationManager.AppSettings["PictureFile"].GetString();

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelBuy = mananger.GetString(thisLanguage + "_LabelBuy");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelDishSize = mananger.GetString(thisLanguage + "_LabelDishSize");
                ViewBag.LabelContainer = mananger.GetString(thisLanguage + "_LabelContainer");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelMastData = mananger.GetString(thisLanguage + "_LabelMastData");
                ViewBag.LabelTips = mananger.GetString(thisLanguage + "_LabelTips");
                ViewBag.LabelCooking = mananger.GetString(thisLanguage + "_LabelCooking");
                ViewBag.LabelInstruction = mananger.GetString(thisLanguage + "_LabelInstruction");
                ViewBag.LabelPropery = mananger.GetString(thisLanguage + "_LabelPropery");
                ViewBag.LabelPicture1 = mananger.GetString(thisLanguage + "_LabelPicture1");
                ViewBag.LabelPicture2 = mananger.GetString(thisLanguage + "_LabelPicture2");
                ViewBag.LabelPicture3 = mananger.GetString(thisLanguage + "_LabelPicture3");
                ViewBag.LabelOtherCost = mananger.GetString(thisLanguage + "_LabelOtherCost");

                #endregion

                return PartialView(itemDicts);
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:FGDetail", e.Message));
            }
        }

        [HttpPost]
        public ActionResult UploadFGPics(string language)
        {
            try
            {          
                string thisLanguage = GetClientLanguage(language);
                ViewBag.PicUrl = ConfigurationManager.AppSettings["PictureUrl"].GetString();
                ViewBag.PicFile = ConfigurationManager.AppSettings["PictureFile"].GetString();
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelPicUpload = mananger.GetString(thisLanguage + "_LabelPicUpload");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelPicture1 = mananger.GetString(thisLanguage + "_LabelPicture1");
                ViewBag.LabelPicture2 = mananger.GetString(thisLanguage + "_LabelPicture2");
                ViewBag.LabelPicture3 = mananger.GetString(thisLanguage + "_LabelPicture3");

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:FGDetail", e.Message));
            }
        }

        #endregion

        #region FG Cost and Price

        [HttpPost]
        public ActionResult FGItemCostPriceView(string language)
        {
            try
            {
                string BUKey = Request.Form["BUKey"].GetString();
                if (string.IsNullOrWhiteSpace(BUKey)) throw new Exception("BUKey not found");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.Type = "FG";
                List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance()
                    .GetBUList(new List<string>() { Utils.Common.EncyptHelper.DesEncypt(BUKey) }, true);
                if (BUList == null) throw new Exception("BUGuid not found.");
                ViewBag.BUList = BUList;
                #region 设置页面标签内容(zh,en)
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelType = mananger.GetString(thisLanguage + "_LabelType");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelCurrentMaterialCost = mananger.GetString(thisLanguage + "_LabelCurrentMaterialCost");
                ViewBag.LabelLastMaterialCost = mananger.GetString(thisLanguage + "_LabelLastMaterialCost");
                ViewBag.LabelOtherCost = mananger.GetString(thisLanguage + "_LabelOtherCost");
                ViewBag.LabelCurrentGMRate = mananger.GetString(thisLanguage + "_LabelCurrentGMRate");
                ViewBag.LabelLastGMRate = mananger.GetString(thisLanguage + "_LabelLastGMRate");
                ViewBag.LabelCalDate = mananger.GetString(thisLanguage + "_LabelCalDate");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelPromotionPrice = mananger.GetString(thisLanguage + "_LabelPromotionPrice");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelExport = mananger.GetString(thisLanguage + "_LabelExport");
                ViewBag.LabelNoChosedRecord = mananger.GetString(thisLanguage + "_LabelNoChosedRecord");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:BUFGItem", e.Message)); }
        }

        #endregion

        #region Sales

        [HttpPost]
        public ActionResult SalesOrder(string language)
        {
            try
            {
                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                string UserID = Utils.Common.EncyptHelper.DesEncypt(Request.Form["UserKey"].GetString());

                if (siteGuid.Equals(string.Empty) && BUGuid.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                string thisLanguage = GetClientLanguage(language);
                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(thisLanguage, siteGuid, BUGuid);
                if (siteList == null || siteList.Count == 0) throw new Exception("get Site failed.");

                ViewBag.UserID = UserID;
                ViewBag.SiteList = siteList;
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelQuery = mananger.GetString(thisLanguage + "_LabelQuery");
                ViewBag.LabelDeliver = mananger.GetString(thisLanguage + "_LabelDeliver");
                ViewBag.LabelOrderNumber = mananger.GetString(thisLanguage + "_LabelOrderNumber");
                ViewBag.LabelUserName = mananger.GetString(thisLanguage + "_LabelUserName");
                ViewBag.LabelWechat = mananger.GetString(thisLanguage + "_LabelWechat");
                ViewBag.LabelMsgHint = mananger.GetString(thisLanguage + "_LabelMsgHint");
                ViewBag.LabelOrderDate = mananger.GetString(thisLanguage + "_LabelOrderDate");
                ViewBag.LabelOrderAmt = mananger.GetString(thisLanguage + "_LabelOrderAmt");
                ViewBag.LabelDetail = mananger.GetString(thisLanguage + "_LabelDetail");
                ViewBag.LabelDeliverOK = mananger.GetString(thisLanguage + "_LabelDeliverOK");
                ViewBag.LabelDeliverNo = mananger.GetString(thisLanguage + "_LabelDeliverNo");
                ViewBag.LabelPrint = mananger.GetString(thisLanguage + "_LabelPrint");
                ViewBag.LabelAllDeliverStatus = mananger.GetString(thisLanguage + "_LabelAllDeliverStatus");
                ViewBag.LabelRequireDate = mananger.GetString(thisLanguage + "_LabelRequireDate");
                ViewBag.LabelPaymentMethod = mananger.GetString(thisLanguage + "_LabelPaymentMethod");
                ViewBag.LabelDinnerType = mananger.GetString(thisLanguage + "_LabelDinnerType");

                ViewBag.LabelSentOK = mananger.GetString(thisLanguage + "_LabelSentOK");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelNoChosedRecord = mananger.GetString(thisLanguage + "_LabelNoChosedRecord");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                ViewBag.LabelComment = mananger.GetString(thisLanguage + "_LabelComment");
                ViewBag.LabelMobileNbr = mananger.GetString(thisLanguage + "_LabelMobileNbr");
                ViewBag.LabelDepartment = mananger.GetString(thisLanguage + "_LabelDepartment");
                ViewBag.LabelSection = mananger.GetString(thisLanguage + "_LabelSection");
                ViewBag.LabelRegularRefresh = mananger.GetString(thisLanguage + "_LabelRegularRefresh");
                ViewBag.LabelShipToAddr = mananger.GetString(thisLanguage + "_LabelShipToAddr");
                ViewBag.LabelProcess = mananger.GetString(thisLanguage + "_LabelProcess");
                ViewBag.LabelToBeWorked = mananger.GetString(thisLanguage + "_LabelToBeWorked");
                ViewBag.NewId = Guid.NewGuid().ToString();

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:SalesOrder", e.Message)); }
        }

        [HttpPost]
        public ActionResult SalesOrderDetail(string language)
        {
            try
            {
                string orderGuid = Request.Form["OrderGuid"].GetString();
                string UserID = Request.Form["userGuid"].GetString();
                string orderstatus = Request.Form["Orderstatus"].GetString();
                if (string.IsNullOrWhiteSpace(orderGuid)) throw new Exception("Order lines not found.");
                string thisLanguage = GetClientLanguage(language);
                string orderCode = "";
                Model.Order.SaleOrder order = SEMI.Order.OrderDataHelper.GetInstance().GetSO(orderGuid, thisLanguage);
                if (order == null) throw new Exception("Sales order not found.");
                ViewBag.Order = order;
                ViewBag.orderGuid = orderGuid;
                ViewBag.UserID = UserID;

                if (string.IsNullOrWhiteSpace(orderstatus))
                    orderstatus = "ToBeShipped";

                ViewBag.orderStatus = orderstatus;

                #region 设置页面标签内容(zh,en)
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelReceiptQty = mananger.GetString(thisLanguage + "_LabelReceiptQty");
                ViewBag.LabelOrderDetail = mananger.GetString(thisLanguage + "_LabelOrderDetail");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelAmt = mananger.GetString(thisLanguage + "_LabelAmt");
                ViewBag.LabelReceiptAmt = mananger.GetString(thisLanguage + "_LabelReceiptAmt");

                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelComplete = mananger.GetString(thisLanguage + "_LabelComplete");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SalesOrderDetail", e.Message)); }
        }

        [HttpPost]
        public ActionResult SOPrintDetail(string language)
        {
            try
            {
                string siteguid = Request.Form["siteguid"].GetString();
                string orderDate = Request.Form["orderDate"].GetString();
                string thisLanguage = GetClientLanguage(language);
                if (string.IsNullOrWhiteSpace("siteguid") || string.IsNullOrWhiteSpace("orderDate")) throw new Exception("No Order to be printed");
                List<Model.Order.SaleLine> PrintSO = SEMI.Order.OrderDataHelper.GetInstance().PrintSO(siteguid, orderDate, thisLanguage);

                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCheck = mananger.GetString(thisLanguage + "_LabelCheck");
                ViewBag.LabelOrderNumber = mananger.GetString(thisLanguage + "_LabelOrderNumber");
                ViewBag.LabelUserName = mananger.GetString(thisLanguage + "_LabelUserName");
                ViewBag.LabelRequireDate = mananger.GetString(thisLanguage + "_LabelRequireDate");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.Order = PrintSO;
                ViewBag.orderDate = orderDate;

                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelOrderDelivery = mananger.GetString(thisLanguage + "_LabelOrderDelivery");
                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SOPrintDetail", e.Message));
            }
        }

        [HttpPost]
        public ActionResult SalesPriceList(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
                if (BUList == null || BUList.Count == 0) throw new Exception("BU not found");
                ViewBag.BUList = BUList;

                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());

                //string selectedBU = Request.Form["BUGuid"].GetString();
                //if (selectedBU.Equals(string.Empty))
                //    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                //List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(thisLanguage, siteGuid, selectedBU);
                //ViewBag.SiteList = siteList;

                ViewBag.Language = thisLanguage;
                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelContainer = mananger.GetString(thisLanguage + "_LabelContainer");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:SalesPriceList", e.Message)); }
        }

        public ActionResult SalesPriceDetail(string language)
        {
            try
            {
                int recordID = Request.Form["RecordID"].ToInt();
                string BUGuid = Request.Form["BUGuid"].GetString();
                if (string.IsNullOrWhiteSpace(BUGuid)) throw new Exception("BU not found.");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.RecordID = recordID;
                ViewBag.BUGuid = BUGuid;
                if (!recordID.Equals(0))
                {
                    ViewBag.SDate = Request.Form["StartDate"].GetString();
                    ViewBag.ItemCode = Request.Form["ItemCode"].GetString();
                    ViewBag.ItemName = Request.Form["ItemName"].GetString();
                    ViewBag.Price = Request.Form["Price"].ToDouble(4);

                }
                else ViewBag.SDate = DateTime.Now.AddDays(1).ToString((thisLanguage.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"));

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SalesPriceDetail", e.Message));
            }
        }

        [HttpPost]
        public ActionResult SalesPriceListbySite(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
                if (BUList == null || BUList.Count == 0) throw new Exception("BU not found");
                ViewBag.BUList = BUList;

                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());

                ViewBag.Language = thisLanguage;

                string selectedBU = Request.Form["BUGuid"].GetString();
                if (selectedBU.Equals(string.Empty) && BUGuid.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(thisLanguage, siteGuid, string.IsNullOrWhiteSpace(BUGuid) ? selectedBU : BUGuid);
                ViewBag.SiteList = siteList;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelContainer = mananger.GetString(thisLanguage + "_LabelContainer");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:SalesPriceList", e.Message)); }
        }

        public ActionResult SalesPriceDetailbySite(string language)
        {
            try
            {
                int recordID = Request.Form["RecordID"].ToInt();
                string SiteGuid = Request.Form["SiteGuid"].GetString();
                if (string.IsNullOrWhiteSpace(SiteGuid)) throw new Exception("BU not found.");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.RecordID = recordID;
                ViewBag.SiteGuid = SiteGuid;
                if (!recordID.Equals(0))
                {
                    ViewBag.EDate = Request.Form["EndDate"].GetString();
                    ViewBag.SDate = Request.Form["StartDate"].GetString();
                    ViewBag.ItemCode = Request.Form["ItemCode"].GetString();
                    ViewBag.ItemName = Request.Form["ItemName"].GetString();
                    ViewBag.Price = Request.Form["Price"].ToDouble(4);

                }
                else ViewBag.SDate = DateTime.Now.AddDays(1).ToString((thisLanguage.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"));

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");
                

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SalesPriceDetail", e.Message));
            }
        }


        public ActionResult Promotion(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
                if (BUList == null || BUList.Count == 0) throw new Exception("BU not found");
                ViewBag.Language = thisLanguage;
                ViewBag.BUList = BUList;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelFilter = mananger.GetString(thisLanguage + "_LabelFilter");
                ViewBag.LabelMinOrderAmt = mananger.GetString(thisLanguage + "_LabelMinOrderAmt");
                ViewBag.LabelPromotionQty = mananger.GetString(thisLanguage + "_LabelPromotionQty");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelPromotionItem = mananger.GetString(thisLanguage + "_LabelPromotionItem");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller: Promotion", e.Message)); }
        }

        public ActionResult PromotionDetail(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                string BUGuid = Request.Form["BUGuid"].GetString();
                if (string.IsNullOrWhiteSpace(BUGuid)) throw new Exception("BUGuid not found.");
                int ID = Request.Form["ID"].ToInt();
                Model.Promotion.PromotionMast promotionMast = null;
                if (!ID.Equals(0))
                {
                    promotionMast = SEMI.Promotion.ItemPromotionHelper.GetInstance().GetPromotionMast(ID, thisLanguage);
                    if (promotionMast == null) throw new Exception("get Promotion data failed,ID=" + ID);
                }
                ViewBag.PromotionMast = promotionMast;
                if (promotionMast == null)
                    ViewBag.SDate = DateTime.Now.AddDays(1).ToString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"));
                else
                {
                    DateTime sDate = DateTime.Parse(promotionMast.StartDate);
                    if (sDate < DateTime.Now) sDate = DateTime.Now;
                    ViewBag.SDate = sDate.ToString((language.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"));
                }
                ViewBag.Language = thisLanguage;
                ViewBag.BUGuid = BUGuid;

                #region 设置页面标签内容(zh,en)
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelMinOrderAmt = mananger.GetString(thisLanguage + "_LabelMinOrderAmt");
                ViewBag.LabelPromotionQty = mananger.GetString(thisLanguage + "_LabelPromotionQty");

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:PromotionDetail", e.Message));
            }
        }

        public ActionResult PromotionItem(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                string BUGuid = Request.Form["BUGuid"].GetString();
                if (string.IsNullOrWhiteSpace(BUGuid)) throw new Exception("BUGuid not found.");
                string promotionGuid = SEMI.Promotion.ItemPromotionHelper.GetInstance().GetPromotionGuid(BUGuid);
                if (string.IsNullOrWhiteSpace(promotionGuid)) throw new Exception("Promotion Data not found.");
                ViewBag.PromotionItemList =
                    SEMI.Promotion.ItemPromotionHelper.GetInstance().GetPromotionItemList(promotionGuid);
                ViewBag.PromotionGuid = promotionGuid;
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelAdd = mananger.GetString(thisLanguage + "_LabelAdd");

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:PromotionItem", e.Message));
            }
        }

        #endregion

        #region BOM

        [HttpPost]
        public ActionResult BOMEdit(string language)
        {
            try
            {
                ViewBag.Permission = Request.Form["Permission"].GetString();

                string itemGuid = Request.Form["ItemGuid"].GetString();
                string itemCode = Request.Form["ItemCode"].GetString();
                string itemName_CN = Request.Form["ItemName_CN"].GetString();
                string itemName_EN = Request.Form["ItemName_EN"].GetString();
                if (string.IsNullOrWhiteSpace(itemGuid)) throw new Exception("ItemGuid not found.");
                string thisLanguage = GetClientLanguage(language);
                List<Model.UOM.UOMMast> UomList = SEMI.MastData.MastDataHelper.GetInstance().GetItemUOMList(thisLanguage, string.Empty);

                if (UomList == null || UomList.Count == 0) throw new Exception("UOM not found.");
                ViewBag.Language = thisLanguage;
                ViewBag.Action = "Edit";
                ViewBag.BOM = SEMI.BOM.BOMHelper.GetInstance().GetFGBOM(itemGuid, language);
                ViewBag.ProductGuid = itemGuid;
                ViewBag.ProductCode = itemCode;
                ViewBag.ProductName = itemName_CN;
                ViewBag.UOMList = UomList;


                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelType = mananger.GetString(thisLanguage + "_LabelType");
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelStdQty = mananger.GetString(thisLanguage + "_LabelStdQty");
                ViewBag.LabelActQty = mananger.GetString(thisLanguage + "_LabelActQty");
                ViewBag.LabelAdd = mananger.GetString(thisLanguage + "_LabelAdd");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");

                #endregion

                return PartialView("BOM");
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("TEXT", "500", "Controller:BOMEdit", e.Message)); }
        }

        [HttpPost]
        public ActionResult BOMCostView(string language)
        {
            try
            {
                string BUGuid = Request.Form["BUGuid"].GetString();
                string itemGuid = Request.Form["ItemGuid"].GetString();
                if (string.IsNullOrWhiteSpace(BUGuid) || string.IsNullOrWhiteSpace(itemGuid))
                    throw new Exception("BUGuid or ItemGuid not found");
                string thisLanguage = GetClientLanguage(language);
                Model.BOM.BOMMast BOMMast = SEMI.Cost.ItemCostHelper.GetInstance().GetBOMCost(BUGuid, itemGuid, DateTime.Now);
                if (BOMMast == null) throw new Exception("BOM Data not found");
                ViewBag.Language = thisLanguage;
                ViewBag.Action = "CostView";
                ViewBag.BOM = BOMMast;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelType = mananger.GetString(thisLanguage + "_LabelType");
                ViewBag.LabelCurrentPrice = mananger.GetString(thisLanguage + "_LabelCurrentPrice");
                ViewBag.LabelLastPrice = mananger.GetString(thisLanguage + "_LabelLastPrice");
                ViewBag.LabelActQty = mananger.GetString(thisLanguage + "_LabelActQty");
                ViewBag.LabelCurrentMaterialCost = mananger.GetString(thisLanguage + "_LabelCurrentMaterialCost");
                ViewBag.LabelLastMaterialCost = mananger.GetString(thisLanguage + "_LabelLastMaterialCost");

                #endregion

                return PartialView("BOM");
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("TEXT", "500", "Controller:BOMView", e.Message)); }
        }

        [HttpPost]
        public ActionResult BOMProduct(string language)
        {
            try
            {
                string itemGuid = Request.Form["ItemGuid"].GetString();
                if (string.IsNullOrWhiteSpace(itemGuid)) throw new Exception("ItemGuid not found.");
                string thisLanguage = GetClientLanguage(language);
                List<Model.UOM.UOMMast> UomList = SEMI.MastData.MastDataHelper.GetInstance().GetItemUOMList(thisLanguage, string.Empty);
                if (UomList == null || UomList.Count == 0) throw new Exception("UOM not found.");
                ViewBag.Language = thisLanguage;
                ViewBag.Action = "BOMProduct";
                ViewBag.Details = SEMI.BOM.BOMHelper.GetInstance().GetBOMProducts(itemGuid, thisLanguage);

                #region 设置页面标签内容(zh,en)
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelStdQty = mananger.GetString(thisLanguage + "_LabelStdQty");
                ViewBag.LabelActQty = mananger.GetString(thisLanguage + "_LabelActQty");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");

                #endregion

                return PartialView("BOM");
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("TEXT", "500", "Controller:BOMProduct", e.Message)); }
        }

        #endregion

        #region 上传,下载

        /// <summary>
        /// 上传单一Excel格式文件界面
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadExcelData(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.Type = Request.Form["Type"].GetString();
                string objStr = Request.Form["JSObject"].GetString();
                if (string.IsNullOrWhiteSpace(objStr)) ViewBag.Obj = "null";
                else ViewBag.Obj = objStr;
                ViewBag.Demo = SEMI.Upload.UploadDatasHelper.GetInstance().GetDemoFile(Request.Form["Type"].GetString());
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelViewError = mananger.GetString(thisLanguage + "_LabelViewError");
                ViewBag.LabelDemo = mananger.GetString(thisLanguage + "_LabelDemo");
                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:UploadItems", e.Message)); }
        }

        /// <summary>
        /// 根据文件路径下载已有Excel文件
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpGet]
        public FileStreamResult DownloadExcelData(string language)
        {
            try
            {
                string fileName = Request.Params["link"].GetString();
                if (string.IsNullOrWhiteSpace(fileName)) return null;
                string retFileName = string.Empty;
                int demo = Request.Params["demo"].ToInt();
                if (demo.Equals(1))
                {
                    retFileName = fileName;
                    fileName = System.IO.Path.Combine(Server.MapPath("~/Demos"), fileName);
                }
                else retFileName = DateTime.Now.ToString("yyyyMMddHHmmss") + System.IO.Path.GetExtension(fileName);
                return File(new System.IO.FileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read),
                    "application/octet-stream", retFileName);
            }
            catch { return null; }
        }

        #endregion

        #region Purchase

        [HttpPost]
        public ActionResult PurchaseOrder(string language)
        {
            try
            {
                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                if (siteGuid.Equals(string.Empty) && BUGuid.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                string thisLanguage = GetClientLanguage(language);
                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(thisLanguage, siteGuid, BUGuid);
                if (siteList == null || siteList.Count == 0) throw new Exception("get Site failed.");
                ViewBag.SiteList = siteList;
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelQuery = mananger.GetString(thisLanguage + "_LabelQuery");
                ViewBag.LabelOrderNumber = mananger.GetString(thisLanguage + "_LabelOrderNumber");
                ViewBag.LabelSupplier = mananger.GetString(thisLanguage + "_LabelSupplier");
                ViewBag.LabelOrderDate = mananger.GetString(thisLanguage + "_LabelOrderDate");
                ViewBag.LabelOrderAmt = mananger.GetString(thisLanguage + "_LabelOrderAmt");
                ViewBag.LabelDetail = mananger.GetString(thisLanguage + "_LabelDetail");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:PurchaseOrder", e.Message)); }
        }

        [HttpPost]
        public ActionResult PurchaseOrderDetail(string language)
        {
            try
            {
                string orderGuid = Request.Form["HeadGUID"].GetString();
                if (string.IsNullOrWhiteSpace(orderGuid)) throw new Exception("Order lines not found.");
                string thisLanguage = GetClientLanguage(language);
                Model.Order.PurchaseOrder order = SEMI.Order.OrderDataHelper.GetInstance().GetPO(orderGuid, thisLanguage);
                if (order == null) throw new Exception("Purchase order not found.");
                ViewBag.Order = order;

                #region 设置页面标签内容(zh,en)
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelReceiptQty = mananger.GetString(thisLanguage + "_LabelReceiptQty");
                ViewBag.LabelOrderDetail = mananger.GetString(thisLanguage + "_LabelOrderDetail");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelAmt = mananger.GetString(thisLanguage + "_LabelAmt");
                ViewBag.LabelReceiptAmt = mananger.GetString(thisLanguage + "_LabelReceiptAmt");

                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:PurchaseOrderDetail", e.Message)); }
        }


        public ActionResult PurchaseReceipt(string language)
        {
            try
            {
                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                string UserID = Utils.Common.EncyptHelper.DesEncypt(Request.Form["UserKey"].GetString());
                if ((siteGuid.Equals(string.Empty) && BUGuid.Equals(string.Empty)) || UserID.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.UserID = UserID;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelQuery = mananger.GetString(thisLanguage + "_LabelQuery");
                ViewBag.LabelDisplay = mananger.GetString(thisLanguage + "_LabelDisplay");
                ViewBag.LabelNoReceipt = mananger.GetString(thisLanguage + "_LabelNoReceipt");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelSpec = mananger.GetString(thisLanguage + "_LabelSpec");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelAmt = mananger.GetString(thisLanguage + "_LabelAmt");
                ViewBag.LabelReceiptQty = mananger.GetString(thisLanguage + "_LabelReceiptQty");
                ViewBag.LabelReceiptAmt = mananger.GetString(thisLanguage + "_LabelReceiptAmt");
                ViewBag.LabelTotalAmt = mananger.GetString(thisLanguage + "_LabelTotalAmt");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelSetZero = mananger.GetString(thisLanguage + "_LabelSetZero");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance()
                    .GetSiteMastList(thisLanguage, siteGuid, BUGuid);
                if (siteList == null || siteList.Count == 0) throw new Exception("get Site failed.");
                ViewBag.SiteList = siteList;
                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:PurchaseReceipt", e.Message)); }
        }

        public ActionResult PurchasePriceList(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                List<Model.Supplier.SupplierMast> supplierList =
                    SEMI.MastData.MastDataHelper.GetInstance().GetSupplierList(thisLanguage);
                if (supplierList == null || supplierList.Count == 0) throw new Exception("Supplier not found");
                ViewBag.Language = thisLanguage;
                ViewBag.SupplierList = supplierList;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelType = mananger.GetString(thisLanguage + "_LabelType");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:PurchasePriceList", e.Message)); }
        }

        public ActionResult PurchasePriceDetail(string language)
        {
            try
            {
                int recordID = Request.Form["RecordID"].ToInt();
                string supplierGuid = Request.Form["SupplierGuid"].GetString();
                if (string.IsNullOrWhiteSpace(supplierGuid)) throw new Exception("Supplier not found.");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.RecordID = recordID;
                ViewBag.Supplier = supplierGuid;
                if (!recordID.Equals(0))
                {
                    ViewBag.SDate = Request.Form["StartDate"].GetString();
                    ViewBag.ItemCode = Request.Form["ItemCode"].GetString();
                    ViewBag.ItemName = Request.Form["ItemName"].GetString();
                    ViewBag.Price = Request.Form["Price"].ToDouble(4);

                }
                else ViewBag.SDate = DateTime.Now.AddDays(1).ToString((thisLanguage.Equals("zh") ? "yyyy-MM-dd" : "MM/dd/yyyy"));

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelItem = mananger.GetString(thisLanguage + "_LabelItem");

                #endregion

                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:PurchasePriceDetail", e.Message));
            }
        }


        #endregion
        

        
        #region Supplier
        [HttpPost]
        public ActionResult Supplier(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelNameCn = mananger.GetString(thisLanguage + "_LabelNameCn");
                ViewBag.LabelNameEn = mananger.GetString(thisLanguage + "_LabelNameEn");
                ViewBag.LabelContact = mananger.GetString(thisLanguage + "_LabelContact");
                ViewBag.LabelEmail = mananger.GetString(thisLanguage + "_LabelEmail");
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:Supplier", e.Message)); }
        }

        [HttpPost]
        public ActionResult SupplierDetail(string language)
        {
            try
            {
                string guid = Request.Form["SupplierGuid"].GetString();
                Model.Supplier.SupplierMast supplierMast = null;
                if (!guid.Equals(string.Empty))
                {
                    supplierMast = SEMI.MastData.MastDataHelper.GetInstance().GetSupplierMast(guid);
                    if (supplierMast == null) throw new Exception("get Supplier failed,Guid=" + guid);
                }
                string thisLanguage = GetClientLanguage(language);
                ViewBag.SupplierMast = supplierMast;
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelNameCn = mananger.GetString(thisLanguage + "_LabelNameCn");
                ViewBag.LabelNameEn = mananger.GetString(thisLanguage + "_LabelNameEn");
                ViewBag.LabelContact = mananger.GetString(thisLanguage + "_LabelContact");
                ViewBag.LabelEmail = mananger.GetString(thisLanguage + "_LabelEmail");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelAddress = mananger.GetString(thisLanguage + "_LabelAddress");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelPostCode = mananger.GetString(thisLanguage + "_LabelPostCode");
                ViewBag.LabelTelNbr = mananger.GetString(thisLanguage + "_LabelTelNbr");
                ViewBag.LabelMobileNbr = mananger.GetString(thisLanguage + "_LabelMobileNbr");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SupplierDetail", e.Message)); }
        }

        [HttpPost]
        public ActionResult SupplierSite(string language)
        {
            try
            {
                string guid = Request.Form["SupplierGuid"].GetString();
                if (string.IsNullOrWhiteSpace(guid)) throw new Exception("Supplier Guid not found.");
                string thisLanguage = GetClientLanguage(language);
                ViewBag.SupplierGuid = guid;
                ViewBag.SupplierSiteList = SEMI.MastData.MastDataHelper.GetInstance().GetSupplierSiteList(guid);
                ViewBag.SiteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList();
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SupplierDetail", e.Message)); }
        }

        #endregion

        #region Site

        [HttpPost]
        public ActionResult Site(string language)
        {
            try
            {
                List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
                if (BUList == null || BUList.Count == 0) throw new Exception("Branch not found.");
                ViewBag.BUList = BUList;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelNameCn = mananger.GetString(thisLanguage + "_LabelNameCn");
                ViewBag.LabelNameEn = mananger.GetString(thisLanguage + "_LabelNameEn");
                ViewBag.LabelAddress = mananger.GetString(thisLanguage + "_LabelAddress");
                ViewBag.LabelTelNbr = mananger.GetString(thisLanguage + "_LabelTelNbr");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:Site", e.Message)); }
        }

        [HttpPost]
        public ActionResult SiteDetail(string language)
        {
            try
            {
                string guid = Request.Form["SiteGuid"].GetString();
                Model.Site.SiteMast siteMast = null;
                if (!guid.Equals(string.Empty))
                {
                    siteMast = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMast(guid);
                    if (siteMast == null) throw new Exception("get Site failed,Guid=" + guid);
                }
                else
                {
                    List<Model.BU.BUMast> BUList = SEMI.MastData.MastDataHelper.GetInstance().GetBUList(null, false);
                    if (BUList == null || BUList.Count == 0) throw new Exception("Branch not found.");
                    ViewBag.BUList = BUList;
                }
                string thisLanguage = GetClientLanguage(language);
                ViewBag.SiteMast = siteMast;
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelNameCn = mananger.GetString(thisLanguage + "_LabelNameCn");
                ViewBag.LabelNameEn = mananger.GetString(thisLanguage + "_LabelNameEn");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelAddress = mananger.GetString(thisLanguage + "_LabelAddress");
                ViewBag.LabelPostCode = mananger.GetString(thisLanguage + "_LabelPostCode");
                ViewBag.LabelTelNbr = mananger.GetString(thisLanguage + "_LabelTelNbr");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelSiteBU = mananger.GetString(thisLanguage + "_LabelSiteBU");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:SiteDetail", e.Message)); }
        }

        #endregion

        #region BU

        [HttpPost]
        public ActionResult BU(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelEndTime = mananger.GetString(thisLanguage + "_LabelEndTime");
                ViewBag.LabelTimeOut = mananger.GetString(thisLanguage + "_LabelTimeOut");
                ViewBag.LabelParent = mananger.GetString(thisLanguage + "_LabelParent");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:BU", e.Message)); }
        }

        [HttpPost]
        public ActionResult BUDetail(string language)
        {
            try
            {
                string guid = Request.Form["BUGuid"].GetString();
                Model.BU.BUMast BUMast = null;
                if (!guid.Equals(string.Empty))
                {
                    BUMast = SEMI.MastData.MastDataHelper.GetInstance().GetBUMast(guid);
                    if (BUMast == null) throw new Exception("get BU failed,Guid=" + guid);
                }
                string thisLanguage = GetClientLanguage(language);
                ViewBag.BUMast = BUMast;
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelEndTime = mananger.GetString(thisLanguage + "_LabelEndTime");
                ViewBag.LabelTimeOut = mananger.GetString(thisLanguage + "_LabelTimeOut");
                ViewBag.LabelParent = mananger.GetString(thisLanguage + "_LabelParent");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:BUDetail", e.Message)); }
        }

        #endregion

        #region 微信报表维护界面

        [HttpGet]
        public ActionResult WeChatReport(string language)
        {
            try
            {
                string parameters = Request.QueryString["p"].GetString();
                if (string.IsNullOrWhiteSpace(parameters)) throw new Exception("Parameters not found.");
                string[] paramArray = Utils.Common.EncyptHelper.DesEncypt(parameters).Split('/'); //解密字符串
                if (paramArray.Length != 2) throw new Exception("Params error.");
                Model.WeChatSqlMessage.WeChatReportDisplayData reportData = SEMI.WeChatSqlMessage.WeChatSqlMessageHelper
                    .GetInstance().GetWeChatDisplayReportData(paramArray[0], paramArray[1]);
                if (reportData == null) throw new Exception("No Data.");
                ViewBag.ReportData = reportData;
                return PartialView();
            }
            catch (Exception e) { return View("Error", GetErrorMast("HTML", "404", "Report Failed", e.Message)); }
        }
        public ActionResult WeChatReportSql(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelMainQuery = mananger.GetString(thisLanguage + "_LabelMainQuery");
                ViewBag.LabelSpaceNumber = mananger.GetString(thisLanguage + "_LabelSpaceNumber");
                ViewBag.LabelDisplayType = mananger.GetString(thisLanguage + "_LabelDisplayType");
                ViewBag.LabelLinkName = mananger.GetString(thisLanguage + "_LabelLinkName");
                ViewBag.LabelLinkField = mananger.GetString(thisLanguage + "_LabelLinkField");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:WeChatReportSql", e.Message)); }
        }
        public ActionResult WeChatReportSqlDetail(string language)
        {
            try
            {
                string guid = Request.Form["GUID"].GetString();
                Model.WeChatSqlMessage.WeChatSqlMessageData data = null;

                if (!string.IsNullOrWhiteSpace(guid))
                {
                    data = SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().GetWeChatSqlMessageData(guid);
                    if (data == null) throw new Exception("WeChatSqlMessageData not found, Guid=" + guid);
                }
                ViewBag.Data = data;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelMainQuery = mananger.GetString(thisLanguage + "_LabelMainQuery");
                ViewBag.LabelSpaceNumber = mananger.GetString(thisLanguage + "_LabelSpaceNumber");
                ViewBag.LabelDisplayType = mananger.GetString(thisLanguage + "_LabelDisplayType");
                ViewBag.LabelLinkName = mananger.GetString(thisLanguage + "_LabelLinkName");
                ViewBag.LabelLinkField = mananger.GetString(thisLanguage + "_LabelLinkField");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelMastData = mananger.GetString(thisLanguage + "_LabelMastData");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelTitleSql = mananger.GetString(thisLanguage + "_LabelTitleSql");
                ViewBag.LabelContentSql = mananger.GetString(thisLanguage + "_LabelContentSql");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelDisplayType = mananger.GetString(thisLanguage + "_LabelDisplayType");
                ViewBag.LabelTable = mananger.GetString(thisLanguage + "_LabelTable");
                ViewBag.LabelList = mananger.GetString(thisLanguage + "_LabelList");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:WeChatReportSqlDetail", e.Message)); }
        }
        public ActionResult WeChatReportJob(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelMainQuery = mananger.GetString(thisLanguage + "_LabelMainQuery");
                ViewBag.LabelRunWeek = mananger.GetString(thisLanguage + "_LabelRunWeek");
                ViewBag.LabelDailyStartTime = mananger.GetString(thisLanguage + "_LabelDailyStartTime");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelKeyWords = mananger.GetString(thisLanguage + "_LabelKeyWords");
                ViewBag.LabelParameter = mananger.GetString(thisLanguage + "_LabelParameter");
                ViewBag.LabelRun = mananger.GetString(thisLanguage + "_LabelRun");
                ViewBag.LabelEveryday = mananger.GetString(thisLanguage + "_LabelEveryday");
                ViewBag.LabelMonday = mananger.GetString(thisLanguage + "_LabelMonday");
                ViewBag.LabelTuesday = mananger.GetString(thisLanguage + "_LabelTuesday");
                ViewBag.LabelWensday = mananger.GetString(thisLanguage + "_LabelWensday");
                ViewBag.LabelThursday = mananger.GetString(thisLanguage + "_LabelThursday");
                ViewBag.LabelFriday = mananger.GetString(thisLanguage + "_LabelFriday");
                ViewBag.LabelSaturday = mananger.GetString(thisLanguage + "_LabelSaturday");
                ViewBag.LabelSunday = mananger.GetString(thisLanguage + "_LabelSunday");
                ViewBag.LabelManual = mananger.GetString(thisLanguage + "_LabelManual");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:WeChatReportJob", e.Message)); }
        }
        public ActionResult WeChatReportRunJob(string language)
        {
            try
            {
                int id = Request.Form["ID"].ToInt();
                if (id.Equals(0)) throw new Exception("Job ID not found.");
                else ViewBag.ID = id;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRun = mananger.GetString(thisLanguage + "_LabelRun");
                ViewBag.LabelReceiver = mananger.GetString(thisLanguage + "_LabelReceiver");
                ViewBag.LabelSend = mananger.GetString(thisLanguage + "_LabelSend");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:WeChatReportRunJob", e.Message)); }
        }
        public ActionResult WeChatReportJobDetail(string language)
        {
            try
            {
                int id = Request.Form["ID"].ToInt();
                Model.WeChatSqlMessage.WeChatSqlMessageJob data = null;

                if (!id.Equals(0))
                {
                    data = SEMI.WeChatSqlMessage.WeChatSqlMessageHelper.GetInstance().GetWeChatReportJobData(id);
                    if (data == null) throw new Exception("WeChatSqlMessageJob not found,ID=" + id);
                }
                ViewBag.Data = data;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelMainQuery = mananger.GetString(thisLanguage + "_LabelMainQuery");
                ViewBag.LabelRunWeek = mananger.GetString(thisLanguage + "_LabelRunWeek");
                ViewBag.LabelDailyStartTime = mananger.GetString(thisLanguage + "_LabelDailyStartTime");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelKeyWords = mananger.GetString(thisLanguage + "_LabelKeyWords");
                ViewBag.LabelParameter = mananger.GetString(thisLanguage + "_LabelParameter");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelReciverField = mananger.GetString(thisLanguage + "_LabelReciverField");
                ViewBag.LabelMastData = mananger.GetString(thisLanguage + "_LabelMastData");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelDataSql = mananger.GetString(thisLanguage + "_LabelDataSql");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEveryday = mananger.GetString(thisLanguage + "_LabelEveryday");
                ViewBag.LabelMonday = mananger.GetString(thisLanguage + "_LabelMonday");
                ViewBag.LabelTuesday = mananger.GetString(thisLanguage + "_LabelTuesday");
                ViewBag.LabelWensday = mananger.GetString(thisLanguage + "_LabelWensday");
                ViewBag.LabelThursday = mananger.GetString(thisLanguage + "_LabelThursday");
                ViewBag.LabelFriday = mananger.GetString(thisLanguage + "_LabelFriday");
                ViewBag.LabelSaturday = mananger.GetString(thisLanguage + "_LabalSaturday");
                ViewBag.LabelSunday = mananger.GetString(thisLanguage + "_LabelSunday");
                ViewBag.LabelManual = mananger.GetString(thisLanguage + "_LabelManual");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:WeChatReportJobDetail", e.Message)); }
        }

        #endregion

        #region Calendar
        [HttpPost]
        public ActionResult Calendar(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelBranch = mananger.GetString(thisLanguage + "_LabelBranch");
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelRest = mananger.GetString(thisLanguage + "_LabelRest");
                ViewBag.LabelWorking = mananger.GetString(thisLanguage + "_LabelWorking");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:Calendar", e.Message)); }
        }

        

        [HttpPost]
        public ActionResult CalendarDetail(string language)
        {
            try
            {
                int calendarID = Request.Form["CalendarID"].ToInt();
                Model.Calendar.CalendarMast calendarMast = null;
                if (!calendarID.Equals(0))
                {
                    calendarMast = SEMI.MastData.MastDataHelper.GetInstance().GetCalendarMast(calendarID);
                    if (calendarMast == null) throw new Exception("get Calendar failed,ID=" + calendarID);
                }
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Calendar = calendarMast;
                ViewBag.Language = thisLanguage;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelBranch = mananger.GetString(thisLanguage + "_LabelBranch");
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelRest = mananger.GetString(thisLanguage + "_LabelRest");
                ViewBag.LabelWorking = mananger.GetString(thisLanguage + "_LabelWorking");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelStartTime = mananger.GetString(thisLanguage + "_LabelStartTime");
                ViewBag.LabelEndTime = mananger.GetString(thisLanguage + "_LabelEndTime");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:CalendarDetail", e.Message)); }
        }
        #endregion

        #region MRP
        [HttpPost]
        public ActionResult MRP(string language)
        {
            try
            {
                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                if (siteGuid.Equals(string.Empty) && BUGuid.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");
                string thisLanguage = GetClientLanguage(language);
                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance()
                    .GetSiteMastList(thisLanguage, siteGuid, BUGuid);
                if (siteList == null || siteList.Count == 0) throw new Exception("get Site failed.");
                ViewBag.SiteList = siteList;
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelQuery = mananger.GetString(thisLanguage + "_LabelQuery");
                ViewBag.LabelSupplier = mananger.GetString(thisLanguage + "_LabelSupplier");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelUnit = mananger.GetString(thisLanguage + "_LabelUnit");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelRun = mananger.GetString(thisLanguage + "_LabelRun");
                ViewBag.LabelDetail = mananger.GetString(thisLanguage + "_LabelDetail");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:MRP", e.Message)); }
        }

        public ActionResult MRPSODetail(string language)
        {
            try
            {
                string lineGuid = Request.Form["LineGuid"].GetString();
                if (string.IsNullOrWhiteSpace(lineGuid)) throw new Exception("MRP LineGuid not found.");
                string thisLanguage = GetClientLanguage(language);
                List<Model.MRP.MRPSODetail> details = SEMI.MRP.MRPHelper.GetInstance().GetMRPLineDetails(lineGuid);
                if (details == null || details.Count == 0) throw new Exception("MRP Line Details not found.");
                ViewBag.Details = details;

                #region 设置页面标签内容(zh,en)
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelPrice = mananger.GetString(thisLanguage + "_LabelPrice");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelOrderDetail = mananger.GetString(thisLanguage + "_LabelOrderDetail");
                ViewBag.LabelOrderNumber = mananger.GetString(thisLanguage + "_LabelOrderNumber");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:MRPDetail", e.Message)); }
        }

        #endregion

        #region CustomerData
        public ActionResult CustomerData(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelInstruction = mananger.GetString(thisLanguage + "_LabelInstruction");
                ViewBag.LabelRunWeek = mananger.GetString(thisLanguage + "_LabelRunWeek");
                ViewBag.LabelRun = mananger.GetString(thisLanguage + "_LabelRun");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelResult = mananger.GetString(thisLanguage + "_LabelResult");
                ViewBag.LabelEveryday = mananger.GetString(thisLanguage + "_LabelEveryday");
                ViewBag.LabelMonday = mananger.GetString(thisLanguage + "_LabelMonday");
                ViewBag.LabelTuesday = mananger.GetString(thisLanguage + "_LabelTuesday");
                ViewBag.LabelWensday = mananger.GetString(thisLanguage + "_LabelWensday");
                ViewBag.LabelThursday = mananger.GetString(thisLanguage + "_LabelThursday");
                ViewBag.LabelFriday = mananger.GetString(thisLanguage + "_LabelFriday");
                ViewBag.LabelSaturday = mananger.GetString(thisLanguage + "_LabelSaturday");
                ViewBag.LabelSunday = mananger.GetString(thisLanguage + "_LabelSunday");
                ViewBag.LabelManual = mananger.GetString(thisLanguage + "_LabelManual");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:CustomerData", e.Message)); }
        }

        public ActionResult CustomerDataDetail(string language)
        {
            try
            {
                int id = Request.Form["ID"].ToInt();
                Model.CustomerData.CustomerDataMast data = null;

                if (!id.Equals(0))
                {
                    data = SEMI.CustomerData.CustomerDataHelper.GetInstance().GetCustomerDataMast(id);
                    if (data == null) throw new Exception("CustomerDataMast not found,ID=" + id);
                }
                ViewBag.Data = data;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelInstruction = mananger.GetString(thisLanguage + "_LabelInstruction");
                ViewBag.LabelRunWeek = mananger.GetString(thisLanguage + "_LabelRunWeek");
                ViewBag.LabelDataSql = mananger.GetString(thisLanguage + "_LabelDataSql");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelMastData = mananger.GetString(thisLanguage + "_LabelMastData");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelRemark = mananger.GetString(thisLanguage + "_LabelRemark");
                ViewBag.LabelEveryday = mananger.GetString(thisLanguage + "_LabelEveryday");
                ViewBag.LabelMonday = mananger.GetString(thisLanguage + "_LabelMonday");
                ViewBag.LabelTuesday = mananger.GetString(thisLanguage + "_LabelTuesday");
                ViewBag.LabelWensday = mananger.GetString(thisLanguage + "_LabelWensday");
                ViewBag.LabelThursday = mananger.GetString(thisLanguage + "_LabelThursday");
                ViewBag.LabelFriday = mananger.GetString(thisLanguage + "_LabelFriday");
                ViewBag.LabelSaturday = mananger.GetString(thisLanguage + "_LabelSaturday");
                ViewBag.LabelSunday = mananger.GetString(thisLanguage + "_LabelSunday");
                ViewBag.LabelManual = mananger.GetString(thisLanguage + "_LabelManual");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:CustomerDataDetail", e.Message)); }
        }

        public ActionResult CustomerDataResult(string language)
        {
            try
            {
                int id = Request.Form["ID"].ToInt();
                Model.CustomerData.CustomerDataLog data = SEMI.CustomerData.CustomerDataHelper.GetInstance().GetCustomerDataResult(id);
                ViewBag.Log = data;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelExport = mananger.GetString(thisLanguage + "_LabelExport");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:CustomerDataResult", e.Message)); }
        }

        #endregion

        #region POS销售报告
        public ActionResult KeyReport(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                List<Model.StallSales.StallSalesEntity> StallEntity = SEMI.MastData.MastDataHelper.GetInstance().GetSallEntity(thisLanguage);
                ViewBag.entitylist = StallEntity;
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager manager = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelBusinessDate = manager.GetString(thisLanguage + "_LabelBusinessDate");
                ViewBag.LabelKeyWordsSearch = manager.GetString(thisLanguage + "_LabelKeyWordsSearch");
                ViewBag.LabelSearch = manager.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelExport = manager.GetString(thisLanguage + "_LabelExport");
                ViewBag.LabelKeyWords = manager.GetString(thisLanguage + "_LabelKeyWords");
                ViewBag.LabelFlowNo = manager.GetString(thisLanguage + "_LabelFlowNo");
                ViewBag.LabelCardNo = manager.GetString(thisLanguage + "_LabelCardNo");
                ViewBag.LabelmbName = manager.GetString(thisLanguage + "_LabelmbName");
                ViewBag.LabelCardType = manager.GetString(thisLanguage + "_LabelCardType");
                ViewBag.LabelTime = manager.GetString(thisLanguage + "_LabelTime");
                ViewBag.LabelProductCode = manager.GetString(thisLanguage + "_LabelProductCode");
                ViewBag.LabelProductName = manager.GetString(thisLanguage + "_LabelProductName");
                ViewBag.LabelSalesPrice = manager.GetString(thisLanguage + "_LabelSalesPrice");
                ViewBag.LabelSalesQty = manager.GetString(thisLanguage + "_LabelSalesQty");
                ViewBag.LabelSalesAmt = manager.GetString(thisLanguage + "_LabelSalesAmt");
                ViewBag.LabelDiscountAmt = manager.GetString(thisLanguage + "_LabelDiscountAmt");
                ViewBag.LabelSetColumn = manager.GetString(thisLanguage + "_LabelSetColumn");
                ViewBag.LabelProduct = manager.GetString(thisLanguage + "_LabelProduct");
                ViewBag.LabelConsumer = manager.GetString(thisLanguage + "_LabelConsumer");
                ViewBag.LabelSalesData = manager.GetString(thisLanguage + "_LabelSalesData");
                ViewBag.LabelYear = manager.GetString(thisLanguage + "_LabelYear");
                ViewBag.LabelMonth = manager.GetString(thisLanguage + "_LabelMonth");
                ViewBag.LabelDate = manager.GetString(thisLanguage + "_LabelDate");
                ViewBag.LabelCounter = manager.GetString(thisLanguage + "_LabelCounter");
                ViewBag.LabelCounterNo = manager.GetString(thisLanguage + "_LabelCounterNo");
                ViewBag.LabelCounterName = manager.GetString(thisLanguage + "_LabelCounterName");

                ViewBag.LabelLoading = manager.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = manager.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = manager.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = manager.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = manager.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = manager.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = manager.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = manager.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = manager.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelNoChosedRecord = manager.GetString(thisLanguage + "_LabelNoChosedRecord");
                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("HTML", "500", "Controller:POSReport", e.Message));
            }
        }


        #endregion
        #endregion

        [HttpPost]
        public ActionResult SiteTimeSet(string language)
        {
            try
            {
                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                if (siteGuid.Equals(string.Empty) && BUGuid.Equals(string.Empty))
                    throw new Exception("'UserKey','SiteKey','BUKey' not found");

                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;

                List<Model.Site.SiteMast> siteList = SEMI.MastData.MastDataHelper.GetInstance().GetSiteMastList(thisLanguage, siteGuid, BUGuid);
                if (siteList == null || siteList.Count == 0) throw new Exception("get Site failed.");
                ViewBag.SiteList = siteList;

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelBranch = mananger.GetString(thisLanguage + "_LabelBranch");
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelStartTime = mananger.GetString(thisLanguage + "_LabelStartTime");
                ViewBag.LabelEndTime = mananger.GetString(thisLanguage + "_LabelEndTime");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelRest = mananger.GetString(thisLanguage + "_LabelRest");
                ViewBag.LabelWorking = mananger.GetString(thisLanguage + "_LabelWorking");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelNameCn = mananger.GetString(thisLanguage + "_LabelNameCn");
                ViewBag.LabelNameEn = mananger.GetString(thisLanguage + "_LabelNameEn");

                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:SetTimeSet", e.Message)); }
        }

        [HttpPost]
        public ActionResult SiteTimeSetDetail(string language)
        {
            try
            {
                int calendarID = Request.Form["CalendarID"].ToInt();
                string siteGUID = Request.Form["SiteGUID"].ToString();
                Model.Calendar.CalendarMast calendarMast = null;
                if (!calendarID.Equals(0))
                {
                    calendarMast = SEMI.MastData.MastDataHelper.GetInstance().GetSiteTimeSetMast(calendarID,"");
                    if (calendarMast == null) throw new Exception("get Calendar failed,ID=" + calendarID);
                }
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Calendar = calendarMast;
                ViewBag.Language = thisLanguage;
                ViewBag.Site = SEMI.MastData.MastDataHelper.GetInstance().GetSiteTimeSetMast(calendarID, siteGUID);

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelBranch = mananger.GetString(thisLanguage + "_LabelBranch");
                ViewBag.LabelSite = mananger.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelRest = mananger.GetString(thisLanguage + "_LabelRest");
                ViewBag.LabelWorking = mananger.GetString(thisLanguage + "_LabelWorking");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelDelete = mananger.GetString(thisLanguage + "_LabelDelete");
                ViewBag.LabelStartTime = mananger.GetString(thisLanguage + "_LabelStartTime");
                ViewBag.LabelEndTime = mananger.GetString(thisLanguage + "_LabelEndTime");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:CalendarDetail", e.Message)); }
        }
        #endregion


        #region SPD报告
        public ActionResult SPDReport(string language)
        {
            try
            {
                string userKey = EncyptHelper.DesEncypt(Request.Form["userKey"].GetString());
                List<Model.SPD.LogEntity> empCode = SEMI.MastData.MastDataHelper.GetInstance().GetEmpCode(userKey);
                List<Model.Site.SiteMast> GetSite = SEMI.MastData.MastDataHelper.GetInstance().GetSite(userKey);
                bool fnVW = true;
                if (empCode.Count > 0)
                {
                    if (string.IsNullOrWhiteSpace(empCode.FirstOrDefault().empCode) && !string.IsNullOrWhiteSpace(empCode.FirstOrDefault().siteCode))
                        fnVW=false;
                }

                ViewBag.fnVW = fnVW;
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                System.Resources.ResourceManager manager = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelSetColumn = manager.GetString(thisLanguage + "_LabelSetColumn");
                ViewBag.LabelSite = manager.GetString(thisLanguage + "_LabelSite");
                ViewBag.LabelConsumer = manager.GetString(thisLanguage + "_LabelConsumer");
                ViewBag.LabelSupplier = manager.GetString(thisLanguage + "_LabelSupplier");
                ViewBag.LabelProduct = manager.GetString(thisLanguage + "_LabelProduct");
                ViewBag.LabelBusinessDate = manager.GetString(thisLanguage + "_LabelBusinessDate");
                ViewBag.LabelYear = manager.GetString(thisLanguage + "_LabelYear");
                ViewBag.LabelMonth = manager.GetString(thisLanguage + "_LabelMonth");
                ViewBag.LabelDate = manager.GetString(thisLanguage + "_LabelDate");
                ViewBag.LabelTime = manager.GetString(thisLanguage + "_LabelTime");
                ViewBag.LabelProductCode = manager.GetString(thisLanguage + "_LabelProductCode");
                ViewBag.LabelProductName = manager.GetString(thisLanguage + "_LabelProductName");
                ViewBag.LabelSalesQty = manager.GetString(thisLanguage + "_LabelSalesQty");
                ViewBag.LabelGrossAmt = manager.GetString(thisLanguage + "_LabelGrossAmt");
                ViewBag.LabelSalesAmt = manager.GetString(thisLanguage + "_LabelSalesAmt");
                ViewBag.LabelCost = manager.GetString(thisLanguage + "_LabelCost");
                ViewBag.LabelGrossMargin = manager.GetString(thisLanguage + "_LabelGrossMargin");
                ViewBag.LabelNetMargin = manager.GetString(thisLanguage + "_LabelNetMargin");
                ViewBag.LabelSearch = manager.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelExport = manager.GetString(thisLanguage + "_LabelExport");
                ViewBag.UserKey = empCode;
                ViewBag.GetSite = GetSite;

                ViewBag.LabelLoading = manager.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = manager.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = manager.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = manager.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = manager.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = manager.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = manager.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = manager.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = manager.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelNoChosedRecord = manager.GetString(thisLanguage + "_LabelNoChosedRecord");
                return PartialView();
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("HTML", "500", "Controller:SPDReport", e.Message));
            }
        }
        #endregion

        /// <summary>
        /// 维护菜单数据界面
        /// </summary>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult MenuData(string language)
        {
            try
            {
                string thisLanguage = GetClientLanguage(language);
                ViewBag.Language = thisLanguage;
                ViewBag.Type = "FG";
                ViewBag.Permission = Request.Form["Permission"].GetString();

                string siteGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["SiteKey"].GetString());
                string BUGuid = Utils.Common.EncyptHelper.DesEncypt(Request.Form["BUKey"].GetString());
                string UserID = Utils.Common.EncyptHelper.DesEncypt(Request.Form["UserKey"].GetString());

                Boolean siteUser = false;
                if (!string.IsNullOrWhiteSpace(siteGuid))
                {
                    siteUser = true;
                }

                ViewBag.siteUser = siteUser;
                List<Model.Item.FGMast> TimeList = SEMI.MastData.ItemDataHelper.GetInstance().GetTimeList(language);
                if (TimeList != null && TimeList.Count > 0)
                    ViewBag.TimeList = TimeList;

                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelDataUpload = mananger.GetString(thisLanguage + "_LabelDataUpload");
                ViewBag.LabelSearchHint = mananger.GetString(thisLanguage + "_LabelSearchHint");
                ViewBag.LabelSearch = mananger.GetString(thisLanguage + "_LabelSearch");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelWeight = mananger.GetString(thisLanguage + "_LabelWeight");
                ViewBag.LabelCreateTime = mananger.GetString(thisLanguage + "_LabelCreateTime");
                ViewBag.LabelBOMUpload = mananger.GetString(thisLanguage + "_LabelBOMUpload");
                ViewBag.LabelPicUpload = mananger.GetString(thisLanguage + "_LabelPicUpload");
                ViewBag.LabelLoading = mananger.GetString(thisLanguage + "_LabelLoading");
                ViewBag.LabelNoData = mananger.GetString(thisLanguage + "_LabelNoData");
                ViewBag.LabelLengthMenu = mananger.GetString(thisLanguage + "_LabelLengthMenu");
                ViewBag.LabelTableInfo = mananger.GetString(thisLanguage + "_LabelTableInfo");
                ViewBag.LabelTableInfoEmpty = mananger.GetString(thisLanguage + "_LabelTableInfoEmpty");
                ViewBag.LabelFirstPage = mananger.GetString(thisLanguage + "_LabelFirstPage");
                ViewBag.LabelLastPage = mananger.GetString(thisLanguage + "_LabelLastPage");
                ViewBag.LabelPreviousPage = mananger.GetString(thisLanguage + "_LabelPreviousPage");
                ViewBag.LabelNextPage = mananger.GetString(thisLanguage + "_LabelNextPage");
                ViewBag.LabelOtherCost = mananger.GetString(thisLanguage + "_LabelOtherCost");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelBusinessType = mananger.GetString(thisLanguage + "_LabelBusinessType");
                ViewBag.LabelPicture_en = mananger.GetString(thisLanguage + "_LabelPicture_en");
                ViewBag.LabelPicture_zh = mananger.GetString(thisLanguage + "_LabelPicture_zh");

                #endregion

                return PartialView();
            }
            catch (Exception e) { return PartialView("Error", GetErrorMast("HTML", "500", "Controller:FGItem", e.Message)); }
        }


        [HttpPost]
        public ActionResult ExportExcelSiteReport(string language)
        {
            try
            {
                string empCode = Request.Form["empCode"].GetString();
                string startdate = Request.Form["startdate"].GetString();
                string enddate = Request.Form["enddate"].GetString();
                string all = Request.Form["all"].GetString();
                string group = Request.Form["group"].GetString();
                string getsite = Request.Form["getsite"].GetString();

                Utils.Excel.ExcelHelper.GetInstance().DownloadExcelFile(SEMI.Report.DownloadReport.GetInstance().
                    ExportExcelSiteReport(empCode, startdate, enddate, all, group, getsite, language));

                return null;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public ActionResult MenuItemDetail(string language)
        {
            try
            {
                ViewBag.Permission = Request.Form["Permission"].GetString();
                string type = Request.Form["Type"].GetString();
                string guid = Request.Form["ItemGuid"].GetString();
                string ItemName_EN = Request.Form["ItemName_EN"].GetString();
                string StartDate = Request.Form["StartDate"].GetString();
                string ItemClassGuid = Request.Form["ItemClassGuid"].GetString();
                string SiteGuid = Request.Form["ItemType"].GetString();

                string langCode = Request.Form["langCode"].GetString();
                if (string.IsNullOrWhiteSpace(type)) throw new Exception("Get ItemType or ItemAction failed");
                Model.Item.FGMast itemMast = null;
                string thisLanguage = GetClientLanguage(language);
                if (!guid.Equals(string.Empty))
                {
                    itemMast = SEMI.MastData.ItemDataHelper.GetInstance().GetMenuDetail(language, guid, ItemName_EN,langCode);
                    if (itemMast == null) throw new Exception("Get Item failed,Guid=" + guid);
                }
                ViewBag.ItemMast = itemMast;
                ViewBag.ItemClassGuid = ItemClassGuid;

                ViewBag.StartDate = StartDate;
                ViewBag.EndDate = DateTime.Parse(StartDate).AddDays(6).ToString("yyyy-MM-dd");

                ViewBag.ItemName_EN = ItemName_EN;
                ViewBag.langCode = langCode;
                ViewBag.SiteGuid = SiteGuid;

                List<Model.Dict.DictGroup> itemDicts = SEMI.MastData.ItemDataHelper.GetInstance().GetItemDict((itemMast == null ? null : itemMast.ItemProperies));
                if (itemDicts == null) itemDicts = new List<Model.Dict.DictGroup>();
                List<Model.Item.ItemClass> itemClasses = SEMI.MastData.ItemDataHelper.GetInstance().GetItemClass("menuClass");
                if (itemClasses == null) itemClasses = new List<Model.Item.ItemClass>();
                ViewBag.ItemClasses = itemClasses;
                ViewBag.Language = thisLanguage;
                ViewBag.Type = type;
                ViewBag.PicUrl = ConfigurationManager.AppSettings["PictureUrl1"].GetString();
                ViewBag.PicFile = ConfigurationManager.AppSettings["PictureFile1"].GetString();
                ViewBag.ParFile = ConfigurationManager.AppSettings["PictureFile"].GetString();
                
                #region 设置页面标签内容(zh,en)

                System.Resources.ResourceManager mananger = ADEN.Properties.Resources.ResourceManager;
                ViewBag.LabelRequired = mananger.GetString(thisLanguage + "_LabelRequired");
                ViewBag.LabelClose = mananger.GetString(thisLanguage + "_LabelClose");
                ViewBag.LabelSave = mananger.GetString(thisLanguage + "_LabelSave");
                ViewBag.LabelNew = mananger.GetString(thisLanguage + "_LabelNew");
                ViewBag.LabelEdit = mananger.GetString(thisLanguage + "_LabelEdit");
                ViewBag.LabelStatus = mananger.GetString(thisLanguage + "_LabelStatus");
                ViewBag.LabelCode = mananger.GetString(thisLanguage + "_LabelCode");
                ViewBag.LabelDescription = mananger.GetString(thisLanguage + "_LabelDescription");
                ViewBag.LabelDescription_EN = mananger.GetString(thisLanguage + "_LabelDescription_EN");
                ViewBag.LabelBuy = mananger.GetString(thisLanguage + "_LabelBuy");
                ViewBag.LabelStatusActive = mananger.GetString(thisLanguage + "_LabelStatusActive");
                ViewBag.LabelStatusBlock = mananger.GetString(thisLanguage + "_LabelStatusBlock");
                ViewBag.LabelClassName = mananger.GetString(thisLanguage + "_LabelClassName");
                ViewBag.LabelDishSize = mananger.GetString(thisLanguage + "_LabelDishSize");
                ViewBag.LabelContainer = mananger.GetString(thisLanguage + "_LabelContainer");
                ViewBag.LabelSort = mananger.GetString(thisLanguage + "_LabelSort");
                ViewBag.LabelMastData = mananger.GetString(thisLanguage + "_LabelMastData");
                //ViewBag.LabelTips = mananger.GetString(thisLanguage + "_LabelTips");
                ViewBag.LabelCooking = mananger.GetString(thisLanguage + "_LabelCooking");
                ViewBag.LabelInstruction = mananger.GetString(thisLanguage + "_LabelInstruction");
                ViewBag.LabelPropery = mananger.GetString(thisLanguage + "_LabelPropery");
                ViewBag.LabelPicture1 = mananger.GetString(thisLanguage + "_LabelPicture1");
                ViewBag.LabelPicture2 = mananger.GetString(thisLanguage + "_LabelPicture2");
                ViewBag.LabelPicture3 = mananger.GetString(thisLanguage + "_LabelPicture3");
                ViewBag.LabelOtherCost = mananger.GetString(thisLanguage + "_LabelOtherCost");
                ViewBag.LabelIngredients = mananger.GetString(thisLanguage + "_LabelIngredients");
                ViewBag.LabelStartDate = mananger.GetString(thisLanguage + "_LabelStartDate");
                ViewBag.LabelEndDate = mananger.GetString(thisLanguage + "_LabelEndDate");
                ViewBag.LabelBusinessType= mananger.GetString(thisLanguage + "_LabelBusinessType");
                ViewBag.LabelQty = mananger.GetString(thisLanguage + "_LabelQty");
                ViewBag.LabelPicture_en = mananger.GetString(thisLanguage + "_LabelPicture_en");
                ViewBag.LabelPicture_zh = mananger.GetString(thisLanguage + "_LabelPicture_zh");

                #endregion

                return PartialView(itemDicts);
            }
            catch (Exception e)
            {
                return PartialView("Error", GetErrorMast("MODAL", "500", "Controller:FGDetail", e.Message));
            }
        }
    }
}
