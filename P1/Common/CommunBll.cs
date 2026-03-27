using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P1.Common
{
    public class CommunBll
    {
        // 将字符串转换为字节数组并计算 CRC16 校验
        public static byte[] ModBusStringToCRC16(string data)
        {
            byte[] byteHex = HexStringToBytes(data);
            byte[] byteCRC = CRC16(byteHex);
            byte[] bytesOut = new byte[byteHex.Length + byteCRC.Length];

            // 高低位调换
            byte midbyte = byteCRC[1];
            byteCRC[1] = byteCRC[0];
            byteCRC[0] = midbyte;

            // 将两个数组重新组合
            byteHex.CopyTo(bytesOut, 0);
            byteCRC.CopyTo(bytesOut, byteHex.Length);
            return bytesOut;
        }

        // 计算 CRC16 校验和
        public static byte[] CRC16(byte[] data)
        {
            int len = data.Length;
            if (len > 0)
            {
                ushort crc = 0xFFFF;

                for (int i = 0; i < len; i++)
                {
                    crc = (ushort)(crc ^ (data[i]));
                    for (int j = 0; j < 8; j++)
                    {
                        crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
                    }
                }
                byte hi = (byte)((crc & 0xFF00) >> 8); //高位置
                byte lo = (byte)(crc & 0x00FF); //低位置

                return new byte[] { hi, lo };
            }
            return new byte[] { 0, 0 };
        }
        // 将十六进制字符串转换为字节数组
        public static byte[] HexStringToBytes(string hs)
        {
            string[] strArr = hs.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            byte[] b = new byte[strArr.Length];
            for (int i = 0; i < strArr.Length; i++)
            {
                string value = strArr[i];
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    value = value[2..];
                }

                b[i] = Convert.ToByte(value, 16);
            }
            return b;
        }
    }
}
