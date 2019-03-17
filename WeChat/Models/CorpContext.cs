using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class CorpContext
    {
        private string _touser = string.Empty;
        private string _toparty = string.Empty;
        private string _totag = string.Empty;
        private string _msgtype = string.Empty;
        private int _agentid = 0;


        /// <summary>
        /// @all表示所有用户，多个用户使用|分隔
        /// </summary>
        public string touser
        {
            get { return _touser; }
            set { _touser = value; }
        }

        /// <summary>
        /// @all表示所有部门，多个部门使用|分隔
        /// 当touser为@all时候，此字段失效
        /// </summary>
        public string toparty
        {
            get { return _toparty; }
            set { _toparty = value; }
        }

        /// <summary>
        /// @all表示所有tag，多个tag使用|分隔，当touser为@all时候，此字段失效
        /// </summary>
        public string totag
        {
            get { return _totag; }
            set { _totag = value; }
        }

        /// <summary>
        /// 消息类型为text
        /// </summary>
        public string msgtype
        {
            get { return _msgtype; }
            set { _msgtype = value; }
        }

        /// <summary>
        /// 应用id，注意应用id的部门权限，此处程序不做控制，可能发生微信调用失败
        /// </summary>
        public int agentid
        {
            get { return _agentid; }
            set { _agentid = value; }
        }
    }
}
