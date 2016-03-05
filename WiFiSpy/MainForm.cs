using PacketDotNet.Ieee80211;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using WiFiSpy.src;
using WiFiSpy.src.GpsParsers;
using WiFiSpy.src.Packets;

namespace WiFiSpy
{
    public partial class MainForm : Form
    {
        private delegate void Invoky();
        //public static List<CapFile> CapFiles { get; private set; }
        public static List<GpsLocation> GpsLocations { get; private set; }
        public const string DateTimeFormat = "dd-MM-yyyy HH:mm:ss";

        public AirservClient AirservClient { get; private set; }
        public CapFile LiveCaptureFile { get; private set; }

        public CaptureInfo captureInfo { get; private set; }

        private AccessPoint[] APList_AccessPoints;

        public MainForm()
        {
            InitializeComponent();
            OuiParser.Initialize("./Data/OUI.txt");

            //CapFiles = new List<CapFile>();
            GpsLocations = new List<GpsLocation>();

            RefreshAll();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        private void importCapFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Title = "Select the capture file(s)";

                if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    bool RefreshFiles = false;
                    foreach(string filePath in dialog.FileNames)
                    {
                        if(File.Exists(filePath))
                        {
                            try
                            {
                                FileInfo inf = new FileInfo(filePath);
                                File.Copy(filePath, Environment.CurrentDirectory + "\\Data\\Captures\\" + inf.Name, true);
                                RefreshFiles = true;
                            }
                            catch(Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                    }

                    if(RefreshFiles)
                    {
                        ReadCapFiles();
                    }
                }
            }
        }

        private void ReadCapFiles()
        {
            LoadingForm LoadForm = new LoadingForm();
            LoadForm.Show();

            //CapFiles.Clear();
            List<CapFile> CapFiles = new List<CapFile>();
            foreach (string capFilePath in Directory.GetFiles(Environment.CurrentDirectory + "\\Data\\Captures", "*.cap"))
            {
                CapFile capFile = new CapFile();

                LoadForm.SetCapFile(capFile);
                LoadForm.SetFileName(new FileInfo(capFilePath).Name);

                capFile.ReadCap(capFilePath);

                CapFiles.Add(capFile);
            }

            this.captureInfo = new CaptureInfo();

            LoadForm.SetCaptureInfo(this.captureInfo);

            captureInfo.AddCapturefiles(CapFiles.ToArray());
            CapFiles.Clear();

            LoadForm.Close();
        }

        private void RefreshGpsLocations()
        {
            GpsLocations.Clear();

            foreach (string FilePath in Directory.GetFiles(Environment.CurrentDirectory + "\\Data\\GPS", "*.csv"))
            {
                GpsLocations.AddRange(GpsCsvParser.GetLocations(FilePath));
            }

            foreach (string FilePath in Directory.GetFiles(Environment.CurrentDirectory + "\\Data\\GPS", "*.gpx"))
            {
                GpsLocations.AddRange(GpxParser.GetLocations(FilePath));
            }
        }

        private void RefreshAll()
        {
            if (this.AirservClient == null)
            {
                ReadCapFiles();
            }

            RefreshGpsLocations();

            FillHourlyChart();
            FillPieChart();
            FillStationWeekOverview();

            FillTrafficChart();

            stationListControl1.SetStations(captureInfo.Stations);
            FillExtenderList();

            FillApTree();
        }

        private void FillApTree()
        {
            APList_AccessPoints = captureInfo.AccessPoints.OrderBy(o => o.SSID).ToArray();

            APList.VirtualListSize = APList_AccessPoints.Length;
            APList.Refresh();
        }

        private void FillExtenderList()
        {
            LvRepeaterNames.Items.Clear();
            LvRepeaterList.Items.Clear();

            SortedList<string, AccessPoint[]> repeaters = captureInfo.PossibleExtenders;

            for (int i = 0; i < repeaters.Count; i++)
            {
                ListViewItem item = new ListViewItem(new string[]
                {
                    repeaters.Keys[i],
                    repeaters.Values[i].Length.ToString()
                });
                item.Tag = repeaters.Values[i];
                LvRepeaterNames.Items.Add(item);
            }
        }

