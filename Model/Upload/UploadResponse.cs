using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Upload
{
    public class UploadResponse : Common.BaseResponse
    {
        public string ResultUrl { get; set; }
        public string FileName { get; set; }
    }
}
