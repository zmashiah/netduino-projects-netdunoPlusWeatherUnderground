using System;
using Microsoft.SPOT;

namespace ZakieM.XMLParserHelper
{
    static class XMLParserHelper
    {
        public static string endTag(string tag) { return "</" + tag + ">"; }

        public static string startTag(string tag) { return "<" + tag + ">"; }

        public static string getData(string line, string tag)
        {
            string st = startTag(tag);
            int location = line.IndexOf(st);

            if (location >= 0)
            {
                line = line.Substring(location + st.Length);
                location = line.IndexOf(endTag(tag));
                if (location >= 0)
                {
                    return line.Substring(0, location);
                }
            }
            return null;
        }
    }
}
