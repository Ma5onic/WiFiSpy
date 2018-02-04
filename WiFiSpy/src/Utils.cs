using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiFiSpy.src
{
    public class Utils
    {
        public static DateTime GetRealArrivalTime(DateTime ArriveTime)
        {
            DateTimeOffset DtOffset = new DateTimeOffset(ArriveTime, TimeSpan.Zero);
            TimeSpan UtcOffset = TimeZoneInfo.Local.GetUtcOffset(DtOffset);

            return ArriveTime.Add(UtcOffset);
        }

        public static long MacToLong(byte[] MacAddress)
        {
            if (MacAddress.Length < 6)
                return 0;

            byte[] MacAddrTemp = new byte[8];
            Array.Copy(MacAddress, MacAddrTemp, 6);
            return BitConverter.ToInt64(MacAddrTemp, 0);
        }
    }
}
