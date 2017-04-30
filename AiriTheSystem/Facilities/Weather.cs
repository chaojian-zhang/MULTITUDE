using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherNet;
using WeatherNet.Clients;
using WeatherNet.Model;

namespace Airi.Facilities
{
    /// <summary>
    /// We used a cleaned up version of weathernet for tidyness
    /// </summary>
    /// <Development> Unpublic this class
    public class Weather
    {
        public static string GetWeather()
        {
            ClientSettings.SetApiKey("bd5e378503939ddaee76f12ad7a97608");   // 865190f17e5a219e99d7ce1336df43ba -- Our own
            var result = CurrentWeather.GetByCityName("Toronto", "Canada", "en", "metric");
            var result2 = FiveDaysForecast.GetByCityName("Toronto", "Canada", "en", "metric");

            // Formation
            if (result.Success)
            {
                // Five day average
                double average = Math.Round(result2.Items.Average(item => item.Temp), 2, MidpointRounding.AwayFromZero);    
                // Check rain in tomorrow
                FiveDaysForecastResult temp = result2.Items.Take(7).First(item => item.Description.Contains("rain"));       
                string rainForecast = (temp != null) ? "Tomorrow will have rain, remember to bring an umbrella." : "";
                // Generate output string
                string resultString = string.Format("Current weather in {0}, is {1}; Windspeed {2} meter per second, Temperature {3} degrees.\n Average temperature in next fives days are: {4} degrees. {5}",
                    result.Item.City, result.Item.Description, result.Item.WindSpeed, result.Item.Temp, average, rainForecast);
                return resultString;
            }

            return null;
        }
    }
}
