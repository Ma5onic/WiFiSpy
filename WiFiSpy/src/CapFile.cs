using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WiFiSpy.src.Packets;

namespace WiFiSpy.src
{
    public class CapFile
    {
        public delegate void ReadBeaconCallback(BeaconFrame beacon);
        public event ReadBeaconCallback onReadBeacon;

        public delegate void ReadAccessPointCallback(AccessPoint AP);
        public event ReadAccessPointCallback onReadAccessPoint;

        public delegate void ReadStationCallback(Station station);
        public event ReadStationCallback onReadStation;

        public delegate void ReadDataFrameCallback(DataFrame dataFrame);
        public event ReadDataFrameCallback onReadDataFrame;

        private List<BeaconFrame> _beacons;
        private SortedList<long, AccessPoint> _accessPoints;
        private SortedList<string, AccessPoint[]> _APExtenders;
        private SortedList<long, Station> _stations;
        private List<DataFrame> _dataFrames;
        private List<AuthRequestFrame> _authRequestFrames;
        private List<AnyPacketFrame> _allFrames;

        public BeaconFrame[] Beacons
        {
            get
            {
                return _beacons.ToArray();
            }
        }

        public AccessPoint[] AccessPoints
        {
            get
            {
                return _accessPoints.Values.ToArray();
            }
        }
        public Station[] Stations
        {
            get
            {
                return _stations.Values.ToArray();
            }
        }
        public DataFrame[] DataFrames
        {
            get
            {
                return _dataFrames.ToArray();
            }
        }
        public AuthRequestFrame[] AuthRequestFrames
        {
            get
            {
                return _authRequestFrames.ToArray();
            }
        }

        public SortedList<string, AccessPoint[]> PossibleExtenders
        {
            get
            {
                if (_APExtenders != null)
                    return _APExtenders;

                SortedList<string, List<AccessPoint>> extenders = new SortedList<string, List<AccessPoint>>();

                foreach (AccessPoint AP in _accessPoints.Values)
                {
                    if (!AP.BeaconFrame.IsHidden)
                    {
                        if (!extenders.ContainsKey(AP.SSID))
                            extenders.Add(AP.SSID, new List<AccessPoint>());

                        extenders[AP.SSID].Add(AP);
                    }
                }

                //only copy now the ones that are having more then 1 AP (Extender)
                SortedList<string, AccessPoint[]> temp = new SortedList<string, AccessPoint[]>();

                for (int i = 0; i < extenders.Count; i++)
                {
                    if (extenders.Values[i].Count > 1)
                    {
                        temp.Add(extenders.Keys[i], extenders.Values[i].ToArray());
                    }
                }

                this._APExtenders = temp;
                return _APExtenders;
            }
        }

        public AnyPacketFrame[] AllFrames
        {
            get
            {
                return _allFrames.ToArray();
            }
        }

        public CapFile()
        {
            _beacons = new List<BeaconFrame>();
            _accessPoints = new SortedList<long, AccessPoint>();
            _stations = new SortedList<long, Station>();
            _dataFrames = new List<DataFrame>();
            _authRequestFrames = new List<AuthRequestFrame>();
            _allFrames = new List<AnyPacketFrame>();
        }

        public void ReadCap(string FilePath)
        {
            ICaptureDevice device = null;

            try
            {
                // Get an offline device
                device = new CaptureFileReaderDevice(FilePath);

                // Open the device
                device.Open();
            }
            catch (Exception e)
            {
                return;
            }
            
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrival);
            device.Capture();
            device.Close();

            //CapFileReader reader = new CapFileReader();
            //reader.ReadCapFile(FilePath);
        }

        /// <summary>
        /// Clear the logged traffic
        /// </summary>
        public void Clear()
        {
            _beacons.Clear();
            _accessPoints.Clear();

            _stations.Clear();
            _dataFrames.Clear();

            if (_APExtenders != null)
                _APExtenders.Clear();
        }

