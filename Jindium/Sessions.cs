using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Jindium
{
    public class Session
    {
        //Create a new session with an optional session ID.
        //Could be used if you want to create a session with a specific ID or you are trying to recreate a session.
        public Session(string sessID = null)
        {
            if (sessID == null)
            {
                this.SessionId = Guid.NewGuid().ToString();
            }
            else
            {
                this.SessionId = sessID;
            }
            
            this.SessionData = new Dictionary<string, object>();
        }

        //Gen a new session ID. This does need remade in the future.
        public static string RandomSessionId()
        {
            return Guid.NewGuid().ToString();
        }

        //Create a new session ID and return it in the response.
        public static void AddSessionCookie(HttpListenerResponse res, string sessID)
        {
            res.Cookies.Add(new Cookie("JindiumSessID", sessID));
        }

        //Gets all of the session data from the cookie stored in the request.
        public static Dictionary<string, object> GetSessionData(AppServer app, HttpListenerRequest req)
        {
            string sessID = req.Cookies["JindiumSessID"].Value;
            return GetSessionData(app, sessID);
        }

        //Applys replacelets to the response from the user's session. Like the user's username, etc.
        public static string ApplySessionReplacelets(string input, AppServer app, string sessID)
        {
            foreach (var replacelet in GetSessionData(app, sessID))
            {
                input = input.Replace($"##{replacelet.Key}##", replacelet.Value.ToString());
            }

            return input;
        }

        //Gets the session data for a given session ID from an Jindium AppServer.
        //It auto gets the session key from the cookie and finds value from the key.
        public static object GetSessionValue(AppServer app, HttpListenerRequest req, string key)
        {
            //Check if cookie exists
            if (req.Cookies["JindiumSessID"] == null)
            {
                return "[ERROR]";
            }

            string sessID = req.Cookies["JindiumSessID"].Value;
            var sessionData = GetSessionData(app, sessID);

            if (!sessionData.ContainsKey(key))
                return null;

            return sessionData[key];
        }

        //Gets all the session data from a session ID. This would noramlly be stored in a cookie on the client.
        public static Dictionary<string, object> GetSessionData(AppServer app, string sessID)
        {
            var session = app.Sessions.Where(s => s.SessionId == sessID).FirstOrDefault();

            if (session == null)
                return new Dictionary<string, object>();

            return session.SessionData;
        }

        public string SessionId { get; set; }
        public Dictionary<string, object> SessionData { get; set; }
    }
}