using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiFiSpy.src.Packets
{
    public class AnyPacketFrame : IEqualityComparer<AnyPacketFrame>
    {
        public DateTime TimeStamp { get; private set; }
        public Packet Packet { get; private set; }
        public int Wifi_Channel { get; private set; }

        public AnyPacketFrame(DateTime TimeStamp, Packet Packet)
            : this(TimeStamp, Packet, 0)
        {

        }

        internal AnyPacketFrame()
        {

        }

        public AnyPacketFrame(DateTime TimeStamp, Packet Packet, int Wifi_Channel)
        {
            this.TimeStamp = TimeStamp;
            this.Packet = Packet;
            this.Wifi_Channel = Wifi_Channel;
        }

        public bool Equals(AnyPacketFrame x, AnyPacketFrame y)
        {
            return x.TimeStamp == y.TimeStamp;
        }

        public int GetHashCode(AnyPacketFrame obj)
        {
            return (int)TimeStamp.Ticks;
        }
    }
}