using System;
using System.Net;

namespace Jindium
{
    public partial class PreFabs
    {
        //Gets the credentials from the login form's POST request. 'jinUser' is the username, 'jinPass' is the password.
        public static (string Username, string Password) GetSuppliedCredentials(HttpListenerRequest req)
        {
            var postParams = AppServer.GetPostParams(req.InputStream, req.ContentEncoding);

            //Check if jinUser and jinPass are in the POST request.
            if (!postParams.ContainsKey("jinUser") || !postParams.ContainsKey("jinPass"))
            {
                return (null, null);
            }

            if (!String.IsNullOrEmpty(postParams["jinUser"]) && !String.IsNullOrEmpty(postParams["jinPass"]))
            {
                return (postParams["jinUser"], postParams["jinPass"]);
            }
            else
            {
                return (null, null);
            }
        }

    }
}