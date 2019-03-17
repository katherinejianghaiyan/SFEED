using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http.Headers;

namespace ADEN.Models
{
    public class ReadMultipartFormDataStreamProvider : System.Net.Http.MultipartFormDataStreamProvider
    {
        private List<Model.Upload.UploadFileName> fileNames;

        private bool _setFileNames = false;

        public ReadMultipartFormDataStreamProvider(string path, bool setFileNames)
            : base(path)
        {
            _setFileNames = setFileNames;
        }

        public ReadMultipartFormDataStreamProvider(string path) : base(path) { }

        public override string GetLocalFileName(HttpContentHeaders headers)
        {
            string fileName = headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            string newName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(fileName);
            if (_setFileNames)
            {
                if (fileNames == null) fileNames = new List<Model.Upload.UploadFileName>();
                fileNames.Add(new Model.Upload.UploadFileName() { OrignFileName = fileName, NewFileName = newName });
            }
            return newName;
        }

        public List<Model.Upload.UploadFileName> GetFormDataFileNames()
        {
            return this.fileNames;
        }
    }
}