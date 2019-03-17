using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class RequestMessage
    {
        /// <summary>
        /// 服务端
        /// </summary>
        public string ToUser { get; set; }

        /// <summary>
        /// 客户端
        /// </summary>
        public string FromUser { get; set; }

        /// <summary>
        /// 请求的消息类型
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// 请求类型为Text时候需要设置Content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 事件请求
        /// </summary>
        public string Event { get; set; }

        /// <summary>
        /// 请求类型为Event时候需要设置,同时需要设置EventKey
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 请求类型为Event时候需要设置,同时需要设置EventName
        /// </summary>
        public string EventKey { get; set; }
    }
}