        int packetsProcessed = 0;
        private void device_OnPacketArrival(object sender, CaptureEventArgs e)
        {
            packetsProcessed++;

            if (e.Packet.LinkLayerType == PacketDotNet.LinkLayers.Ieee80211 || e.Packet.LinkLayerType == PacketDotNet.LinkLayers.Ieee80211_Radio)
            {
                Packet packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);

                if (packet != null)
                    ProcessPacket(packet, Utils.GetRealArrivalTime(e.Packet.Timeval.Date));
            }
            else if (e.Packet.LinkLayerType == PacketDotNet.LinkLayers.PerPacketInformation)
            {
                try
                {
                    Packet packet = PacketDotNet.Packet.ParsePacket(e.Packet.LinkLayerType, e.Packet.Data);
                
                if (packet != null)
                {
                    /*if (packetsProcessed > 30)
                    {
                        //check GPS type
                        if (packet.Bytes[8] == 0x32 && packet.Bytes[9] == 0x75 && packet.Bytes[10] == 0x18 && packet.Bytes[11] == 0x00)
                        {
                            byte[] TempBytes = new byte[packet.Bytes.Length];
                            Array.Copy(packet.Bytes, TempBytes, TempBytes.Length);

                            int HeaderRevision = TempBytes[12];
                            int HeaderPad = TempBytes[13];
                            int HeaderLength = BitConverter.ToInt16(TempBytes, 14);
                            int Present = BitConverter.ToInt32(TempBytes, 16);

                            Array.Reverse(TempBytes, 20, 4);
                            float Latitude = BitConverter.ToSingle(TempBytes, 20);

                            Array.Reverse(TempBytes, 24, 4);
                            float Longitude = BitConverter.ToSingle(TempBytes, 24);

                        }
                    }*/

                    ProcessPacket(packet.PayloadPacket, Utils.GetRealArrivalTime(e.Packet.Timeval.Date));
                }
                }
                catch { }

            }
        }

        public void ProcessPacket(Packet packet, DateTime ArrivalDate)
        {
            ProcessPacket(packet, ArrivalDate, 0);
        }

        public void ProcessPacket(Packet packet, DateTime ArrivalDate, int Channel)
        {
            PacketDotNet.Ieee80211.BeaconFrame beacon = packet as PacketDotNet.Ieee80211.BeaconFrame;
            PacketDotNet.Ieee80211.ProbeRequestFrame probeRequest = packet as PacketDotNet.Ieee80211.ProbeRequestFrame;
            PacketDotNet.Ieee80211.QosDataFrame DataFrame = packet as PacketDotNet.Ieee80211.QosDataFrame;

            PacketDotNet.Ieee80211.DeauthenticationFrame DeAuthFrame = packet as PacketDotNet.Ieee80211.DeauthenticationFrame;
            PacketDotNet.Ieee80211.AssociationRequestFrame AuthRequestFrame = packet as PacketDotNet.Ieee80211.AssociationRequestFrame;

            PacketDotNet.Ieee80211.DataDataFrame DataDataFrame = packet as PacketDotNet.Ieee80211.DataDataFrame;
            PacketDotNet.Ieee80211.RadioPacket radioFrame = packet as PacketDotNet.Ieee80211.RadioPacket;

            if (packet != null && false)//LiveCaptureMode)
            {
                _allFrames.Add(new AnyPacketFrame(ArrivalDate, packet, Channel));
            }

            if (radioFrame != null)
            {
                PacketDotNet.Ieee80211.FrameControlField.FrameSubTypes type = ((PacketDotNet.Ieee80211.MacFrame)radioFrame.PayloadPacket).FrameControl.SubType;
                if (type == PacketDotNet.Ieee80211.FrameControlField.FrameSubTypes.ManagementBeacon)
                {
                    beacon = new PacketDotNet.Ieee80211.BeaconFrame(new PacketDotNet.Utils.ByteArraySegment(packet.Bytes));
                }
                else if (type == PacketDotNet.Ieee80211.FrameControlField.FrameSubTypes.Data)
                {
                    DataFrame = new PacketDotNet.Ieee80211.QosDataFrame(new PacketDotNet.Utils.ByteArraySegment(packet.Bytes));
                }
            }
            if (beacon != null)
            {
                BeaconFrame beaconFrame = new BeaconFrame(beacon, ArrivalDate, Channel);
                _beacons.Add(beaconFrame);

                long MacAddrNumber = Utils.MacToLong(beaconFrame.MacAddress);

                //check for APs with this Mac Address
                AccessPoint AP = null;

                if (!_accessPoints.TryGetValue(MacAddrNumber, out AP))
                {
                    AP = new AccessPoint(beaconFrame);
                    _accessPoints.Add(MacAddrNumber, AP);
                }
                AP.AddBeaconFrame(beaconFrame);

                if (onReadAccessPoint != null)
                    onReadAccessPoint(AP);
            }
            else if (probeRequest != null)
            {
                ProbePacket probe = new ProbePacket(probeRequest, ArrivalDate);
                Station station = null;

                long MacAddrNumber = Utils.MacToLong(probe.SourceMacAddress);

                if (!_stations.TryGetValue(MacAddrNumber, out station))
                {
                    station = new Station(probe);
                    _stations.Add(MacAddrNumber, station);
                }

                station.AddProbe(probe);

                if (onReadStation != null)
                    onReadStation(station);
            }
            /**/else if (DataFrame != null)
            {
                DataFrame _dataFrame = new Packets.DataFrame(DataFrame, ArrivalDate, Channel);

                _dataFrames.Add(_dataFrame);

                if (onReadDataFrame != null)
                    onReadDataFrame(_dataFrame);
            }
            else if (DataDataFrame != null)
            {
                DataFrame _dataFrame = new Packets.DataFrame(DataDataFrame, ArrivalDate, Channel);

                _dataFrames.Add(_dataFrame);

                if (onReadDataFrame != null)
                    onReadDataFrame(_dataFrame);
            }
            else if (AuthRequestFrame != null)
            {
                _authRequestFrames.Add(new AuthRequestFrame(AuthRequestFrame, ArrivalDate));
            }
            else if (radioFrame != null)
            {

            }
        }
    }
}