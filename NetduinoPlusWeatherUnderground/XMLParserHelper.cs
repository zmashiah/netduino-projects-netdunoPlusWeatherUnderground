using System;
using Microsoft.SPOT;

namespace ZakieM.XMLParserHelper
{
    static class XMLParserHelper
    {
        private static string endTag(string tag) { return "</" + tag + ">"; }

        private static string startTag(string tag) { return "<" + tag + ">"; }

        public static bool amAtTag(string line, string token)
        {
            return (line.IndexOf(startTag(token)) >= 0);
        }

        public static bool amAtEndTag(string line, string token)
        {
            return (line.IndexOf(endTag(token)) >= 0);
        }


        public static string getData(string line, string tag)
        {
            int location = line.IndexOf(startTag(tag));

            if (location >= 0)
            {
                line = line.Substring(location + tag.Length + 2);
                location = line.IndexOf("</" + tag + ">");
                if (location >= 0)
                {
                    return line.Substring(0, location);
                }
            }
            return null;
        }
    }
}
