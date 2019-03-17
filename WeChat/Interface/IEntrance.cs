using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Threading.Tasks;

namespace WeChat.Interface
{
    public interface IEntrance
    {
        string ReturnMessage(XmlDocument xml);
    }
}
