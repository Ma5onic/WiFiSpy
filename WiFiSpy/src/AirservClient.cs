using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using WiFiSpy.src.AirServConnectors;

namespace WiFiSpy.src
{
    public class AirservClient
    {
        public const int HEADER_SIZE = 5;
        private object SendLock = new object();

        public delegate void PacketArrivedCallback(Packet packet, DateTime ArrivalTime, int Channel);
        public event PacketArrivedCallback onPacketArrival;

        private IAirServConnector AirServConnector;

        //receive info
        private ReceiveType ReceiveState = ReceiveType.Header;
        private int ReadOffset = 0;
        private int WriteOffset = 0;
        private int ReadableDataLen = 0;
        private int PayloadLen { get { return (int)net.nh_len; } }

        net_hdr net = new net_hdr();

        Random rnd = new Random();

        int[] ScanChannels = new int[] { 1, 3, 6, 9, 11, 2, 4, 5, 7, 8, 10, 12, 13 };
        int ChannelHopIndex = 0;
        
        public enum PacketType
        {
            NET_RC = 1,
            NET_GET_CHAN = 2,
            NET_SET_CHAN = 3,
            NET_WRITE = 4,
            NET_PACKET = 5,
            NET_GET_MAC = 6,
            NET_MAC = 7,
            NET_GET_MONITOR = 8,
            NET_GET_RATE = 9,
            NET_SET_RATE = 10,
        }

        public enum ReceiveType
        {
            Header,
            Payload
        }

        public AirservClient(string Host, int port, string ComPort, bool UseSerial)
        {
            if (UseSerial)
            {
                AirServConnector = new AirServSerialClient(ComPort);
                AirServConnector.Connect();
            }
            else
            {
                AirServConnector = new AirServTcpClient(Host, port);
                AirServConnector.Connect();
            }

            ThreadPool.QueueUserWorkItem(ReceiveThread);
            ThreadPool.QueueUserWorkItem(ChannelHopThread);
        }

        Stopwatch ChannelHopSw = Stopwatch.StartNew();

        uint PrevSize = 0;


        private void ReceiveThread(object o)
        {
            while (true) //fix later
            {
                byte[] Header = AirServConnector.Receive(HEADER_SIZE);

                if (Header != null)
                {
                    net.ReadHeader(Header, 0);

                    byte[] Payload = AirServConnector.Receive((int)net.nh_len);
                    net.ReadPayload(Payload, 0);

                    Packet packet = PacketDotNet.Packet.ParsePacket(PacketDotNet.LinkLayers.Ieee80211, net.nh_data);

                    if (packet != null)
                    {
                        DateTime ArrivalTime = DateTime.Now;
                        onPacketArrival(packet, ArrivalTime, net.Channel);
                    }
                }
            }
        }

        private void ChannelHopThread(object o)
        {
            while(true)
            {
                Thread.Sleep(250);

                try
                {
                    int newChannel = ScanChannels[ChannelHopIndex];
                    SetChannel(newChannel);
                    ChannelHopIndex++;

                    if (ChannelHopIndex > ScanChannels.Length - 1)
                        ChannelHopIndex = 0;

                }
                catch { }
            }
        }

        private void SendPacket(PacketType Command, byte[] data)
        {
            lock (SendLock)
            {
                byte[] Packet = new net_hdr().WriteData(Command, data);
                AirServConnector.Send(Packet, 0, Packet.Length);
            }
        }

        private void SetChannel(int Channel)
        {
            lock (SendLock)
            {
                byte[] Packet = new net_hdr().WriteData(PacketType.NET_SET_CHAN, new byte[] { 0, 0, 0, (byte)Channel});
                AirServConnector.Send(Packet, 0, Packet.Length);
            }

        }

        public void Disconnect()
        {
            AirServConnector.Disconnect();
        }

        private class net_hdr
        {
            //Thanks to: https://github.com/aircrack-ng/aircrack-ng/blob/master/src/osdep/network.h
            /*
             * Type
             * Packet Length
             * Payload
            */

            public PacketType nh_type { get; private set; }
            public uint nh_len { get; private set; }

            public byte[] PayloadHeader { get; private set; }
            public byte[] nh_data { get; private set; }

            public int Channel { get; private set; }

            public net_hdr()
            {
                this.PayloadHeader = new byte[5];
                this.nh_data = new byte[0];
            }

            public void ReadHeader(byte[] Data, int Offset)
            {
                this.nh_type = (PacketType)Data[Offset];

                Array.Reverse(Data, Offset + 1, 4); //reverse-endian
                this.nh_len = BitConverter.ToUInt32(Data, Offset + 1);
            }

            public void ReadPayload(byte[] Data, int Offset)
            {
                if (nh_type == PacketType.NET_PACKET)// || nh_type == PacketType.NET_RC)
                {
                    this.PayloadHeader = new byte[(int)nh_len];
                    Array.Copy(Data, Offset, PayloadHeader, 0, 32);

                    this.nh_data = new byte[(int)nh_len - 32];
                    Array.Copy(Data, Offset + 32, nh_data, 0, nh_data.Length);

                    this.Channel = PayloadHeader[19];
                }
                else
                {
                    //this.nh_data = new byte[(int)nh_len];
                    //Array.Copy(Data, Offset, nh_data, 0, nh_len);
                    

                    /*string derp = "";
                    for (int i = 0; i < nh_data.Length; i++)
                    {
                        if (char.IsLetter((char)nh_data[i]))
                        {
                            derp += (char)nh_data[i];
                        }
                    }
                    
                    Debug.WriteLine(derp);*/
                }
            }

            public byte[] WriteData(PacketType Command, byte[] Data)
            {
                PayloadHeader[0] = (byte)Command;

                byte[] PacketLength = BitConverter.GetBytes(Data.Length);
                Array.Reverse(PacketLength, 0, 4); //reverse-endian
                Array.Copy(PacketLength, 0, PayloadHeader, 1, 4);

                nh_data = Data;

                byte[] data = new byte[PayloadHeader.Length + nh_data.Length];
                Array.Copy(PayloadHeader, 0, data, 0, PayloadHeader.Length);
                Array.Copy(nh_data, 0, data, PayloadHeader.Length, nh_data.Length);
                return data;
            }
        }
    }
}