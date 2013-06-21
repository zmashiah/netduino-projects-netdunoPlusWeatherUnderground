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
        public static DateTime startTime;
        public static NetworkTimeProtocolService ntpService = new NetworkTimeProtocolService();
        public static WeatherUndergroundData weatherData;
        //public static SerialPort displaySerial = new SerialPort(SerialPorts.COM2, 31250, Parity.None, 8, StopBits.One);
        public static SerialDisplay displaySerial = new SerialDisplay(SerialPorts.COM2, 9600);
        public static InterruptPort onboardButton = new InterruptPort(Pins.ONBOARD_SW1, false, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
        public static OutputPort onboardLed = new OutputPort(Pins.ONBOARD_LED, false);
        public static bool getWeatherNow = false;
        public static bool sendTimeNow = false;
        public static Timer displayTimeTimer;
        public static Timer displayWeatherTimer;
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
                if (getWeatherNow) // every 10 minutes
                {
                    onboardLed.Write(true);
                    weatherData.Read();
                    weatherData.Show();
                    weatherData.Serialize(displaySerial);
                    getWeatherNow = false;
                    onboardLed.Write(false);
                }
                if (sendTimeNow)
                {
                    onboardLed.Write(true);
                    serializeTime();
                    sendTimeNow = false;
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
            getWeatherNow = true;
            sendTimeNow = true;
        }

        static void updateDisplayTimeHandler(object src)    { sendTimeNow = true; }

        static void updateWeatherHandler(object src)        { getWeatherNow = true; }
        #endregion

        #region InitFunctions
        private static void setup()
        {
            // Initialization
            displaySerial.Open();
            Debug.EnableGCMessages(false);
            InitLocation();
            // Start the NTP
            ntpService.SetRefreshRate(5);
            ntpService.begin();
            startTime = DateTime.Now;

            // Set timers for updating weather and display time
            // Time on display will be update every 3 minutes
            // Weather will be fetched every 10 minutes
            TimeSpan tsEveryThreeMinutes = new TimeSpan(0, 3, 0);
            TimeSpan tsEveryTenMinutes = new TimeSpan(0, 10, 0);

            displayTimeTimer = new Timer(new TimerCallback(updateDisplayTimeHandler), null, new TimeSpan(0,0,3), tsEveryThreeMinutes);
            displayWeatherTimer = new Timer(new TimerCallback(updateWeatherHandler), null, new TimeSpan(0,0,4), tsEveryTenMinutes);
            onboardButton.OnInterrupt += new NativeEventHandler(onboardButtonPress);
        }
        

        
        private static void InitLocation()
        {
            GeoLoactionByIP myLocation = new GeoLoactionByIP();
            AskGeoService askGeo = new AskGeoService();

            myLocation.queryLocation();
            
            weatherData = new WeatherUndergroundData("9d9ca34336e25c7c", myLocation.latitude, myLocation.longitude);

            askGeo.queryLocation(myLocation.latitude, myLocation.longitude);
            
            int minutes = (int)(askGeo.currentOffserMS / 1000 / 60);
            Debug.Print("\tTimezone offset (+DST)=" + minutes + " minutes");
            ntpService.SetTimeZoneOffset(minutes);
        }

        #endregion

        #region time serialization functions
        private static void serializeTime()
        {
            if (ntpService.LastUpdteOK() == false)
                ntpService.DoUpdate(); // If last time we failed, retry...

            DateTime dt = DateTime.Now;
            if (ntpService.HasValidDateAndTime())
            {
                Debug.Print("Updating display time: " + dt.ToString());
                displaySerial.SerializeString("[T" + dt.Year + "/" + dt.Month + "/" + dt.Day + " " + dt.Hour + ":" + dt.Minute + ":" + dt.Second + "]");
            }
        }
        #endregion


    }
}