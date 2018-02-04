using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WiFiSpy.src;

namespace WiFiSpy
{
    public partial class AirServSettingsForm : Form
    {
        public AirservClient client { get; private set; }
        public bool UseTcpIp { get; private set; }
        public bool UseSerial { get; private set; }

        public AirServSettingsForm()
        {
            InitializeComponent();
            base.DialogResult = DialogResult.Cancel;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtHost.Text))
            {
                MessageBox.Show("Fillin a valid Ip/Host name");
                return;
            }

            try
            {
                UseTcpIp = rbTcp.Checked;
                UseSerial = rbSerial.Checked;

                client = new AirservClient(txtHost.Text, (int)nudPort.Value, cmbSerialPort.Text, UseSerial);

                base.DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbSerialPort_DropDown(object sender, EventArgs e)
        {
            cmbSerialPort.Items.Clear();
            foreach (string port in SerialPort.GetPortNames())
            {
                cmbSerialPort.Items.Add(port);
            }
        }
    }
}
