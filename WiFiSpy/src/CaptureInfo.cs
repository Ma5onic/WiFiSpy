using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using WiFiSpy.src.Packets;

namespace WiFiSpy.src
{
    public class CaptureInfo
    {
        public bool LiveCaptureMode { get; set; }
        private List<BeaconFrame> _beacons;
        private SortedList<long, AccessPoint> _accessPoints;
        private SortedList<string, AccessPoint[]> _APExtenders;
        private SortedList<long, Station> _stations;
        private List<DataFrame> _dataFrames;
        private List<AuthRequestFrame> _authRequestFrames;
        private List<AnyPacketFrame> _allFrames;

        public delegate void DataFrameProgressCallback(int Value, int Max);
        public event DataFrameProgressCallback onDataFrameProgress;

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
        public AuthRequestFrame[] AuthRequestFrames
        {
            get
            {
                return _authRequestFrames.ToArray();
            }
        }
        public AnyPacketFrame[] AllFrames
        {
            get
            {
                return _allFrames.ToArray();
            }
        }


        /// <summary>
        /// Floating Data Frames that belong to no station we know of
        /// </summary>
        public DataFrame[] FloatingDataFrames
        {
            get
            {
                return _dataFrames.ToArray();
            }
        }

        public SortedList<string, AccessPoint[]> PossibleExtenders
        {
            get
            {
                if (_APExtenders != null && !LiveCaptureMode)
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

        public CaptureInfo()
        {
            _beacons = new List<BeaconFrame>();
            _accessPoints = new SortedList<long, AccessPoint>();
            _stations = new SortedList<long, Station>();
            _dataFrames = new List<DataFrame>();
            _authRequestFrames = new List<AuthRequestFrame>();
            _allFrames = new List<AnyPacketFrame>();
        }

        public void AddCapturefile(CapFile capFile)
        {
            AddCapturefiles(new CapFile[] { capFile });
        }

        public void AddCapturefiles(CapFile[] capFiles)
        {
            foreach (CapFile capFile in capFiles)
            {
                //merge beacons
                HashSet<BeaconFrame> beacons = new HashSet<BeaconFrame>(capFile.Beacons.AsEnumerable(), new BeaconFrame());
                beacons.UnionWith(this._beacons.AsEnumerable());

                _beacons.Clear();
                _beacons.AddRange(beacons.ToArray());


                //merge Access Points
                HashSet<AccessPoint> APs = new HashSet<AccessPoint>(capFile.AccessPoints.AsEnumerable(), new AccessPoint());
                APs.UnionWith(this.AccessPoints.AsEnumerable());

                _accessPoints.Clear();

                foreach(AccessPoint AP in APs.ToArray())
                {
                    long MacAddrNumber = Utils.MacToLong(AP.BeaconFrame.MacAddress);
                    _accessPoints.Add(MacAddrNumber, AP);
                }


                //merge Stations
                HashSet<Station> stations = new HashSet<Station>(capFile.Stations.AsEnumerable(), new Station());
                stations.UnionWith(Stations.AsEnumerable());

                _stations.Clear();
                foreach (Station station in stations.ToArray())
                {
                    long MacAddrNumber = Utils.MacToLong(station.SourceMacAddress);
                    _stations.Add(MacAddrNumber, station);
                }

                //merge Data Frames
                HashSet<DataFrame> dataFrames = new HashSet<DataFrame>(_dataFrames, new DataFrame());
                dataFrames.UnionWith(capFile.DataFrames);

                _dataFrames.Clear();
                _dataFrames.AddRange(dataFrames.ToArray());

                //merge Auth Request Frames
                HashSet<AuthRequestFrame> authFrames = new HashSet<AuthRequestFrame>(_authRequestFrames, new AuthRequestFrame());
                authFrames.UnionWith(capFile.AuthRequestFrames.AsEnumerable());

                _authRequestFrames.Clear();
                _authRequestFrames.AddRange(authFrames.ToArray());

                if (LiveCaptureMode)
                {
                    //merge All Frames - live only
                    HashSet<AnyPacketFrame> allFrames = new HashSet<AnyPacketFrame>(_allFrames, new AnyPacketFrame());
                    allFrames.UnionWith(capFile.AllFrames.AsEnumerable());

                    _allFrames.Clear();
                    _allFrames.AddRange(allFrames.ToArray());
                }
            }

            foreach (Station station in _stations.Values)
            {
                station.CaptureInfo = this;
            }


            //link all the DataFrames to the Stations
            int DataFrameCount = _dataFrames.Count;
            int DataFrameProgressValue = 0;

            SortedList<long, Station> StationList = new SortedList<long, Station>();

            foreach (Station station in _stations.Values)
            {
                long MacSourceAddrNumber = Utils.MacToLong(station.SourceMacAddress);
                station.ClearDataFrames();

                if (!StationList.ContainsKey(MacSourceAddrNumber))
                    StationList.Add(MacSourceAddrNumber, station);
            }

            /**/Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < _dataFrames.Count; i++)
            {
                Station station = null;
                if (StationList.TryGetValue(_dataFrames[i].SourceMacAddressLong, out station) ||
                    StationList.TryGetValue(_dataFrames[i].TargetMacAddressLong, out station))
                {
                    station.AddDataFrame(_dataFrames[i]);
                    _dataFrames.RemoveAt(i);
                    i--;
                    DataFrameProgressValue++;

                    if (onDataFrameProgress != null && sw.ElapsedMilliseconds >= 1000)
                    {
                        onDataFrameProgress(DataFrameProgressValue, DataFrameCount);
                        sw = Stopwatch.StartNew();
                    }
                }
            }
        }

        public Station[] GetStationsFromAP(AccessPoint AP)
        {
            return Stations.Where(o => o.DataFrames.FirstOrDefault(frame => frame.TargetMacAddressLong == AP.MacAddressLong) != null).ToArray();
        }

        /// <summary>
        /// De-Cloak an hidden Access Point
        /// </summary>
        /// <param name="AP">The AP to De-Cloak</param>
        /// <returns>De-Cloak successful ?</returns>
        public bool DeCloakAP(AccessPoint AP, ref string SSID)
        {
            if (!AP.BeaconFrame.IsHidden)
                return false;

            foreach (AuthRequestFrame frame in _authRequestFrames.Where(o => o.SourceMacAddressLong == AP.MacAddressLong ||
                                                                             o.TargetMacAddressLong == AP.MacAddressLong))
            {
                if(String.IsNullOrWhiteSpace(frame.SSID))
                    continue;
                SSID = frame.SSID;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Is the Access Point dangerous ? Running Karma/Mana modules ?
        /// </summary>
        /// <param name="AP"></param>
        /// <returns></returns>
        public bool IsAccessPointDanger(AccessPoint AP)
        {
            List<string> names = new List<string>();

            foreach (AuthRequestFrame frame in _authRequestFrames.Where(o => (o.SourceMacAddressLong == AP.MacAddressLong ||
                                                                             o.TargetMacAddressLong == AP.MacAddressLong) &&
                                                                             !String.IsNullOrWhiteSpace(o.SSID)))
            {
                if (!names.Contains(frame.SSID))
                    names.Add(frame.SSID);

                if (names.Count > 5)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clear all the logged traffic
        /// </summary>
        public void Clear()
        {
            _beacons.Clear();
            _accessPoints.Clear();
            _stations.Clear();
            _dataFrames.Clear();
        }
    }
}