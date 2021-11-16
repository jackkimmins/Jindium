using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Jindium
{
    public class Replacelet<TKey, TValue>
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }

        public Replacelet(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    public class Replacelets
    {
        //Apply the replacelets to the input string.
        public static string ApplyReplacelets(string input, Replacelet<string, object>[] replacelets)
        {
            foreach (var replacelet in replacelets)
            {
                input = input.Replace($"@@{replacelet.Key}@@", replacelet.Value.ToString());
            }

            return input;
        }

        public static Replacelet<string, object> New(string key, object value)
        {
            if (value == null)
                value = "";
            return new Replacelet<string, object>(key, value);
        }

        public static List<Replacelet<string, object>> LocalReplacelets()
        {
            return new List<Replacelet<string, object>>();
        }

        //Replaces any left over replacelets with the following values.
        public static string CheckForLeftOverReplacelets(string data)
        {
            data = new Regex(@"@@(.*?)@@").Replace(data, "[INVALID]");
            data = new Regex(@"##(.*?)##").Replace(data, "[SESSION]");
            return data;
        }
    }
}