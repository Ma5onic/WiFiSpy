using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace WiFiSpy.src.AirServConnectors
{
    public class AirServSerialClient : IAirServConnector
    {
        private SerialPort serial;
        public string Port { get; private set; }

        public AirServSerialClient(string Port)
        {
            this.Port = Port;
        }

        public bool Connect()
        {
            try
            {
                serial = new SerialPort(Port, 115200, Parity.None, 8, StopBits.One);
                serial.Handshake = Handshake.XOnXOff;
                serial.Open();
                //System.Windows.Forms.MessageBox.Show("serial.Handshake: " + serial.Handshake);
                
                return true;
            }
            catch { }
            return false;
        }

        public void Disconnect()
        {
            serial.Close();
            serial.Dispose();
        }

        public byte[] Receive(int Count)
        {
            byte[] buffer = new byte[Count];
            int ToRead = Count;
            int WriteOffset = 0;

            while (ToRead > 0 && serial.IsOpen)
            {
                int read = serial.Read(buffer, WriteOffset, ToRead);

                if (read <= 0)
                    return null;

                ToRead -= read;
                WriteOffset += read;
            }

            if (!serial.IsOpen)
                return null;

            return buffer;
        }

        public void Send(byte[] Data, int Offset, int Length)
        {
            serial.Write(Data, Offset, Length);
        }
    }
}