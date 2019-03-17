using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Common
{
    public class ErrorMast
    {
        /// <summary>
        /// 错误页面返回类型, 暂时定义有TEXT,MODAL,HTML
        /// Text的话直接返回文本内容Html
        /// Modal的话返回Modal框Html
        /// Html的话返回SectionContent内容
        /// </summary>
        public string ErrorType { get; set; }

        public string ErrorCode { get; set; }
        public string ErrorTitle { get; set; }
        public string ErrorMsg { get; set; }
    }
}
