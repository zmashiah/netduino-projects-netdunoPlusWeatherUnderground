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
    public class NetworkTimeProtocolService
    {
        #region Variables
        private int m_sleepTime = 60 * 10; // Every 10 hours by default
        private string m_ntpName = "time-a.nist.gov";
        private int m_minutesFromGMT;
        private Timer m_ntpSyncTimer;
        private bool m_hasValidDateTime = false;
        private bool m_lastUpdateOK = false;
        private bool m_updateRunning = false;
        #endregion
        

        #region PrivateFunctions
        private bool ntpTimeUpdater(int retries = 1)
        {
            m_updateRunning = true;
            int i;
            DateTime dtBefore = DateTime.Now;

            Debug.Print("NTP:: Checking NTP server time. Time before: " + dtBefore.ToString());
            for (i = 0; i < retries; i++)
            {
                DateTime dt = GetNetworkTime();
                
                m_lastUpdateOK = (dt.Year > 2011) && (dt.Year < 2100);
                if (m_lastUpdateOK)
                {
                    m_hasValidDateTime = true;
                    SetSystemTime(dt);
                    TimeSpan ts = dt - dtBefore;
                    Debug.Print("NTP:: Delta is: " + ts.Milliseconds.ToString());
                    break;
                }
            }
            m_updateRunning = false;
            return m_lastUpdateOK;

        }
        
        private void ntpTimerCallback(object src)
        {
            if (m_updateRunning == false)
                ntpTimeUpdater(3);
        }

        private void SetSystemTime(DateTime dt)
        {
            TimeSpan timezoneOffset = TimeSpan.FromTicks(m_minutesFromGMT * TimeSpan.TicksPerMinute);
            dt += timezoneOffset;
            Utility.SetLocalTime(dt);
        }
        #endregion

        #region PublicFunctions
        public void SetRefreshRate(int hoursForRefresh) { m_sleepTime = hoursForRefresh * 60; }
        //public void SetNTPHost(string host) { ntpName = host; }
        public int GetTimeZoneOffset() { return m_minutesFromGMT; }
        public void SetTimeZoneOffset(int minutes_from_GMT) { m_minutesFromGMT = minutes_from_GMT; }
        public bool HasValidDateAndTime() { return m_hasValidDateTime; }
        public bool LastUpdteOK() { return m_lastUpdateOK; }

        public DateTime GetNetworkTime()
        {
            DateTime dtNull = new DateTime();
            Socket s = null;

            try
            {
                EndPoint ep = new IPEndPoint(Dns.GetHostEntry(m_ntpName).AddressList[0], 123);
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
            m_lastUpdateOK = ntpTimeUpdater(3);
            return m_lastUpdateOK;
        }
        
        public void begin()
        {
            Debug.Print("NTP:: Starting...\n\tServer\t" + m_ntpName + "\n\tevery\t" + m_sleepTime.ToString() + " minutes");

            // Now we want to see at least once that the system time gets updated
            DoUpdate();
            // Install timer to sync time every 'sleepTime' minutes
            TimeSpan ts = new TimeSpan(0, m_sleepTime, 0);
            m_ntpSyncTimer = new Timer(new TimerCallback(ntpTimerCallback), null, new TimeSpan(1000), ts);
        }
        #endregion
    } // End of NTP  class
}
