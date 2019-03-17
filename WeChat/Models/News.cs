using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class News
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _picurl = string.Empty;
        private string _url = string.Empty;


        /// <summary>
        /// 图文标题
        /// </summary>
        public string title
        {
            get { return _title; }
            set { _title = value; }
        }

        /// <summary>
        /// 图文描述，多图文时候此字段失效
        /// </summary>
        public string description
        {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// 图片Url
        /// </summary>
        public string picurl
        {
            get { return _picurl; }
            set { _picurl = value; }
        }

        /// <summary>
        /// 点击图片跳转url
        /// </summary>
        public string url
        {
            get { return _url; }
            set { _url = value; }
        }
    }
}
