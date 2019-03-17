using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Utils.Common;


namespace SEMI.Upload
{
    public class UploadDatasHelper : Common.BaseDataHelper
    {
        private static UploadDatasHelper instance = new UploadDatasHelper();

        private UploadDatasHelper() { }

        public static UploadDatasHelper GetInstance() { return instance; }

        public string Process(string uploadType, string file)
        {
            Model.Upload.UploadType type = (Model.Upload.UploadType)Enum.Parse(typeof(Model.Upload.UploadType), uploadType);
            string resultUrl = string.Empty;
            switch (type)
            {
                case Model.Upload.UploadType.PurchasePriceList : resultUrl = 
                    Price.ItemPriceHelper.GetInstance().UploadPurchasePriceList(file); break;
                case Model.Upload.UploadType.SalesPriceList: resultUrl =
                    Price.ItemPriceHelper.GetInstance().UploadSalesPriceList(file); break;
                case Model.Upload.UploadType.RM : resultUrl = 
                    MastData.ItemDataHelper.GetInstance().UploadRMDatas(file); break;
                case Model.Upload.UploadType.BOM: resultUrl = BOM.BOMHelper.GetInstance().UploadBOMs(file); break;
                case Model.Upload.UploadType.FG: resultUrl = MastData.ItemDataHelper.GetInstance().UploadFGDatas(file); break;
                default: throw new Exception("上传类型错误,请联系管理员");
            }
            return resultUrl;
        }

        public string GetDemoFile(string uploadType)
        {
            string sql = "select top 1 FileName from [dbo].[tblUploadDemo] where UploadType='" + uploadType + "' and Status=1";
            string fileName = new Utils.Database.SqlServer.DBHelper(_conn).GetDataScalar(sql).GetString();
            return fileName;        
        }
    }
}
