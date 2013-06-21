using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;


namespace WeatherUndergroundToSerialDisplay
{
    public static class LedNotifier
    {
        private static OutputPort ledNTP;
        private static OutputPort ledAskGeo;
        private static OutputPort ledGeoBytes;
        private static OutputPort ledWeatherUnderground;

        public enum NotificationMessages
        {
            NTPStarted,
            NTPEndedSuccess,
            NTPEndedError,

            AskGeoStarted,
            AskGeoEndedSuccess,
            AskGeoEndedError,

            GeoBytesStarted,
            GeoBytesEndedSuccess,
            GeoBytesEndedError,

            WeatherUndergroundStarted,
            WeatherUndergroundEndedSuccess,
            WeatherUndergroundEndedError
        }

    }
}
