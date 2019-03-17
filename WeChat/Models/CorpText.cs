using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class CorpText
    {
        private string _content = string.Empty;
        private string _safe = string.Empty;

        /// <summary>
        /// 发送的文本内容
        /// </summary>
        public string content
        {
            get { return _content; }
            set { _content = value; }
        }

        /// <summary>
        /// 是否加密，0-否，1-是
        /// </summary>
        public string safe
        {
            get { return _safe; }
            set { _safe = value; }
        }
    }
}
