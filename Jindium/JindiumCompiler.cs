using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Jindium
{
    class JindiumCompiler
    {
        //Path of website to convert to Jindium
        public JindiumCompiler(string path)
        {
            Path = System.IO.Path.GetFullPath(path);
        }

        public string Path { get; set; }

        private JindiumCompilerConfig ComplieConfig = new JindiumCompilerConfig();

        private List<JindiumFile> Files = new List<JindiumFile>();

        //Load the config file and apply it to the new site.
        private void JindiumConfigFromFile(string file)
        {
            cText.WriteLine("Found Jindium config file. Reading...", "JindiumCompiler", ConsoleColor.Cyan);

            string[] lines = File.ReadAllLines(file);
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=');
                if (parts.Length != 2)
                    continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key)
                {
                    case "site-version":
                        ComplieConfig.SiteVersion = value;
                        break;
                    case "site-name":
                        ComplieConfig.SiteName = General.CleanStringForTitle(value);
                        break;
                    case "site-description":
                        ComplieConfig.SiteDescription = value;
                        break;
                    case "site-author":
                        ComplieConfig.SiteAuthor = value;
                        break;
                    case "php-path":
                        ComplieConfig.phpPath = value;
                        break;
                }
            }

            cText.WriteLine("Jindium config file read. Configuration has been applied!", "JindiumCompiler", ConsoleColor.Cyan);
        }

        //Compile the website into a Jindium site and return the site as an object.
        public JindiumSite Compile(string newSiteName = null)
        {
            cText.WriteLine($"Converting Static Website ({Path}) to new Jindium...", "JindiumCompiler", ConsoleColor.Cyan);

            if (newSiteName != null)
                ComplieConfig.SiteName = General.CleanStringForTitle(newSiteName);

            if (!Directory.Exists(Path))
            {
                cText.WriteLine($"Error: Directory '{Path}' does not exist! Site could not be converted!", "JindiumCompiler", ConsoleColor.Red);
                return JindiumSiteTemplates.SiteNotFound();
            }

            if (File.Exists(Path + "\\jindium.config"))
            {
                JindiumConfigFromFile(Path + "\\jindium.config");
            }
            else
            {
                cText.WriteLine($"Warning: No Jindium config file found! Creating one...", "JindiumCompiler", ConsoleColor.Yellow);
                File.WriteAllText(Path + "\\jindium.config", StaticResp.NewJindiumConfigFile());
            }

            List<string> existingPaths = new List<string>();

            //Load all files into the Jindium site, and check for duplicates.
            string[] allFiles = Directory.GetFiles(Path, "*.*", SearchOption.AllDirectories);
            foreach (ref string file in allFiles.AsSpan())
            {
                if (file.EndsWith("jindium.config"))
                    continue;

                JindiumFile JindiumFile = new JindiumFile();
                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.Name.StartsWith("[S]"))
                {
                    JindiumFile.isSecure = true;
                }

                JindiumFile.MimeType = General.GetMimeType(fileInfo.Extension);
                
                //Minify the file if it is a CSS or JS file.
                switch (JindiumFile.MimeType)
                {
                    case "text/css":
                        JindiumFile.Data = Encoding.UTF8.GetBytes(Minify.CSS(File.ReadAllText(file)));
                        break;
                    case "application/x-javascript":
                        JindiumFile.Data = Encoding.UTF8.GetBytes(Minify.JavaScript(File.ReadAllText(file)));
                        break;
                    default:
                        JindiumFile.Data = File.ReadAllBytes(file);
                        break;
                }

                file = file.Replace(Path, "");
                file = file.Replace("\\", "/");
                if (file == "/index.html")
                    file = "/";
                file = file.Replace("[S]", "");

                JindiumFile.Path = file.Contains('.') ? file.Substring(0, file.IndexOf(".")) : file;

                if (existingPaths.Contains(JindiumFile.Path))
                    cText.WriteLine($"Error, the mapped path '{JindiumFile.Path}' already exists. Endpoints with this name will not be accessible.", "JindiumCompiler", ConsoleColor.Red);

                existingPaths.Add(JindiumFile.Path);

                cText.WriteLine($"Processing file '{file}'...", "JindiumCompiler", ConsoleColor.Cyan);

                Files.Add(JindiumFile);
            }

            JindiumSite site = new JindiumSite(ComplieConfig.SiteName);

            site.Config = ComplieConfig;

            site.SetFiles(Files);

            cText.WriteLine($"Done! '{ComplieConfig.SiteName}' has now been converted to a Jindium site.", "JindiumCompiler", ConsoleColor.Cyan);

            return site;
        }
    }
}
