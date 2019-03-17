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
using WeChatEvent;
using Utils.Common;

namespace ADEN.Controllers
{
    public class WeChatApiController : ApiController
    {

        [HttpPost]
        public Model.Common.BaseResponse BindPerson([FromBody] Person data)
        {
            return PersonPicHelper.BindingUser(data);
        }

        /// <summary>
        /// 获取用户保存图片数据
        /// </summary>
        /// <param name="userKey"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        [HttpPost]
        public Model.Common.BaseResponse SavePersonPic([FromBody] PersonPic data)
        {
            Model.Common.BaseResponse response = new Model.Common.BaseResponse();
            try
            {
                if (PersonPicHelper.SavePicData(data)) response.Status = "ok";
                else response.Status = "error";
            }
            catch { response.Status="error"; }
            return response;
        }
    }
}
