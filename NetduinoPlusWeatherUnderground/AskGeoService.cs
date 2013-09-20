using System;
using System.Threading;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net;
using Microsoft.SPOT.Time;

using SecretLabs.NETMF;
using SecretLabs.NETMF.Hardware;


using ZakieM.XMLParserHelper;

namespace GeoTimeServices
{
    public class AskGeoService
    {
        public long currentOffserMS;

        private const string accountID = "32";                 // <<<< Put up your account ID, PLEASE!
        private const string apiKey = "1qoio30dtb5gdu7lr00i9saar4jb4fro65hbarruif6i39vrdace"; // <<<< Put here your API key, PLEASE!
        private string queryString(string latitude, string longitude)
        {
            return "http://api.askgeo.com/v1/" + accountID + "/" + apiKey + "/query.json?databases=TimeZone&points=" + latitude + "," + longitude;
        }


        public void queryLocation(string latitude, string longitude)
        {
            string q = queryString(latitude, longitude);

            Debug.Print("AG:: Read @" + DateTime.Now.ToString());
            HttpWebRequest request = HttpWebRequest.Create(q) as HttpWebRequest;
            WebResponse resp = null;

            try
            {
                resp = request.GetResponse();
            }
            catch (Exception e)
            {
                Debug.Print("ERROR: Exception for askgeo.com" + e.ToString());
            }

            if (resp != null)
            {
                Stream respStream = resp.GetResponseStream();
                respStream.ReadTimeout = 5000;

                System.Threading.Thread.Sleep(1000); // Wait a while
                StreamReader sr = new StreamReader(respStream);
                string parser = sr.ReadToEnd();
                int location;

                location = parser.IndexOf("CurrentOffsetMs\":");
                if (location > 0)
                {
                    parser = parser.Substring(location);
                    int start = parser.IndexOf(':') + 1;
                    location = parser.IndexOf(',');
                    string currentOffsetMsString = parser.Substring(start, location - start);

                    currentOffserMS = Convert.ToInt32(currentOffsetMsString);
                }
                resp.Close();
            }
            request.Dispose();
            Debug.Print("AG:: Done @" + DateTime.Now.ToString());
        }
    }
}
