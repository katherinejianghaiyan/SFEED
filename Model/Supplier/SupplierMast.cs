using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Supplier
{
    public class SupplierMast : Table.TableData
    {
        public string SupplierGuid { get; set; }
        public string SupplierCode { get; set; }
        public string SupplierName { get; set; }
        public string SupplierNameEn { get; set; }
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string TelNbr { get; set; }
        public string ContactName { get; set; }
        public string EmailAddress { get; set; }
        public string MobileNbr { get; set; }
        public int Status { get; set; }
        public int IsDel { get; set; }
        public List<SupplierSite> Sites { get; set; }
    }
}
