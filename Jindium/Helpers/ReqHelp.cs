using System;
using System.Text;
using System.Collections.Generic;

namespace Jindium
{
    public partial class AppServer
    {
        //Get the POST data from an input Stream.
        public static Dictionary<string, string> GetPostParams(System.IO.Stream input, Encoding encoding)
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>();

            using (var reader = new System.IO.StreamReader(input, encoding))
            {
                string parms = reader.ReadToEnd();

                string[] rawParams = parms.Split('&');
                foreach (string param in rawParams)
                {
                    string[] kvPair = param.Split('=');
                    string key = kvPair[0];
                    string value = System.Web.HttpUtility.UrlDecode(kvPair[1]);
                    postParams.Add(key, value);
                }
            }

            return postParams;
        }
    }
}