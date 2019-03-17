using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using System.Threading.Tasks;
using Microsoft.VisualBasic.Devices;

namespace SEMI.UpdateApp
{
    public class UpdateAppHelper
    {
        private static UpdateAppHelper instance = new UpdateAppHelper();
        private UpdateAppHelper() { }

        public static UpdateAppHelper GetInstance() { return instance; }

        public bool UpdateFiles(string uri, string appPath)
        {
            //删除已有旧文件
            DeleteFiles(appPath);
            try
            {
                byte[] data = new System.Net.WebClient().DownloadData(uri);
                if (data.Length > 0)
                {
                    string path = System.IO.Path.Combine(appPath, "update.data");
                    System.IO.File.WriteAllBytes(path, data); //下载数据
                    SetFiles(path);
                    return true;
                }
                else return false;
            }
            catch { return false; }
        }
        
        private void SetFiles(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                string[] oldFilePaths = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(filePath));
                List<int> exceptIndex = new List<int>();
                using (ZipFile zip = ZipFile.Read(filePath))
                {
                    foreach (ZipEntry zipEntry in zip)
                    {
                        bool toExtract = true;
                        if (exceptIndex.Count <= oldFilePaths.Length)
                        {
                            for (int i = 0; i < oldFilePaths.Length; i++)
                            {
                                if (exceptIndex.Contains(i)) continue;
                                if (System.IO.Path.GetFileName(oldFilePaths[i]).Equals(zipEntry.FileName))
                                {
                                    exceptIndex.Add(i);
                                    try { new Computer().FileSystem.RenameFile(oldFilePaths[i], System.IO.Path.GetFileName(zipEntry.FileName) + ".old"); }
                                    catch { toExtract = false; }
                                    break;
                                }
                            }
                        }
                        if (toExtract) zipEntry.Extract();
                    }
                }
            }
        }

        private void DeleteFiles(string filePath)
        {
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            string[] oldFilePaths = System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(filePath));
            if (oldFilePaths.Length > 0)
            {
                for (int i = 0; i < oldFilePaths.Length; i++)
                    try
                    {
                        if (System.IO.Path.GetExtension(oldFilePaths[i]).Equals(".old")
                            || System.IO.Path.GetFileName(oldFilePaths[i]).Equals("update.data")) System.IO.File.Delete(oldFilePaths[i]);
                    }
                    catch { }
            }
        }
    }
}
