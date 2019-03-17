using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Aspose.Cells;
using System.Web;
using System.Data;
using System.Configuration;
using Utils.Common;
using SEMI.MastData;


namespace SEMI.Report
{
    public class DownloadReport:Common.BaseDataHelper
    {
        private static DownloadReport instance = new DownloadReport();
        private DownloadReport() { }
        public static DownloadReport GetInstance() { return instance; }


        public MemoryStream ExportExcelSiteReport(string empCode, string startdate, string enddate, string all, string group, string getsite, string language)
        {
            try
            {
                                                                                                              
                List<Model.SPD.SPDMast> siteReport = SEMI.MastData.MastDataHelper.GetInstance().GetSPDReport(empCode, startdate, enddate, all, group, getsite, language).ToList();

                Workbook excel = new Workbook();

                Worksheet dataSheet = excel.Worksheets[0];

                dataSheet.Name = getsite+"销售统计报告";

                Cells dataCells = dataSheet.Cells;

                dataCells[0, 0].PutValue("成本中心/Cost Center");
                dataCells[0, 1].PutValue("日/Date");
                dataCells[0, 2].PutValue("客户/Consumer");
                dataCells[0, 3].PutValue("商品编号/Item Code");
                dataCells[0, 4].PutValue("商品名称/Item Name");
                dataCells[0, 4].PutValue("销售数量/Quantity");
                dataCells[0, 4].PutValue("销售毛额/Gross Turnover");

                return excel.SaveToStream();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }
    }
}
