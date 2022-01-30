using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Jindium
{
    public class httpArgs : EventArgs
    {
        public httpArgs(HttpListenerRequest req, HttpListenerResponse res, bool isAuthenticated = false)
        {
            this.req = req;
            this.res = res;
            this.Path = req.RawUrl;
            this.isAuthenticated = isAuthenticated;
        }

        public HttpListenerRequest req { get; set; }
        public HttpListenerResponse res { get; set; }
        public string Path { get; set; }
        public bool isAuthenticated { get; private set; }
    }

    public partial class AppServer
    {
        private HttpListener listener = new HttpListener();
        public List<Session> Sessions { get; set; }
        public List<Replacelet<string, object>> PublicReplacelets { get; set; }
        public string ContentPath { get; set; }
        public string URL { get; set; }

        //Specify the URL to listen on.
        public AppServer(string _URL)
        {
            URL = _URL;
            Sessions = new List<Session>();
            PublicReplacelets = new List<Replacelet<string, object>>();
            ContentPath = null;
        }

        //Add a custom header(s) to the response.
        private static Task AddCustomHeaders(ref httpArgs args)
        {
            args.res.Headers.Add("Server", "Jindium");

            return Task.CompletedTask;
        }

        //Set a public replacelet value.
        public void Replacelet(string replaceletName, object value)
        {
            var replacelet = PublicReplacelets.Where(r => r.Key == replaceletName).FirstOrDefault();

            //Check if the replacelet exists.
            if (replacelet != null)
            {
                replacelet.Value = value;
            }
            else
            {
                PublicReplacelets.Add(Replacelets.New(replaceletName, value));
            }
        }

        //Get a public replacelet value.
        public object Replacelet(string replaceletName)
        {
            var replacelet = PublicReplacelets.Where(r => r.Key == replaceletName).FirstOrDefault();
            return (replacelet != null) ? replacelet.Value : 0;
        }

        //Create a response for the user from a JindiumFile with the specified arguments.
        public async Task ResponseGen(httpArgs args, JindiumFile file, string contentType = null)
        {
            if (file.isSecure && !args.isAuthenticated)
            {
                args.res.StatusCode = 401;
                args.res.StatusDescription = "Unauthorized";
                await args.res.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Unauthorized"), 0, 12);
                return;
            }

            byte[] byteData = file.Data;

            //Set the response data.
            args.res.ContentType = contentType ?? file.MimeType;  
            args.res.ContentEncoding = Encoding.UTF8;
            args.res.StatusCode = file.StatusCode;
            await AddCustomHeaders(ref args);

            if (byteData == null)
            {
                //Check if the file exists at the specified path.
                if (System.IO.File.Exists(file.FullPath))
                {
                    //If it does, load it into byteData;
                    byteData = System.IO.File.ReadAllBytes(file.FullPath);
                }
                else
                {
                    //If it doesn't, return a 404.
                    args.res.StatusCode = 404;
                    args.res.StatusDescription = "Not Found";
                    byteData = StaticResp.ConvertText("File was not found.");
                    args.res.ContentType = "text/html";
                }
            }

            //Apply replacelets to text-base files only, not things like images or binary files.
            if (file.MimeType.StartsWith("text/"))
            {
                string textData = Encoding.UTF8.GetString(byteData);

                if (args.req.Cookies["JindiumSessID"] != null)
                {
                    textData = Session.ApplySessionReplacelets(textData, this, args.req.Cookies["JindiumSessID"].Value);
                }    

                textData = Replacelets.ApplyReplacelets(textData, PublicReplacelets.ToArray());
                textData = Replacelets.ApplyReplacelets(textData, ServerReplacelets.Fetch());
                textData = Replacelets.CheckForLeftOverReplacelets(textData);

                byteData = Encoding.UTF8.GetBytes(textData);

                if (file.MimeType == "text/html")
                {
                    byteData = General.Combine(Encoding.UTF8.GetBytes(StaticResp.FileTopComment()), byteData);
                }
            }

            args.res.ContentLength64 = byteData.LongLength;

            //Send the response asynchronously.
            await args.res.OutputStream.WriteAsync(byteData, 0, byteData.Length);
        }

        public event EventHandler<httpArgs> OnGET;
        public event EventHandler<httpArgs> OnPOST;

        //Return a bool if the user is authenticated.
        private bool CheckIfAuthenticated(HttpListenerRequest req)
        {
            if (req.Cookies["JindiumSessID"] == null)
                return false;

            return Sessions.Any(x => x.SessionId == req.Cookies["JindiumSessID"].Value) ? true : false;
        }

        //Handle all incoming requests.
        public async Task HandleIncomingConnections()
        {
            while (true)
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse res = ctx.Response;

                switch (req.HttpMethod)
                {
                    case "GET":
                        OnGET?.Invoke(null, new httpArgs(req, res, CheckIfAuthenticated(req)));
                        break;
                    case "POST":
                        OnPOST?.Invoke(null, new httpArgs(req, res, CheckIfAuthenticated(req)));
                        break;
                    default:
                        cText.WriteLine("Unhandled HTTP Method: " + req.HttpMethod, "AppServer", ConsoleColor.Yellow);
                        break;
                }


                res.Close();
            }
        }

        //Output the server's status.
        private void DisplaySiteStatusMSG(SiteStatus siteStatus)
        {
            switch ((int)siteStatus)
            {
                case 1:
                    //cText.WriteLine("Site is OK", "SITE");
                    break;
                case 2:
                    cText.WriteLine("Site could not be loaded. The .jin file could not be found. Default site has been loaded.", "SITE", ConsoleColor.Red);
                    break;
                default:
                    cText.WriteLine("Unknown Site State", "SITE", ConsoleColor.Yellow);
                    break;
            }
        }

        //Start the Jindium server
        public void Start(ref JindiumSite site)
        {
            DisplaySiteStatusMSG(site.Status);
            Console.Title = site.Config.SiteName + " - Jindium Server";
            listener.Prefixes.Add(URL);

            ContentPath = site.Config.CompileFolder;

            //Start the listener.
            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                cText.WriteLine($"Could not start the Jindimum on the following URI: {URL}", "ERR", ConsoleColor.Red);
                cText.WriteLine("Reason: " + ex.Message, "ERR", ConsoleColor.Red);
                return;
            }

            cText.WriteLine($"Jindium is Online! ({URL})", "INFO", ConsoleColor.Green);

            //Handle incoming connections.
            try
            {
                Task listenTask = HandleIncomingConnections();
                listenTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                cText.WriteLine($"An error occured while handling incoming connections: {ex.Message}", "ERR", ConsoleColor.Red);
            }
            
            listener.Close();
        }
    }
}
