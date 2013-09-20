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
    public class GeoLocationByIP
    {
        public enum GL_ParserState { Start = 0, Info = 1 }

        public string country;
        public string region;
        public string city;
        public string latitude;
        public string longitude;
        public string externalIPAddress;
        private GL_ParserState currentState;

        private string queryString 
        {
            get { return "http://www.geobytes.com/IpLocator.htm?GetLocation&Template=XML.txt"; }
        }

        

        /// <summary>
        /// Reads the stream of reply from the geobytes and parses it into the variables
        /// </summary>
        /// <param name="s">the stream of the reply</param>
        private void _ProcessLine(string line)
        {
            const string _info = "info";
            const string _country = "country";
            const string _region = "region";
            const string _city = "city";
            const string _latitude = "latitude";
            const string _longitude = "longitude";
            const string _ipaddress = "ipaddress";

            switch (currentState)
            {
                case GL_ParserState.Start:
                    if (XMLParserHelper.amAtTag(line, _info))
                        currentState = GL_ParserState.Info;
                    break;
                case GL_ParserState.Info:
                    if (XMLParserHelper.amAtTag(line, _country))
                        this.country = XMLParserHelper.getData(line, _country);
                    else
                        if (XMLParserHelper.amAtTag(line, _region))
                            this.region = XMLParserHelper.getData(line, _region);
                        else
                            if (XMLParserHelper.amAtTag(line, _city))
                                this.city = XMLParserHelper.getData(line, _city);
                            else
                                if (XMLParserHelper.amAtTag(line, _latitude))
                                    this.latitude = XMLParserHelper.getData(line, _latitude);
                                else
                                    if (XMLParserHelper.amAtTag(line, _longitude))
                                        this.longitude = XMLParserHelper.getData(line, _longitude);
                                    else
                                        if (XMLParserHelper.amAtTag(line, _ipaddress))
                                            this.externalIPAddress = XMLParserHelper.getData(line, _ipaddress);
                                        else
                                        {
                                            if (XMLParserHelper.amAtEndTag(line, _info))
                                                currentState = GL_ParserState.Start;
                                        }
                    break;
            }
        }
        


        private void _readStream(Stream s)
        {
            string parser = string.Empty;
            byte[] buffer = new byte[2];
            int bytesRead = 0;
            bool endOfStreamReached = false;

            Debug.Print("GL:: Read @" + DateTime.Now.ToString());
            System.GC.WaitForPendingFinalizers();
            Debug.GC(true);

            while (endOfStreamReached == false)
            {
                try
                {
                    int c = s.Read(buffer, 0, 1);
                    if (c > 0)
                    {
                        bytesRead++;
                        if ((buffer[0] != 10))
                            parser += (char)buffer[0];
                        else
                        {
                            _ProcessLine(parser);
                            parser = string.Empty;
                        }
                    }
                    else
                        endOfStreamReached = true;
                }
                catch (Exception e)
                {
                    Debug.Print("GL:: Error reading: " + e.Message);
                    GC.WaitForPendingFinalizers();
                    Debug.GC(true);
                    return;
                }
            }

            Debug.Print("GL:: Done @" + DateTime.Now.ToString());
            GC.WaitForPendingFinalizers();
            Debug.GC(true);
        }

        /// <summary>
        /// Queries the Geobytes service for location of the IP address.
        /// </summary>
        /// <param name="ip"></param>
        public void queryLocation(string ip = null)
        {
            string q = (ip == null) ? queryString : queryString + ip;
            HttpWebRequest request = HttpWebRequest.Create(q) as HttpWebRequest;
            request.UserAgent = ".NETMF";
            WebResponse resp = null;

            try
            {
                resp = request.GetResponse();
            }
            catch (Exception e)
            {
                Debug.Print("ERROR: Exception for geobytes.com" + e.ToString());
            }

            if (resp != null)
            {
                Stream respStream = resp.GetResponseStream();
                respStream.ReadTimeout = 5000;
                _readStream(respStream);
                resp.Close();
                resp.Dispose();
            }
            request.Dispose();
        }

    }
}
