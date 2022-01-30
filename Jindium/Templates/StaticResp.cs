using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jindium
{
    public static class StaticResp
    {
        //Put a comment at the top of text Jindium files.
        public static string FileTopComment()
        {
            return $@"<!--
   _ _           _ _
  (_(_)         | (_)
   _ _ _ __   __| |_ _   _ _ __ ___
  | | | '_ \ / _` | | | | | '_ ` _ \
  | | | | | | (_| | | |_| | | | | | |
  | |_|_| |_|\__,_|_|\__,_|_| |_| |_|
 _/ |
|__/

 - Jindium Server - Version {CONFIG.VERSION} -

-->
";
        }

        public static string NewJindiumConfigFile(JindiumCompilerConfig complieConfig = null)
        {
            if (complieConfig == null)
            {
                complieConfig = new JindiumCompilerConfig();
            }

            string[] lines = new string[]
            {
                "# Jindium Config File",
                "# Date Created: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                "site-version=1",
                "site-name=" + General.CleanStringForTitle(complieConfig.SiteName),
                "site-description=" + complieConfig.SiteDescription,
                "site-author=" + complieConfig.SiteAuthor,
                "php-path=" + complieConfig.phpPath
            };

            return string.Join("\n", lines);
        }

        public static string ReturnStaticError(string code, string siteName)
        {
            return @"<!DOCTYPE html><html lang=""en""><head> <meta charset=""UTF-8""> <meta http-equiv=""X-UA-Compatible"" content=""IE=edge""> <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""> <title>" + code + @"</title> <style>body{margin:0;color:#444;background-color:#475bce;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:80%;width:100vw;height:100vh;display:flex;justify-content:center;align-items:center}h2{font-size:1.2em}#header,#page{-webkit-box-shadow:0 0 4px 0 rgba(0,0,0,.6);-moz-box-shadow:0 0 4px 0 rgba(0,0,0,.6);box-shadow:0 0 4px 0 rgba(0,0,0,.6)}#page{background-color:#FFF;width:40%;padding:20px}#header{padding:2px;text-align:center;background-color:#C55042;color:#FFF}#content{padding:4px 0 34px 0;border-bottom:5px #c54f4277 solid}a{text-decoration:none}</style></head><body> <div id=""page""> <div id=""header""> <h1>ERROR " + code + @"</h1> </div><div id=""content""> <h2>" + siteName + @"</h2> <p>The requested URL was not found on this Jindium Server.</p><P>Please ensure that the URL is correct and try again.</p><a href=""/"">Return to Homepage</a> </div></div></body></html>";
        }

        public static string ReturnStaticWelcome(string siteName)
        {
            return @"<!DOCTYPE html><html lang=""en""><head> <meta charset=""UTF-8""> <meta http-equiv=""X-UA-Compatible"" content=""IE=edge""> <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""> <title>" + siteName + @"</title> <style>body{margin:0;color:#444;background-color:#475bce;font-family:'Segoe UI',Tahoma,Geneva,Verdana,sans-serif;font-size:80%;width:100vw;height:100vh;display:flex;justify-content:center;align-items:center}h2{font-size:1.2em}#header,#page{-webkit-box-shadow:0 0 4px 0 rgba(0,0,0,.6);-moz-box-shadow:0 0 4px 0 rgba(0,0,0,.6);box-shadow:0 0 4px 0 rgba(0,0,0,.6)}#page{background-color:#FFF;width:40%;padding:20px}#header{padding:2px;text-align:center;background-color:#4e9d26;color:#FFF}#content{padding:4px 0 34px 0;border-bottom:5px #c54f4277 solid}a{text-decoration:none}</style></head><body> <div id=""page""> <div id=""header""> <h1>Welcome to Jindium!</h1> </div><div id=""content""> <h2>Welcome to this instance of Jindium!</h2> <p>This Jindium Server is called '" + siteName + @"'.</p> </div></div></body></html>";
        }

        public static JindiumFile ErrorNotFound(string siteName)
        {
            return new JindiumFile(Encoding.UTF8.GetBytes(ReturnStaticError("404", siteName + " encountered an error")), 404);
        }

        public static JindiumFile ErrorNoPermission(string value = "No permission, you must be authenticated to access this endpoint.")
        {
            return new JindiumFile(Encoding.UTF8.GetBytes(ReturnStaticError("403", value)), 403);
        }

        public static byte[] ConvertText(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        public static JindiumFile GenTextResp(string text, int statusCode = 200, bool isSecure = false)
        {
            JindiumFile file = new JindiumFile(Encoding.UTF8.GetBytes(text), statusCode);
            file.isSecure = isSecure;
            return file;
        }

        public static JindiumFile WelcomePage(string siteName)
        {
            return new JindiumFile(Encoding.UTF8.GetBytes(ReturnStaticWelcome(siteName)), 404);
        }
    }
}
