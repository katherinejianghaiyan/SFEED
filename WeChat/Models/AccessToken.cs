using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class AccessToken
    {
        public DateTime date { get; set; }
        public string access_token { get; set; }
        public string expires_in { get; set; }
    }
}
