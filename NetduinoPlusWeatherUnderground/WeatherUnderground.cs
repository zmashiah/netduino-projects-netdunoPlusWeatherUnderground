/* ====================================================================================
 * WeatherUnderground.cs - Code to access and parse infromation from the internet
 * weather service of Weather Underground.
 * Ref: http://www.wunderground.com/weather/api/d/documentation.html#logos
 * Sample Snapshot at weather data:
 *  http://api.wunderground.com/api/9d9ca34336e25c7c/conditions/forecast/astronomy/q/32.0849,34.8884.xml
 * ====================================================================================
 * By Zakie Mashiah, 2012
 * You may use, copy or modify the code as you see fit.
 * Crediting the author is required in any public posting or commercial use.
 * ====================================================================================
 */
using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Net;
using System.Collections;
using Microsoft.SPOT;

using ZakieM.XMLParserHelper;
using ZakieM.tools.HW.Serial.Display;

namespace WeatherReporter.WeatherUnderground
{
    public class WeatherUndergroundCurrent
    {
        public string city;
        public string condition;
        public string temperature;
        public string humidity;
        public string windDir;
        public string windSpeed;
        public string pressure;
        public string iconBasename;
        
        public WeatherUndergroundCurrent() { }
    }
    
    public class WeatherUndergroundForecast
    {
        public string dayOfWeek;
        public string highTemperature;
        public string lowTemperature;
        public string condition;
        public string iconBasename;
        public string windSpeed;
        public string windDir;
        public string humidity;
        
        public WeatherUndergroundForecast() { }
    }

    public class WeatherUndergroundMoonPhase
    {
        public string percentIlluminated;
        public string sunsetHour;
        public string sunsetMinute;
        public string sunriseHour;
        public string sunriseMinute;
        
        public WeatherUndergroundMoonPhase() { }
    }

    public class WeatherUndergroundData
    {
        public WeatherUndergroundCurrent m_current;
        public WeatherUndergroundMoonPhase m_astronomy;
        public ArrayList m_forecast;
        
        private WeatherUndergroundForecast m_wuf;

        private string m_queryKey;
        private string m_latitude;
        private string m_longitude;


        public enum WU_ParserState
        {
            Start = 0,
            Response = 1,
            CurrentObservation = 10,
            ObservationLocation = 100,
            Forecast = 20,
            SimpleForecast = 21,
            ForecastDays = 210,
            ForecastDay = 2100,
            Date = 21000,
            High = 21001,
            Low = 21002,
            MaxWind = 21003,
            MoonPhase = 30,
            Sunset = 300,
            Sunrise = 301
        }

        private WU_ParserState m_currentState;
                

        public WeatherUndergroundData(string key, string lat, string longi)
        {
            m_queryKey = key;
            m_latitude = lat;
            m_longitude = longi;

            m_current = new WeatherUndergroundCurrent();
            m_astronomy = new WeatherUndergroundMoonPhase();
            m_forecast = new ArrayList();
        }
        
