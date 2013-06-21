/* =====================================================================================
 * NTP Protocol synchronization code
 * In this code an implementation of NTP client and auto-detection classes for actual
 * timezone. To avoid too large assemblies, I did not use any XML or JSON parsing
 * classes. If your application is using XML or JSON parsing, the use of those here
 * could reduce code size.
 * 
 * How it Works:
 *      By quering geobytes.com and askGeo.com I am finding the actual time-zone of the
 *      device:
 *          Through geobytes.com I am finding my location in the world
 *          Through askGeo.com I am finding the timezone data of this location
 * Limitations:
 *      This auto-detection will work if the device is connected to the internet through
 *      standard NAT configuration, but with remote proxies the auto-detection will be
 *      wrong as it will detect the timezone of the proxy itself. For most applications
 *      however it is good enough.
 *           
 *      Don't expect a GPS accuracy for your location. It will probably get you the
 *      name of city/town where your ISP is, and not your actual location, still for
 *      timezone purposes this good enough.
 *      
 *      Please read http://www.askgeo.com Terms of Use and legal disclaimer
 *      
 * Special NOTE:
 *      In this code an API Key that is registered to the author of this code.
 *      >>>> Please modify it to your registered key <<<<
 *      Search below for <<<< to location of where you need to put your key
 * =====================================================================================
 * By: Zakie Mashiah
 * You may use the code, modify or include in any project you have.
 * Please read http://www.askgeo.com Terms of Use and legal disclaimer
 * Please read http://www.geobytes.com Terms and Conditions
 * =====================================================================================
 */
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
    public class GeoLoactionByIP
    {
        public enum glParserState
        {
            glStart = 0,
            glInfo = 1
        }

        public string country;
        public string region;
        public string city;
        public string latitude;
        public string longitude;
        public string externalIPAddress;
        private glParserState currentState;

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
                case glParserState.glStart:
                    if (line.IndexOf(XMLParserHelper.startTag(_info)) >= 0)
                        currentState = glParserState.glInfo;
                    break;
                case glParserState.glInfo:
                    if (line.IndexOf(XMLParserHelper.startTag(_country)) >= 0)
                        this.country = XMLParserHelper.getData(line, _country);
                    else
                        if (line.IndexOf(XMLParserHelper.startTag(_region)) >= 0)
                            this.region = XMLParserHelper.getData(line, _region);
                        else
                            if (line.IndexOf(XMLParserHelper.startTag(_city)) >= 0)
                                this.city = XMLParserHelper.getData(line, _city);
                            else
                                if (line.IndexOf(XMLParserHelper.startTag(_latitude)) >= 0)
                                    this.latitude = XMLParserHelper.getData(line, _latitude);
                                else
                                    if (line.IndexOf(XMLParserHelper.startTag(_longitude)) >= 0)
                                        this.longitude = XMLParserHelper.getData(line, _longitude);
                                    else
                                        if (line.IndexOf(XMLParserHelper.startTag(_ipaddress)) >= 0)
                                            this.externalIPAddress = XMLParserHelper.getData(line, _ipaddress);
                                        else
                                        {
                                            if (line.IndexOf(XMLParserHelper.endTag(_info)) >= 0)
                                                currentState = glParserState.glStart;
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

            Debug.Print("GL:: Reading @" + DateTime.Now.ToString());
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

            Debug.Print("AG:: Reading @" + DateTime.Now.ToString());
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





    public class NetworkTimeProtocolService
    {
        #region Variables
        private int sleepTime = 60 * 10; // Every 10 hours by default
        private string ntpName = "time-a.nist.gov";
        private int minutesFromGMT;
        private Timer ntpSyncTimer;
        private bool _hasValidDateTime = false;
        private bool _lastUpdateOK = false;
        private bool _updateRunning = false;
        #endregion
        

        #region PrivateFunctions
        private bool ntpTimeUpdater(int retries = 1)
        {
            _updateRunning = true;
            int i;
            DateTime dtBefore = DateTime.Now;

            Debug.Print("NTP:: Checking NTP server time. Time before: " + dtBefore.ToString());
            for (i = 0; i < retries; i++)
            {
                DateTime dt = GetNetworkTime();
                
                _lastUpdateOK = (dt.Year > 2011) && (dt.Year < 2100);
                if (_lastUpdateOK)
                {
                    _hasValidDateTime = true;
                    SetSystemTime(dt);
                    TimeSpan ts = dt - dtBefore;
                    Debug.Print("NTP:: Delta is: " + ts.Milliseconds.ToString());
                    break;
                }
            }
            _updateRunning = false;
            return _lastUpdateOK;

        }
        
        private void ntpTimerCallback(object src)
        {
            if (_updateRunning == false)
                ntpTimeUpdater(3);
        }

        private void SetSystemTime(DateTime dt)
        {
            TimeSpan timezoneOffset = TimeSpan.FromTicks(minutesFromGMT * TimeSpan.TicksPerMinute);
            dt += timezoneOffset;
            Utility.SetLocalTime(dt);
        }
        #endregion

        #region PublicFunctions
        public void SetRefreshRate(int hoursForRefresh) { sleepTime = hoursForRefresh * 60; }
        //public void SetNTPHost(string host) { ntpName = host; }
        public int GetTimeZoneOffset() { return minutesFromGMT; }
        public void SetTimeZoneOffset(int minutes_from_GMT) { minutesFromGMT = minutes_from_GMT; }
        public bool HasValidDateAndTime() { return _hasValidDateTime; }
        public bool LastUpdteOK() { return _lastUpdateOK; }

        public DateTime GetNetworkTime()
        {
            DateTime dtNull = new DateTime();
            Socket s = null;

            try
            {
                EndPoint ep = new IPEndPoint(Dns.GetHostEntry(ntpName).AddressList[0], 123);
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                s.Connect(ep);
                byte[] ntpData = new byte[48]; // RFC 2030
                Array.Clear(ntpData, 0, 48);
                ntpData[0] = 0x1B;

                s.SendTo(ntpData, ep);
                if (s.Poll(10 * 1000 * 1000, SelectMode.SelectRead)) // Waiting an answer for 30s, if nothing: timeout
                {

                    s.ReceiveFrom(ntpData, ref ep); // Receive Time
                    byte offsetTransmitTime = 40;
                    ulong intpart = 0;
                    ulong fractpart = 0;
                    for (int i = 0; i <= 3; i++) intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
                    for (int i = 4; i <= 7; i++) fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
                    s.Close();
                    TimeSpan timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
                    DateTime dateTime = new DateTime(1900, 1, 1);
                    dateTime += timeSpan;

                    TimeSpan offsetAmount = TimeZone.CurrentTimeZone.GetUtcOffset(dateTime);
                    DateTime networkDateTime = (dateTime + offsetAmount);
                    return networkDateTime;
                }
                s.Close();
            }
            catch
            {
                try { s.Close(); }
                catch { }
            }
            return dtNull;
        }

        public bool DoUpdate()
        {
            _lastUpdateOK = ntpTimeUpdater(3);
            return _lastUpdateOK;
        }
        
        public void begin()
        {
            Debug.Print("NTP:: Starting...\n\tServer\t" + ntpName + "\n\tevery\t" + sleepTime.ToString() + " minutes");

            // Now we want to see at least once that the system time gets updated
            DoUpdate();
            // Install timer to sync time every 'sleepTime' minutes
            TimeSpan ts = new TimeSpan(0, sleepTime, 0);
            ntpSyncTimer = new Timer(new TimerCallback(ntpTimerCallback), null, new TimeSpan(1000), ts);
        }
        #endregion
    } // End of NTP  class
}