        private void FillHourlyChart()
        {
            AllHourlyChart.Titles.Clear();
            AllHourlyChart.Series.Clear();


            AllHourlyChart.Titles.Add("Hourly Overview");

            Series APSerie = AllHourlyChart.Series.Add("Access Points");
            APSerie.LegendText = "Access Points";

            Series HiddenAPSerie = AllHourlyChart.Series.Add("Hidden Access Points");
            HiddenAPSerie.LegendText = "Hidden Access Points";

            Series StationsSerie = AllHourlyChart.Series.Add("Stations");
            StationsSerie.LegendText = "Stations";

            int TotalAPCount = 0;
            int TotalHiddenAPCount = 0;
            int TotalStationCount = 0;

            for (int i = 1; i < 24; i++)
            {
                int count = 0;
                int hiddenCount = 0;
                int stationCount = 0;

                count += captureInfo.AccessPoints.Where(o => o.TimeStamp.Hour == i).Count();
                hiddenCount += captureInfo.AccessPoints.Where(o => o.BeaconFrame.IsHidden && o.TimeStamp.Hour == i).Count();
                stationCount += captureInfo.Stations.Where(o => o.TimeStamp.Hour == i).Count();

                APSerie.Points.AddXY(i, count);
                HiddenAPSerie.Points.AddXY(i, hiddenCount);
                StationsSerie.Points.AddXY(i, stationCount);

                TotalAPCount += count;
                TotalHiddenAPCount += hiddenCount;
                TotalStationCount += stationCount;
            }

            APSerie.LegendText += " (" + TotalAPCount + ")";
            HiddenAPSerie.LegendText += " (" + TotalHiddenAPCount + ")";
            StationsSerie.LegendText += " (" + TotalStationCount + ")";
        }

        private void FillPieChart()
        {
            APInfoPieChart.Titles.Clear();
            APInfoPieChart.Series.Clear();

            int APCount_Pie = 0;
            int hiddenCount_Pie = 0;
            int WpsEnabledCount_Pie = 0;
            
            APCount_Pie += captureInfo.AccessPoints.Where(o => !o.BeaconFrame.IsHidden).Count();
            hiddenCount_Pie += captureInfo.AccessPoints.Where(o => o.BeaconFrame.IsHidden).Count();
            WpsEnabledCount_Pie += captureInfo.AccessPoints.Where(o => o.WPS_Enabled).Count();

            Series APSerie_Pie = new Series
            {
                Name = "Access Points",
                IsVisibleInLegend = true,
                ChartType = SeriesChartType.Pie
            };

            DataPoint DataPointPie = APSerie_Pie.Points.Add(APCount_Pie);
            DataPointPie.LegendText = "Access Points (" + APCount_Pie + ")";

            DataPoint hiddenDataPointPie = APSerie_Pie.Points.Add(hiddenCount_Pie);
            hiddenDataPointPie.LegendText = "Hidden Access Points (" + hiddenCount_Pie + ")";

            DataPoint WpsEnabledDataPointPie = APSerie_Pie.Points.Add(WpsEnabledCount_Pie);
            WpsEnabledDataPointPie.LegendText = "WPS Enabled (" + WpsEnabledCount_Pie + ")";



            APInfoPieChart.Series.Add(APSerie_Pie);
        }

