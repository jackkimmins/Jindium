using System;
using Jindium;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JindiumDemo
{
    class Program
    {
        private static JindiumSite site = new JindiumSite();
        private static AppServer app = new AppServer("http://localhost:8000/");

        //The event handler for the GET event.
        public static async void OnGET(object sender, httpArgs args)
        {
            await app.ResponseGen(args, site.GetEndpoint(General.CleanURL(args.Path)));
        }

        //The event handler for the POST event.
        public static async void OnPOST(object sender, httpArgs args)
        {
            switch (args.Path)
            {
                case "/auth":
                    //Authenticate the user.
                    var creds = PreFabs.GetSuppliedCredentials(args.req);

                    if (creds.Username == "admin" && creds.Password == "admin")
                    {
                        //Start the session.
                        Session session = new Session();
                        session.SessionData.Add("username", creds.Username);
                        Session.AddSessionCookie(args.res, session.SessionId);

                        app.Sessions.Add(session);
                        
                        //After a successful login, redirect to the dashboard.
                        args.res.Redirect("/dashboard");
                    }
                    else
                    {
                        //If the credentials are incorrect, display an error.
                        await app.ResponseGen(args, StaticResp.GenTextResp("Invalid Username or Password"));
                    }
                    break;
                default:
                    await app.ResponseGen(args, StaticResp.GenTextResp("Endpoint not found: " + args.Path, 404));
                    break;
            }
        }

        //Entry point for the program.
        static void Main(string[] args)
        {
            //Load a static Jindium site from file.
            //site = JindiumSite.Load("my-personal-website.jin");

            //Create and compile a new Jindium site and save it to file "Site".
            JindiumCompiler toJindium = new JindiumCompiler("Site");
            site = toJindium.Compile();

            //Set an endpoint override at the specified path.
            site.NewStaticJindiumFile("/about", (file) =>
            {
                file.isSecure = false;
                file.MimeType = "text/html";
                // file.Data = StaticResp.ConvertText("Hello, world! <h1>@@ServerName@@</h1>");
                return file;
            });

            site.NewStaticJindiumFile("/dashboard", (file) =>
            {
                file.DataFromString("<h1>Dashboard</h1><p>Welcome ##username## to the system. This Jindium server is hosted on @@ServerName@@.</p>");
                file.MimeType = "text/html";
                file.isSecure = true;
                return file;
            });

            //Create a public replacelet for the site.
            app.PublicReplacelets.Add(Replacelets.New("Author", "John Smith"));

            //Save the compiled site to file.
            JindiumSite.Save(site, site.Config.SiteName + ".jin");

            //Initialise event handlers for the Jindium server.
            app.OnGET += OnGET;
            app.OnPOST += OnPOST;

            //Start the Jindium server.
            app.Start(ref site);
        }
    }
}
