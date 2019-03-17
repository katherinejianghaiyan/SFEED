using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeChat.Models
{
    public class Message
    {
        public string UserIds { get; set; }
        public string PartyIds { get; set; }
        public string TagIds { get; set; }
        public int AgentId { get; set; }
        public string TextMessage { get; set; }
        public List<News> NewsMessage { get; set; }
    }
}
