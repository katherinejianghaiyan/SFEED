using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.MBSales
{
    public class MBInfoMast:Table.TableData
    {
        public string id { get; set; }
        public string mbNo { get; set; }
        public string mbName { get; set; }
        public string mbSex { get; set; }
        public string BirthDay { get; set; }
        public string mbTel { get; set; }
        public string mbMobile { get; set; }
        public string mbMail { get; set; }
        public string mbAdd { get; set; }
        public string CardStatus { get; set; }
        public string RemainAmt { get; set; }
        public string CardScore { get; set; }
        public string CardExpireDate { get; set; }

    }
}
