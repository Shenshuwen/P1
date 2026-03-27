using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P1.Common
{
    public class Decode
    {
        private bool isFirstParse = true;
        public float ParseAndCalculateWind(byte[] responseBuffer)
        {
            if (isFirstParse)
            {
                isFirstParse = false;
                return 0;
            }
            if (responseBuffer.Length < 5)
                return 0;

            byte highByte = responseBuffer[3];
            byte lowByte = responseBuffer[4];
            ushort combinedValue = (ushort)((highByte << 8) | lowByte);

            return combinedValue / 100f;
        }
        public float ParseAndCalculateStress(byte[] responseBuffer)
        {
            if (responseBuffer.Length < 5)
                return 0;

            byte highByte = responseBuffer[3];
            byte lowByte = responseBuffer[4];
            ushort combinedValue = (ushort)((highByte << 8) | lowByte);

            return combinedValue;
        }
        // 解析温湿度数据，并返回温度和湿度
        public Tuple<double, double> ParseTemperatureHumidity(byte[] responseData)
        {

            string humidityStr = Encoding.ASCII.GetString(responseData, 14, 5);
            //Console.WriteLine(humidityStr);
            double humidity = double.Parse(humidityStr, CultureInfo.InvariantCulture);


            string temperatureStr = Encoding.ASCII.GetString(responseData, 31, 5);

            //Console.WriteLine(temperatureStr);
            double temperature = double.Parse(temperatureStr, CultureInfo.InvariantCulture);

            return new Tuple<double, double>(temperature, humidity);
        }

    }
}
