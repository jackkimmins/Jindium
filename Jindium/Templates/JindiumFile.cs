using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jindium
{
    [Serializable]
    public class JindiumFile
    {
        public string MimeType { get; set; }
        public string Path { get; set; }
        public string FullPath { get; set; }
        public byte[] Data { get; set; }
        public string Method { get; set; }
        public int StatusCode { get; set; }
        public bool isSecure { get; set; } = false;

        public JindiumFile()
        {
            StatusCode = 200;
            Method = "GET";
            MimeType = "text/plain";
            Data = StaticResp.ConvertText("Error, file is empty.");
        }

        public JindiumFile(byte[] data, int statusCode = 200, string path = null, string method = "GET", string mimeType = "text/html", string fullPath = null)
        {
            MimeType = mimeType;
            Path = path;
            Method = method;
            Data = data;
            StatusCode = statusCode;
            FullPath = fullPath;
        }

        public string DataAsString()
        {
            return System.Text.Encoding.Default.GetString(Data);
        }

        public void DataFromString(string data)
        {
            Data = Encoding.UTF8.GetBytes(data);
        }
    }
}
