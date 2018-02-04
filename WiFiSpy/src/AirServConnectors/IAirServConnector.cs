using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiFiSpy.src.AirServConnectors
{
    public interface IAirServConnector
    {
        bool Connect();
        void Disconnect();
        void Send(byte[] Data, int Offset, int Length);
        byte[] Receive(int Count);
    }
}