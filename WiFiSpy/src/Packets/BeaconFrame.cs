using PacketDotNet;
using PacketDotNet.Ieee80211;
using PacketDotNet.Tcp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WiFiSpy.src.Packets
{
    public class BeaconFrame : IEqualityComparer<BeaconFrame>
    {
        public string Manufacturer { get; private set; }
        public string SSID { get; private set; }
        public bool IsHidden { get; private set; }
        public byte[] MacAddress { get; private set; }
        public int Channel { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public int Wifi_Channel { get; private set; }
        public int FrameSize { get; private set; }

        public string MacAddressStr
        {
            get
            {
                if (MacAddress != null)
                    return BitConverter.ToString(MacAddress);
                return "";
            }
        }

        public bool WPS_Enabled { get; private set; }

        public BeaconFrame()
        {

        }

        public BeaconFrame(PacketDotNet.Ieee80211.BeaconFrame frame, DateTime TimeStamp)
            : this(frame, TimeStamp, 0)
        {

        }

        public BeaconFrame(PacketDotNet.Ieee80211.BeaconFrame frame, DateTime TimeStamp, int Channel)
        {
            this.Manufacturer = OuiParser.GetOuiByMac(frame.SourceAddress.GetAddressBytes());
            this.MacAddress = frame.SourceAddress.GetAddressBytes();
            this.TimeStamp = TimeStamp;
            this.Wifi_Channel = Channel;
            this.FrameSize = frame.FrameSize;

            foreach (InformationElement element in frame.InformationElements)
            {
                switch (element.Id)
                {
                    case InformationElement.ElementId.ServiceSetIdentity:
                    {
                        SSID = ASCIIEncoding.ASCII.GetString(element.Value);
                        IsHidden = String.IsNullOrWhiteSpace(SSID);
                        break;
                    }
                    case InformationElement.ElementId.DsParameterSet:
                    {
                        if (element.Value != null && element.Value.Length >= 3)
                        {
                            Channel = element.Value[2];
                        }
                        break;
                    }
                    case InformationElement.ElementId.VendorSpecific:
                    {
                        //correct me if I was wrong here...
                        if (element.Bytes.Length > 5)
                        {
                            if (element.Bytes[5] == 4)
                            {
                                WPS_Enabled = true;
                            }
                        }
                        break;
                    }
                }
            }

            if (String.IsNullOrEmpty(SSID))
                SSID = "";
        }

        public bool Equals(BeaconFrame x, BeaconFrame y)
        {
            return x.MacAddressStr == y.MacAddressStr;
        }

        public int GetHashCode(BeaconFrame obj)
        {
            return (int)Utils.MacToLong(obj.MacAddress);
        }
    }
}