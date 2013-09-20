using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO.Ports;
using System.Collections;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

using GeoTimeServices;
using WeatherReporter.WeatherUnderground;
using ZakieM.tools.HW.Serial.Display;


namespace WeatherUndergroundToSerialDisplay
{

    public static class WeatherInformationReporter
    {
        #region Variables
        private static DateTime m_startTime;
        private static NetworkTimeProtocolService m_ntpService = new NetworkTimeProtocolService();
        private static WeatherUndergroundData m_weatherData;

        //public static SerialPort displaySerial = new SerialPort(SerialPorts.COM2, 31250, Parity.None, 8, StopBits.One);
        public static SerialDisplay m_displaySerial = new SerialDisplay(SerialPorts.COM2, 9600);
        public static InterruptPort onboardButton = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
        public static OutputPort onboardLed = new OutputPort(Pins.ONBOARD_LED, false);

        public static bool m_getWeatherNow = false;
        public static bool m_sendTimeNow = false;
        public static Timer m_displayTimeTimer;
        public static Timer m_displayWeatherTimer;
        #endregion // Variables

        #region Main
        /// <summary>
        /// Retrieves weather current and forecast information from Google Weather and display
        /// it on the LCD with time ticking clock
        /// </summary>
        public static void Main()
        {
            setup();

            // Loop (forever)
            for (; ; )
            {
                if (m_getWeatherNow) // every 10 minutes
                {
                    onboardLed.Write(true);
                    m_weatherData.Read();
                    m_weatherData.Show();
                    m_weatherData.Serialize(m_displaySerial);
                    m_getWeatherNow = false;
                    onboardLed.Write(false);
                }
                if (m_sendTimeNow)
                {
                    onboardLed.Write(true);
                    serializeTime();
                    m_sendTimeNow = false;
                    onboardLed.Write(false);
                }
                Thread.Sleep(1000);
            }
        }
        #endregion

        #region interrupt and timer functions
        static void onboardButtonPress(uint d1, uint d2, DateTime dt)
        {
            // Update both time and weather information to screen
            m_getWeatherNow = true;
            m_sendTimeNow = true;
        }

        static void updateDisplayTimeHandler(object src)    { m_sendTimeNow = true; }

        static void updateWeatherHandler(object src)        { m_getWeatherNow = true; }
        #endregion

        #region InitFunctions
        private static void setup()
        {
            // Initialization
            m_displaySerial.Open();
            Debug.EnableGCMessages(false);

            InitLocation();
            
            // Start the NTP
            m_ntpService.SetRefreshRate(5);
            m_ntpService.begin();
            m_startTime = DateTime.Now;

            // Set timers for updating weather and display time
            // Time on display will be update every 3 minutes
            // Weather will be fetched every 10 minutes
            TimeSpan tsEveryThreeMinutes = new TimeSpan(0, 3, 0);
            TimeSpan tsEveryTenMinutes = new TimeSpan(0, 10, 0);

            m_displayTimeTimer = new Timer(new TimerCallback(updateDisplayTimeHandler), null, new TimeSpan(0,0,3), tsEveryThreeMinutes);
            m_displayWeatherTimer = new Timer(new TimerCallback(updateWeatherHandler), null, new TimeSpan(0,0,4), tsEveryTenMinutes);
            onboardButton.OnInterrupt += new NativeEventHandler(onboardButtonPress);
        }
        

        
        private static void InitLocation()
        {
            //GeoLoactionByIP myLocation = new GeoLoactionByIP();
            AskGeoService askGeo = new AskGeoService();

            //myLocation.queryLocation();
            
            //m_weatherData = new WeatherUndergroundData("9d9ca34336e25c7c", myLocation.latitude, myLocation.longitude);
            string lat = "32.1833"; // "32.0667";
            string longi = "34.8667";// "34.7667";
            //askGeo.queryLocation(myLocation.latitude, myLocation.longitude);
            askGeo.queryLocation(lat, longi);
            m_weatherData = new WeatherUndergroundData("9d9ca34336e25c7c", lat, longi);

            int minutes = (int)(askGeo.currentOffserMS / 1000 / 60);
            Debug.Print("\tTZ offset(+DST)=" + minutes + " minutes");
            m_ntpService.SetTimeZoneOffset(minutes);
        }

        #endregion

        #region time serialization functions
        private static void serializeTime()
        {
            if (m_ntpService.LastUpdteOK() == false)
                m_ntpService.DoUpdate(); // If last time we failed, retry...

            DateTime dt = DateTime.Now;
            if (m_ntpService.HasValidDateAndTime())
            {
                Debug.Print("Send time 2 disp: " + dt.ToString());
                m_displaySerial.SerializeString("[T" + dt.Year + "/" + dt.Month + "/" + dt.Day + " " + dt.Hour + ":" + dt.Minute + ":" + dt.Second + "]");
            }
        }
        #endregion


    }
}