using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Aspose.Cells;
using System.IO;
using System.Web;
using System.Configuration;

namespace Utils.Excel
{
    public class ExcelHelper
    {
        private static ExcelHelper instance = new ExcelHelper();
        private ExcelHelper() { }
        public static ExcelHelper GetInstance() { return instance; }

        /// <summary>
        /// 根据文件名,加载并读取Excel,转换成DataSet数据集返回
        /// DataTable中所有数据类型都为String,获取后,需自行根据需求转换
        /// 空值Sheet将不会添加到DataSet中
        /// </summary>
        /// <param name="file">完整文件路径名</param>
        /// <param name="sheetNames">Sheet名集合</param>
        /// <param name="method">1: 符合SheetNames中的表名, 0: 排除SheetNames中的表名, 其他值: 全部</param>
        /// <returns>数据集</returns>
        public DataSet GetDataSet(string file, List<string> sheetNames, int method, int startRow, int startCol)
        {
            if((sheetNames == null || sheetNames.Count == 0) && method.Equals(1)) return null;
            Workbook wb = new Workbook(file);
            if (wb == null) return null;
            DataSet ds = new DataSet();
            DataTable data = null;
            int maxRow = 0;
            int maxCol = 0;
            foreach (Worksheet ws in wb.Worksheets)
            {
                if (ws.Cells.MaxRow <= 0 || ws.Cells.MaxColumn <= 0) continue;
                if (method.Equals(1) && !sheetNames.Contains(ws.Name)) continue;
                if (method.Equals(0) && sheetNames.Contains(ws.Name)) continue;
                maxRow = ws.Cells.MaxRow + 1 - startRow;
                maxCol = ws.Cells.MaxColumn + 1 - startCol;
                if(maxRow <= 0 || maxCol <= 0) continue;
                data = ws.Cells.ExportDataTableAsString(startRow, startCol, maxRow, maxCol, true);
                if (data != null && data.Rows.Count > 0)
                {
                    data.TableName = ws.Name;
                    ds.Tables.Add(data);
                }
            }
            return ds;
        }

        /// <summary>
        /// 根据文件名和SheetName,加载并读取Excel,返回DataTable
        /// SheetName为空,返回第一个Sheet的内容
        /// SheeName匹配不到,则返回空对象
        /// </summary>
        /// <param name="file">完整文件路径名</param>
        /// <param name="sheetName">SheeName</param>
        /// <returns>DataTable</returns>
        public DataTable GetDataTable(string file, string sheetName, int startRow, int startCol)
        {
            Workbook wb = new Workbook(file);
            Worksheet ws = null;
            if (string.IsNullOrWhiteSpace(sheetName)) ws = wb.Worksheets[0];
            else ws = wb.Worksheets[sheetName];
            if (ws == null) return null;
            int maxRow = ws.Cells.MaxRow + 1 - startRow;
            int maxCol = ws.Cells.MaxColumn + 1 - startCol;
            if (maxRow <= 0 || maxCol <= 0) return null;
            return ws.Cells.ExportDataTableAsString(startRow, startCol, maxRow, maxCol, true);
        }

        public string SaveDataSet(DataSet ds, string path, string extension)
        {
            try
            {
                Workbook wb = new Workbook();
                bool firstSheet = true;
                Worksheet sheet = null;
                foreach (DataTable data in ds.Tables)
                {
                    if(firstSheet)
                    {
                        sheet = wb.Worksheets[0];
                        sheet.Name = data.TableName;
                        firstSheet = false;
                    }
                    else sheet = wb.Worksheets.Add(data.TableName);
                    sheet.Cells.ImportDataTable(data, true, 0, 0);
                }
                string fileName = System.IO.Path.Combine(path, Guid.NewGuid().ToString() + extension);
                wb.Save(fileName);
                return fileName;
            }
            catch { return string.Empty; }
        }

        public string SaveDataTable(DataTable data, string path, string extension)
        {
            try
            {
                Workbook wb = new Workbook();
                wb.Worksheets[0].Name = data.TableName;
                wb.Worksheets[0].Cells.ImportDataTable(data, true, 0, 0);
                string fileName = System.IO.Path.Combine(path, Guid.NewGuid().ToString() + extension);
                wb.Save(fileName);
                return fileName;
            }
            catch { return string.Empty; } 
        }

        public string SaveDemoDataTable(DataTable data, string demoFile, string path, string extension)
        {
            WorkbookDesigner designer = new WorkbookDesigner();
            designer.Workbook = new Workbook(demoFile);
            designer.SetDataSource(data);
            designer.Process();
            string fileName = System.IO.Path.Combine(path, Guid.NewGuid().ToString() + extension);
            designer.Workbook.Save(fileName);
            return fileName;
        }


        public void DownloadExcelFile(MemoryStream ms)
        {
            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.Buffer = true;
            response.ContentType = "Application/ms-excel";
            response.AddHeader("Content-Disposition", "attachment;filename=report.xls");
            response.ContentEncoding = Encoding.Default;
            response.BinaryWrite(ms.ToArray());
            response.Flush();
            response.End();
        }


    }
}
