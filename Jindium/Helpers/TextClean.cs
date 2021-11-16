using System;
using System.Text.RegularExpressions;

namespace Jindium
{
    partial class General
    {
        public static string CleanURL(string path)
        {
            if (path.Contains('.'))
                return path.Substring(0, path.IndexOf("."));
            return path;
        }

        //Remove all chars except letters and numbers, replace spaces with hyphens and make lowercase
        public static string CleanStringForTitle(string str)
        {
            return Regex.Replace(Regex.Replace(Regex.Replace(str.ToLower(), "[^a-z0-9]", "-"), "-+", "-"), " ", "-");
        }
    }
}