        private void FillStationWeekOverview()
        {
            WeekStationOverviewChart.Titles.Clear();
            WeekStationOverviewChart.Series.Clear();

            DateTime WeekStartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            WeekStartDate = WeekStartDate.AddDays(DayOfWeek.Monday - WeekStartDate.DayOfWeek);

            SortedList<string, int> DeviceList = new SortedList<string, int>();
            SortedList<string, int> DeviceListCount = new SortedList<string, int>();

            Series UniqueStationsSerie = WeekStationOverviewChart.Series.Add("Unique Stations");
            UniqueStationsSerie.LegendText = "Unique Stations";

            foreach (Station station in captureInfo.Stations)
            {
                if (station.DeviceTypeStr.Length > 0)
                {
                    if (!DeviceList.ContainsKey(station.DeviceTypeStr))
                    {
                        Series TempSerie = WeekStationOverviewChart.Series.Add(station.DeviceTypeStr);
                        TempSerie.LegendText = station.DeviceTypeStr;

                        DeviceList.Add(station.DeviceTypeStr, 0);
                        DeviceListCount.Add(station.DeviceTypeStr, 0);
                    }
                }
            }

            int TotalStationCount = 0;
            SortedList<DayOfWeek, List<string>> StationMacs = new SortedList<DayOfWeek, List<string>>();
            StationMacs.Add(DayOfWeek.Monday, new List<string>());
            StationMacs.Add(DayOfWeek.Tuesday, new List<string>());
            StationMacs.Add(DayOfWeek.Wednesday, new List<string>());
            StationMacs.Add(DayOfWeek.Thursday, new List<string>());
            StationMacs.Add(DayOfWeek.Friday, new List<string>());
            StationMacs.Add(DayOfWeek.Saturday, new List<string>());
            StationMacs.Add(DayOfWeek.Sunday, new List<string>());

            for(int i = 0; i < StationMacs.Count; i++)
            {
                foreach(Station station in captureInfo.Stations.Where(o => WeekStartDate.Year == o.TimeStamp.Year &&
                                                                        WeekStartDate.Month == o.TimeStamp.Month &&
                                                                        WeekStartDate.Day == o.TimeStamp.Day))
                {
                    if(!String.IsNullOrEmpty(station.SourceMacAddressStr))
                    {
                        if(!StationMacs.Values[i].Contains(station.SourceMacAddressStr))
                        {
                            if(DeviceList.ContainsKey(station.DeviceTypeStr))
                            {
                                DeviceList[station.DeviceTypeStr]++;
                                DeviceListCount[station.DeviceTypeStr]++;
                            }

                            StationMacs.Values[i].Add(station.SourceMacAddressStr);
                            TotalStationCount++;
                        }
                    }
                }

                UniqueStationsSerie.Points.AddXY(StationMacs.Keys[i].ToString(), StationMacs.Values[i].Count);

                for (int j = 0; j < DeviceList.Count; j++)
                {
                    WeekStationOverviewChart.Series[DeviceList.Keys[j]].Points.AddXY(DeviceList.Keys[j], DeviceList.Values[j]);
                    DeviceList[DeviceList.Keys[j]] = 0;
                }

                WeekStartDate = WeekStartDate.AddDays(1);
            }

            UniqueStationsSerie.LegendText += " (" + TotalStationCount + ")";
            StationWeekOverviewBox.Text = "Stations week overview (" + WeekStartDate.ToShortDateString() + " - " + WeekStartDate.AddDays(7).ToShortDateString() + ")";

            for (int i = 0; i < DeviceListCount.Count; i++)
            {
                WeekStationOverviewChart.Series[DeviceListCount.Keys[i]].LegendText += " (" + DeviceListCount.Values[i] + ")";
            }
        }