        private string queryString
        {
            get { return "http://api.wunderground.com/api/" + m_queryKey +
                "/conditions/forecast/astronomy/q/" + m_latitude + "," + m_longitude + ".xml"; }
        }

        
        // Implementation could be changed to XML parser on systems where memory is not as tight as on Netduino Plus.
        private void ProcessWeatherXMLLine(string line)
        {
            const string _response = "response";
            const string _current_observation = "current_observation";
            const string _forecast = "forecast";
            const string _moon_phase = "moon_phase";
            const string _observation_location = "observation_location";
            const string _weather = "weather";
            const string _temp_c = "temp_c";
            const string _relative_humidity = "relative_humidity";
            const string _wind_dir = "wind_dir";
            const string _wind_kph = "wind_kph";
            const string _pressure_mb = "pressure_mb";
            const string _icon = "icon";
            const string _city = "city";
            const string _simpleforecast = "simpleforecast";
            const string _forecastdays = "forecastdays";
            const string _forecastday = "forecastday";
            const string _date = "date";
            const string _high = "high";
            const string _low = "low";
            const string _conditions = "conditions";
            const string _maxwind = "maxwind";
            const string _maxhumidity = "avehumidity";
            const string _weekday_short = "weekday_short";
            const string _celsius = "celsius";
            const string _kph = "kph";
            const string _dir = "dir";
            const string _percentIlluminated = "percentIlluminated";
            const string _sunset = "sunset";
            const string _sunrise = "sunrise";
            const string _hour = "hour";
            const string _minute = "minute";

            switch (m_currentState)
            {
                case WU_ParserState.Start: // Look for the <response>
                    if (XMLParserHelper.amAtTag(line, _response))
                        m_currentState = WU_ParserState.Response;
                    break;
                case WU_ParserState.Response:
                    if (XMLParserHelper.amAtTag(line, _current_observation))
                        m_currentState = WU_ParserState.CurrentObservation;
                    else
                        if (XMLParserHelper.amAtTag(line, _forecast))
                            m_currentState = WU_ParserState.Forecast;
                        else
                            if (XMLParserHelper.amAtTag(line, _moon_phase))
                                m_currentState = WU_ParserState.MoonPhase;
                            else
                                if (XMLParserHelper.amAtEndTag(line, _response))
                                    m_currentState = WU_ParserState.Start;
                    break;
                case WU_ParserState.CurrentObservation:
                    if (XMLParserHelper.amAtTag(line, _observation_location))
                        m_currentState = WU_ParserState.ObservationLocation;
                    else
                        if (XMLParserHelper.amAtTag(line, _weather))
                            this.m_current.condition = XMLParserHelper.getData(line, _weather);
                        else
                            if (XMLParserHelper.amAtTag(line, _temp_c))
                                this.m_current.temperature = XMLParserHelper.getData(line, _temp_c);
                            else
                                if (XMLParserHelper.amAtTag(line, _relative_humidity))
                                    this.m_current.humidity = XMLParserHelper.getData(line, _relative_humidity);
                                else
                                    if (XMLParserHelper.amAtTag(line, _wind_dir))
                                        this.m_current.windDir = XMLParserHelper.getData(line, _wind_dir);
                                    else
                                        if (XMLParserHelper.amAtTag(line, _wind_kph))
                                            this.m_current.windSpeed = XMLParserHelper.getData(line, _wind_kph);
                                        else
                                            if (XMLParserHelper.amAtTag(line, _pressure_mb))
                                                this.m_current.pressure = XMLParserHelper.getData(line, _pressure_mb);
                                            else
                                                if (XMLParserHelper.amAtTag(line, _icon))
                                                    this.m_current.iconBasename = XMLParserHelper.getData(line, _icon);
                                                else
                                                    if (XMLParserHelper.amAtEndTag(line, _current_observation))
                                                        m_currentState = WU_ParserState.Response;
                    break;
                case WU_ParserState.ObservationLocation:
                    if (XMLParserHelper.amAtTag(line, _city))
                        this.m_current.city = XMLParserHelper.getData(line, _city);
                    else
                        if (XMLParserHelper.amAtEndTag(line, _observation_location))
                            m_currentState = WU_ParserState.CurrentObservation;
                    break;
                case WU_ParserState.Forecast:
                    if (XMLParserHelper.amAtTag(line, _simpleforecast))
                        m_currentState = WU_ParserState.SimpleForecast;
                    else
                        if (XMLParserHelper.amAtEndTag(line, _forecast))
                            m_currentState = WU_ParserState.Response;
                    break;
                case WU_ParserState.SimpleForecast:
                    if (XMLParserHelper.amAtTag(line, _forecastdays))
                    {
                        m_currentState = WU_ParserState.ForecastDays;
                        this.m_forecast.Clear();
                    }
                    else
                        if (XMLParserHelper.amAtEndTag(line, _simpleforecast))
                            m_currentState = WU_ParserState.Forecast;
                    break;
                case WU_ParserState.ForecastDays:
                    if (XMLParserHelper.amAtTag(line, _forecastday))
                    {
                        m_currentState = WU_ParserState.ForecastDay;
                        m_wuf = new WeatherUndergroundForecast();
                    }
                    else
                        if (XMLParserHelper.amAtEndTag(line, _forecastdays))
                            m_currentState = WU_ParserState.SimpleForecast;
                    break;
                case WU_ParserState.ForecastDay:
                    if (XMLParserHelper.amAtTag(line, _date))
                        m_currentState = WU_ParserState.Date;
                    else
                        if (XMLParserHelper.amAtTag(line, _high))
                            m_currentState = WU_ParserState.High;
                        else
                            if (XMLParserHelper.amAtTag(line, _low))
                                m_currentState = WU_ParserState.Low;
                            else
                                if (XMLParserHelper.amAtTag(line, _conditions))
                                    m_wuf.condition = XMLParserHelper.getData(line, _conditions);
                                else
                                    if (XMLParserHelper.amAtTag(line, _icon))
                                        m_wuf.iconBasename = XMLParserHelper.getData(line, _icon);
                                    else
                                        if (XMLParserHelper.amAtTag(line, _maxwind))
                                            m_currentState = WU_ParserState.MaxWind;
                                        else
                                            if (XMLParserHelper.amAtTag(line, _maxhumidity))
                                                m_wuf.humidity = XMLParserHelper.getData(line, _maxhumidity);
                                            else
                                                if (XMLParserHelper.amAtEndTag(line, _forecastday))
                                                {
                                                    m_currentState = WU_ParserState.ForecastDays;
                                                    this.m_forecast.Add(m_wuf);
                                                    m_wuf = new WeatherUndergroundForecast();
                                                }
                    break;
                case WU_ParserState.Date:
                    if (XMLParserHelper.amAtTag(line, _weekday_short))
                        m_wuf.dayOfWeek = XMLParserHelper.getData(line, _weekday_short);
                    else
                        if (XMLParserHelper.amAtEndTag(line, _date))
                            m_currentState = WU_ParserState.ForecastDay;
                    break;
                case WU_ParserState.High:
                    if (XMLParserHelper.amAtTag(line, _celsius))
                        m_wuf.highTemperature = XMLParserHelper.getData(line, _celsius);
                    else
                        if (XMLParserHelper.amAtEndTag(line, _high))
                            m_currentState = WU_ParserState.ForecastDay;
                    break;
                case WU_ParserState.Low:
                    if (XMLParserHelper.amAtTag(line, _celsius))
                        m_wuf.lowTemperature = XMLParserHelper.getData(line, _celsius);
                    else
                        if (XMLParserHelper.amAtEndTag(line, _low))
                            m_currentState = WU_ParserState.ForecastDay;
                    break;
                case WU_ParserState.MaxWind:
                    if (XMLParserHelper.amAtTag(line, _kph))
                        m_wuf.windSpeed = XMLParserHelper.getData(line, _kph);
                    else
                        if (XMLParserHelper.amAtTag(line, _dir))
                            m_wuf.windDir = XMLParserHelper.getData(line, _dir);
                        else
                            if (XMLParserHelper.amAtEndTag(line, _maxwind))
                                m_currentState = WU_ParserState.ForecastDay;
                    break;
                case WU_ParserState.MoonPhase:
                    if (XMLParserHelper.amAtTag(line, _percentIlluminated))
                        this.m_astronomy.percentIlluminated = XMLParserHelper.getData(line, _percentIlluminated);
                    else
                        if (XMLParserHelper.amAtTag(line, _sunset))
                            m_currentState = WU_ParserState.Sunset;
                        else
                            if (XMLParserHelper.amAtTag(line, _sunrise))
                                m_currentState = WU_ParserState.Sunrise;
                            else
                                if (XMLParserHelper.amAtEndTag(line, _moon_phase))
                                    m_currentState = WU_ParserState.Response;
                    break;
                case WU_ParserState.Sunset:
                    if (XMLParserHelper.amAtTag(line, _hour))
                        this.m_astronomy.sunsetHour = XMLParserHelper.getData(line, _hour);
                    else
                        if (XMLParserHelper.amAtTag(line, _minute))
                            this.m_astronomy.sunsetMinute = XMLParserHelper.getData(line, _minute);
                        else
                            if (XMLParserHelper.amAtEndTag(line, _sunset))
                                m_currentState = WU_ParserState.MoonPhase;
                    break;
                case WU_ParserState.Sunrise:
                    if (XMLParserHelper.amAtTag(line, _hour))
                        this.m_astronomy.sunriseHour = XMLParserHelper.getData(line, _hour);
                    else
                        if (XMLParserHelper.amAtTag(line, _minute))
                            this.m_astronomy.sunriseMinute = XMLParserHelper.getData(line, _minute);
                        else
                            if (XMLParserHelper.amAtEndTag(line, _sunrise))
                                m_currentState = WU_ParserState.MoonPhase;
                    break;
            }
        }



