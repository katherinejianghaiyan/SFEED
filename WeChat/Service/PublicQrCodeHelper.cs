using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils.Net;

namespace WeChat.Service
{
    public static class PublicQrCodeHelper
    {
        public static string GetQrCode()
        {
            string postString = "{\"action_name\": \"QR_LIMIT_STR_SCENE\", \"action_info\": {\"scene\": {\"scene_str\": \"adenannualparty\"}}}";
            string url = "https://api.weixin.qq.com/cgi-bin/qrcode/create?access_token=meZcYq8OH2syUwxyv_LSJwaLZaBXbJuM6Iv-Sbrz_F9w2REerLnu4rtr6c2GlkbiEFJFByrY6vN85cFvqkP3TBCwEd86bNSQ5ttXhK9vMTHmEdkGtS_BlLFqq_3M1jhpOKVhAJAHDO";
            return new HttpHelper().Post(url, postString);
        }
    }
}