        private void FillTrafficChart()
        {
            TrafficPieChart.Titles.Clear();
            TrafficPieChart.Series.Clear();

            int FTP = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 20 || o.PortDest == 20)).Count();
            int SFTP = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 22 || o.PortDest == 22)).Count();
            int HTTP = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 80 || o.PortDest == 80)).Count();
            int HTTPS = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 443 || o.PortDest == 443)).Count();
            int SMTP = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 25 || o.PortDest == 25)).Count();
            int DNS = captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 53 || o.PortDest == 53)).Count();

            int VNC = 0;
            VNC += captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5500 || o.PortDest == 5500)).Count();
            VNC += captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5800 || o.PortDest == 5800)).Count();
            VNC += captureInfo.FloatingDataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5900 || o.PortDest == 5900)).Count();

            foreach (Station station in captureInfo.Stations)
            {
                FTP += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 20 || o.PortDest == 20)).Count();
                SFTP += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 22 || o.PortDest == 22)).Count();
                HTTP += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 80 || o.PortDest == 80)).Count();
                HTTPS += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 443 || o.PortDest == 443)).Count();
                SMTP += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 25 || o.PortDest == 25)).Count();
                DNS += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 53 || o.PortDest == 53)).Count();
                

                VNC += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5500 || o.PortDest == 5500)).Count();
                VNC += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5800 || o.PortDest == 5800)).Count();
                VNC += station.DataFrames.Where(o => o.isIPv4 && o.isTCP && (o.PortSource == 5900 || o.PortDest == 5900)).Count();
            }

            Series APSerie_Pie = new Series
            {
                Name = "Traffic",
                IsVisibleInLegend = true,
                ChartType = SeriesChartType.Pie
            };

            DataPoint FTPPie = APSerie_Pie.Points.Add(FTP);
            FTPPie.LegendText = "FTP (" + FTP + " packets)";

            DataPoint SFTPPie = APSerie_Pie.Points.Add(SFTP);
            SFTPPie.LegendText = "SFTP (" + SFTP + " packets)";

            DataPoint HTTPPie = APSerie_Pie.Points.Add(HTTP);
            HTTPPie.LegendText = "HTTP (" + HTTP + " packets)";

            DataPoint HTTPSPie = APSerie_Pie.Points.Add(HTTPS);
            HTTPSPie.LegendText = "HTTPS (" + HTTPS + " packets)";

            DataPoint SMTPPie = APSerie_Pie.Points.Add(SMTP);
            SMTPPie.LegendText = "SMTP (" + SMTP + " packets)";

            DataPoint DNSPie = APSerie_Pie.Points.Add(DNS);
            DNSPie.LegendText = "DNS (" + DNS + " packets)";

            DataPoint VNCPie = APSerie_Pie.Points.Add(VNC);
            VNCPie.LegendText = "VNC (" + VNC + " packets)";


            TrafficPieChart.Series.Add(APSerie_Pie);
        }

        private void LvRepeaterNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (LvRepeaterNames.SelectedItems.Count > 0)
            {
                LvRepeaterList.Items.Clear();

                AccessPoint[] Repeaters = LvRepeaterNames.SelectedItems[0].Tag as AccessPoint[];

                if (Repeaters != null)
                {
                    foreach (AccessPoint AP in Repeaters)
                    {
                        ListViewItem item = new ListViewItem(new string[]
                        {
                            AP.MacAddress,
                            AP.Manufacturer,
                            AP.BeaconFrame.TimeStamp.ToString(DateTimeFormat),
                            AP.WPS_Enabled.ToString()
                        });
                        item.Tag = AP;
                        LvRepeaterList.Items.Add(item);
                    }
                }
            }
        }

        private void followDeviceByGPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void liveModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!liveModeToolStripMenuItem.Checked)
            {
                if (AirservClient != null)
                {
                    AirservClient.Disconnect();
                }

                AirServSettingsForm sessingsForm = new AirServSettingsForm();

                if (sessingsForm.ShowDialog() == DialogResult.OK)
                {
                    this.AirservClient = sessingsForm.client;
                    this.LiveCaptureFile = new CapFile();
                    captureInfo.Clear();

                    this.AirservClient.onPacketArrival += AirservClient_onPacketArrival;

                    liveModeToolStripMenuItem.Checked = true;
                }

            }
        }

        Stopwatch sw = Stopwatch.StartNew();
        private void AirservClient_onPacketArrival(PacketDotNet.Packet packet, DateTime ArrivalTime)
        {
            this.LiveCaptureFile.ProcessPacket(packet, ArrivalTime);
            captureInfo.AddCapturefile(this.LiveCaptureFile);
            this.LiveCaptureFile.Clear();

            if (sw.ElapsedMilliseconds >= 1000)
            {
                this.Invoke(new Invoky(() => RefreshAll()));
                sw.Reset();
                sw.Start();
            }
        }

        private void APTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            
        }

        private void APList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (APList.SelectedIndices.Count > 0)
            {
                AccessPoint AP = this.APList_AccessPoints[APList.SelectedIndices[0]];
                if (AP != null)
                {
                    ApStationList.SetStations(new Station[0]); //refresh fix
                    ApStationList.SetStations(captureInfo.GetStationsFromAP(AP));
                }
            }
        }

        private void APList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            AccessPoint AP = this.APList_AccessPoints[e.ItemIndex];
            Station[] Stations = captureInfo.GetStationsFromAP(AP);

            string DeCloak_SSID = "";
            bool IsDeCloaked = captureInfo.DeCloakAP(AP, ref DeCloak_SSID);
            bool IsDanger = captureInfo.IsAccessPointDanger(AP);

            e.Item = new ListViewItem(new string[]
            {
                IsDeCloaked ? DeCloak_SSID : AP.SSID,
                Stations.Length.ToString(),
                IsDeCloaked.ToString()
            });
            e.Item.Tag = AP;

            if (IsDanger)
                e.Item.BackColor = Color.Red;
        }
    }
}