        private void ReadWeatherUndergroundXmlStream(Stream s)
        {
            string parser = string.Empty;
            byte[] buffer = new byte[2];
            int bytesRead = 0;
            bool endOfStreamReached = false;


            Debug.Print("WU:: Reading @" + DateTime.Now.ToString());
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
                        {
                            parser += (char)buffer[0];
                        }
                        else
                        {
                            ProcessWeatherXMLLine(parser);
                            parser = string.Empty;
                        }
                    }
                    else
                        endOfStreamReached = true;
                }
                catch (Exception e)
                {
                    Debug.Print("WU:: Error reading from socket: " + e.Message);
                    GC.WaitForPendingFinalizers();
                    Debug.GC(true);
                    return;
                }
            }

            Debug.Print("WU:: Done @" + DateTime.Now.ToString());
            GC.WaitForPendingFinalizers();
            Debug.GC(true);
        }



        private void ReadWeatherunderground(string url)
        {
            // Create an HTTP Web request.
            HttpWebRequest request = HttpWebRequest.Create(url) as HttpWebRequest;

            // Set request.KeepAlive to use a persistent connection. 
            request.KeepAlive = true;

            // Get a response from the server.
            WebResponse resp = null;

            try
            {
                resp = request.GetResponse();
            }
            catch (Exception e)
            {
                Debug.Print("ERROR: Exception for WU" +
                    e.ToString());
            }

            // Get the network response stream to read the page data.
            if (resp != null)
            {
                Stream respStream = resp.GetResponseStream();
                respStream.ReadTimeout = 5000;
                ReadWeatherUndergroundXmlStream(respStream);
                resp.Close();
                resp.Dispose();
            }
            request.Dispose();

        }



        public void Read()
        {
            ReadWeatherunderground(queryString);
        }



        private void ShowForecastData(WeatherUndergroundForecast wuf)
        {
            Debug.Print("Day: " + wuf.dayOfWeek + " Condition:" + wuf.condition + 
                " Low: " + wuf.lowTemperature + " High: " + wuf.highTemperature + " Icon: " + wuf.iconBasename +
                " Humidity: " + wuf.humidity + " % Wind:" + wuf.windDir + " " + wuf.windSpeed + "K/h");
        }

        private void showWeather()
        {
            string[] vect = new string[] { "City    :" + this.m_current.city,
                                           "Cond.   :" + this.m_current.condition,
                                           #if USE_FDEGREES // We do not use F degrees so supress it
                                           "Temp.   :" + this.m_current.TempF.ToString() + " F",
                                           #else
                                           "Temp.   :" + this.m_current.temperature + " C",
                                           #endif
                                           "Humidity:" + this.m_current.humidity + " %", // Google set the value as Humidity: value
                                           "Icon    :" + this.m_current.iconBasename,
                                           "Wind    :" + this.m_current.windDir + " " + this.m_current.windSpeed + "kph",
                                           "Pressure:" + this.m_current.pressure + "mb",
                                           "Moon    :" + this.m_astronomy.percentIlluminated + " %",
                                           "Sunrise :" + this.m_astronomy.sunriseHour + ":" + this.m_astronomy.sunriseMinute,
                                           "Sunset  :" + this.m_astronomy.sunsetHour + ":" + this.m_astronomy.sunsetMinute,
                                           "Time    :" + DateTime.Now.ToString() };

            for (int i = 0; i < vect.Length; i++)
            {
                if (vect[i] != null)
                    Debug.Print(vect[i]);
            }

            for (int i = 0; i < this.m_forecast.Count; i++)
                ShowForecastData((WeatherUndergroundForecast)m_forecast[i]);

            Debug.Print("=-=-=-=");
        }

        public void Show()
        {
            if (this.m_current.city == null)
                return;

            showWeather();
            GC.WaitForPendingFinalizers();
            Debug.GC(true);
        }


        private void SerailizeForecastData(SerialDisplay sd, int day, WeatherUndergroundForecast wuf)
        {
            string ds = day.ToString();
            string[] vect = new string[] {
                "[F" + "D" + ds + wuf.dayOfWeek + "]",
                "[F" + "L" + ds + wuf.lowTemperature + "]",
                "[F" + "H" + ds + wuf.highTemperature + "]",
                "[F" + "I" + ds + wuf.iconBasename + "]",
                "[F" + "C" + ds + wuf.condition + "]",
                "[F" + "Y" + ds + wuf.humidity + "]", // H is taken for High
                "[F" + "W" + ds + wuf.windDir + " " + wuf.windSpeed + "kph]"
            };

            for (int i = 0; i < vect.Length; i++)
                sd.SerializeString(vect[i]);
        }

        public void Serialize(SerialDisplay sd)
        {
            if (this.m_current.city == null)
                return;
            string[] vect = new string[] {
                "[IC" + this.m_current.city + "]",
                "[CC" + this.m_current.condition + "]",
                "[CT" + this.m_current.temperature + "]",
                "[CH" + this.m_current.humidity + "]",
                "[CI" + this.m_current.iconBasename + "]",
                "[CW" + this.m_current.windDir + " " + this.m_current.windSpeed + "kph]",
                "[CR" + this.m_astronomy.sunriseHour + ":" + this.m_astronomy.sunriseMinute + "]",
                "[CS" + this.m_astronomy.sunsetHour + ":" + this.m_astronomy.sunsetMinute + "]",
                "[CM" + this.m_astronomy.percentIlluminated.ToString() + "]",
                "[CP" + this.m_current.pressure + "]"
            };
            int i;

            for (i = 0; i < vect.Length; i++)
                sd.SerializeString(vect[i]);
            for (i = 0; i < this.m_forecast.Count; i++)
                SerailizeForecastData(sd, i, (WeatherUndergroundForecast)this.m_forecast[i]);
        }
    }
}
