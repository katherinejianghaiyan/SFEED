using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WeChatEvent;
using Utils.Common;

namespace ADEN.Controllers
{
    public class WeChatController : Controller
    {
        // GET: WeChat
        public ActionResult Binding()
        {
            string key = Request.Params["key"].GetString();
            if (string.IsNullOrWhiteSpace(key)) return View("WeChatError");
            else
            {
                PersonPic data = PersonPicHelper.CheckEncyptedEmployeeID(key);
                if (data != null && !string.IsNullOrWhiteSpace(data.EmpID)) return View("PersonPic", data);
                else
                {
                    ViewBag.OpenID = Utils.Common.EncyptHelper.DesEncypt(key);
                    ViewBag.Message = Request.Params["status"].GetString();
                    return View();
                }
            }
        }

        public void CallBack()
        {
            WeChat.Entrance.PublicEntrance.CallBackResponse(HttpContext, Service.GetInstance());
        }

        [HttpGet]
        public ActionResult Pic()
        {
            PersonPic data = PersonPicHelper.CheckEncyptedEmployeeID(Request.Params["key"].GetString());
            if (data == null || string.IsNullOrWhiteSpace(data.EmpID))
                return RedirectToAction("Binding", "WeChat", new { status = "error" });
            else
            {
                return View("PersonPic", data);
            }
        }
    }
}