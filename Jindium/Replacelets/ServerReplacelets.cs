using System;
using System.Collections.Generic;

namespace Jindium
{
    class ServerReplacelets
    {
        //Sets the static replacelets for the Jindium server. This is used to replace the placeholders in the endpoint outputs.
        private static List<Replacelet<string, object>> replacelets = new List<Replacelet<string, object>>
        {
            new Replacelet<string, object>("ServerName", Environment.MachineName),
            new Replacelet<string, object>("ServerTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
        };

        public static Replacelet<string, object>[] Fetch()
        {
            return replacelets.ToArray();
        }
    }
}