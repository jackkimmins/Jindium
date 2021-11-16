using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Jindium
{
    [Serializable]
    public class JindiumCompilerConfig
    {
        public string SiteVersion { get; set; }
        public string SiteName { get; set; }
        public string SiteDescription { get; set; }
        public string SiteAuthor { get; set; }
        public string phpPath { get; set; }

        public JindiumCompilerConfig()
        {
            this.SiteVersion = "0";
            this.SiteName = "A Server Instance";
            this.SiteDescription = "Just another Jindium Server";
            this.SiteAuthor = "Jindium";
            this.phpPath = "php";
        }
    }
}