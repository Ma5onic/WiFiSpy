using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace WiFiSpy.src.AirServConnectors
{
    public class AirServTcpClient : IAirServConnector
    {
        public const int BUFFER_SIZE = 8192;
        private byte[] Buffer = new byte[BUFFER_SIZE];
        private Socket client;

        public string Host { get; private set; }
        public int Port { get; private set; }
        public bool Isconnected { get; private set; }

        public AirServTcpClient(string Host, int Port)
        {
            this.Host = Host;
            this.Port = Port;
        }

        public bool Connect()
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(Host, Port);
                Isconnected = true;
                return true;
            }
            catch { }
            return false;
        }

        public void Disconnect()
        {
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                client.Dispose();
            }
            catch { }
        }

        public byte[] Receive(int Count)
        {
            byte[] buffer = new byte[Count];
            int ToRead = Count;
            int WriteOffset = 0;

            while (ToRead > 0 && Isconnected)
            {
                int read = client.Receive(buffer, WriteOffset, ToRead, SocketFlags.None);

                if (read <= 0)
                {
                    Isconnected = false;
                    return null;
                }

                ToRead -= read;
                WriteOffset += read;
            }

            if (!Isconnected)
                return null;

            return buffer;
        }

        public void Send(byte[] Data, int Offset, int Length)
        {
            client.Send(Data, Offset, Length, SocketFlags.None);
        }
    }
}