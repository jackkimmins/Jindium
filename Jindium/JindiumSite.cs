using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Jindium
{
    public enum SiteStatus
    {
        SiteOK = 1,
        SiteNotFound = 2
    };

    [Serializable]
    public class JindiumSite
    {
        public System.Version JindiumVersion { get; private set; }
        public SiteStatus Status { get; private set; }
        private List<JindiumFile> Files = new List<JindiumFile>();
        public JindiumCompilerConfig Config = new JindiumCompilerConfig();

        //Create a new site with specified data.
        public JindiumSite(string siteName = "TestSite", SiteStatus SiteStatus = SiteStatus.SiteOK, System.Version version = null)
        {
            Config.SiteName = siteName;
            this.Status = SiteStatus;

            this.JindiumVersion = version ?? CONFIG.VERSION;
        }

        //Setter for the site's files.
        public void SetFiles(List<JindiumFile> files)
        {
            Files = files;
        }

        //Check if an endpoint (page) exists in the site.
        public bool CheckEndpointExists(string path)
        {
            return Files.Where(f => f.Path == path).Count() == 1 ? true : false;
        }

        //Create a static endpoint in the site. This could be used to override the default page or to create a new page.
        public void NewStaticJindiumFile(string path, Func<JindiumFile, JindiumFile> handler)
        {
            var file = handler(new JindiumFile());
            file.Path = path;
            cText.WriteLine($"Initialised New Static File. Path: '{path}'", "JIN", ConsoleColor.Yellow);

            if (CheckEndpointExists(path))
            {
                cText.WriteLine("Endpoint '" + path + "' already exists, overriding...", "JIN", ConsoleColor.Yellow);
                Files.RemoveAll(f => f.Path == path);
            }
                
            Files.Add(file);
        }

        //Return the JindiumFile from the site at the given path.
        public JindiumFile GetEndpoint(string path)
        {
            //If it doesn't exist, return an error page.
            if (CheckEndpointExists(path))
            {
                var file = Files.Where(f => f.Path == path).FirstOrDefault();

                //If the file is a PHP file, run it using the PHP interpreter.
                //This is a little hacky, but it works and does enable the PHP interpreter to be used for Jindium projects.
                if (file.MimeType == "text/x-httpd-php")
                {
                    var returnFile = file;
                    returnFile.Data = PHP.ParseAndExecute(Config.phpPath, file.Data);
                    cText.WriteLine($"{file.Method} {path}", "PHP", ConsoleColor.DarkMagenta);
                    return returnFile;
                }

                cText.WriteLine($"{file.Method} {path}", "REQ", ConsoleColor.Green);
                return file;
            }
            else
            {
                cText.WriteLine($"ERR {path}", "REQ", ConsoleColor.Red);

                if (path == "/")
                    return StaticResp.WelcomePage(Config.SiteName);

                return StaticResp.ErrorNotFound(Config.SiteName);
            }   
        }

        //Save the object to a file. using the serializer. This is a little hacky and could be done better, but it works.
        public static void Save(JindiumSite site, string fileName = null)
        {
            if (fileName == null)
                fileName = site.Config.SiteName + ".jin";

            try
            {
                Stream ms = File.OpenWrite(fileName);
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                #pragma warning disable SYSLIB0011 // Type or member is obsolete
                formatter.Serialize(ms, site);
                #pragma warning restore SYSLIB0011 // Type or member is obsolete
                ms.Flush();
                ms.Close();
                ms.Dispose();

                cText.WriteLine($"Saved site to '{fileName}'", "JIN", ConsoleColor.Yellow);
            }
            catch (Exception e)
            {
                cText.WriteLine(e.Message, "ERR", ConsoleColor.Red);
                cText.WriteLine($"Could not save {site.Config.SiteName} as a Jindium Site!", "ERR", ConsoleColor.Red);
            }
        }

        //Load a site from a file and return it as a JindiumSite object.
        public static JindiumSite Load(String fileName)
        {
            if (!File.Exists(fileName))
            {
                return JindiumSiteTemplates.SiteNotFound();
            }

            try
            {
                System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                FileStream fs = File.Open(fileName, FileMode.Open);

                #pragma warning disable SYSLIB0011 // Type or member is obsolete
                object obj = formatter.Deserialize(fs);
                #pragma warning restore SYSLIB0011 // Type or member is obsolete
                JindiumSite site = (JindiumSite)obj;
                fs.Flush();
                fs.Close();
                fs.Dispose();

                if (site.JindiumVersion != CONFIG.VERSION)
                {
                    cText.WriteLine("Site version is not compatible with this Jindium version. It might not work correctly.", "WARN", ConsoleColor.Yellow);
                }

                return site;
            }
            catch (Exception e)
            {
                cText.WriteLine(e.Message, "ERR", ConsoleColor.Red);
                return JindiumSiteTemplates.SiteNotFound();
            }
        }
    }

    //Site templates.
    class JindiumSiteTemplates
    {
        public static JindiumSite SiteNotFound()
        {
            return new JindiumSite("SiteNotFound", SiteStatus.SiteNotFound);
        }
    }
}
