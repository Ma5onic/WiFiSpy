namespace WiFiSpy
{
    partial class AirServSettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AirServSettingsForm));
            this.btnStart = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtHost = new System.Windows.Forms.TextBox();
            this.nudPort = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.rbTcp = new System.Windows.Forms.RadioButton();
            this.rbSerial = new System.Windows.Forms.RadioButton();
            this.label2 = new System.Windows.Forms.Label();
            this.cmbSerialPort = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(262, 254);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 0;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 44);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "IP / Host";
            // 
            // txtHost
            // 
            this.txtHost.Location = new System.Drawing.Point(94, 41);
            this.txtHost.Name = "txtHost";
            this.txtHost.Size = new System.Drawing.Size(162, 20);
            this.txtHost.TabIndex = 3;
            this.txtHost.Text = "192.168.1.25";
            // 
            // nudPort
            // 
            this.nudPort.Location = new System.Drawing.Point(262, 41);
            this.nudPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.nudPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudPort.Name = "nudPort";
            this.nudPort.Size = new System.Drawing.Size(74, 20);
            this.nudPort.TabIndex = 7;
            this.nudPort.Value = new decimal(new int[] {
            666,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 108);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(299, 143);
            this.label1.TabIndex = 8;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // rbTcp
            // 
            this.rbTcp.AutoSize = true;
            this.rbTcp.Checked = true;
            this.rbTcp.Location = new System.Drawing.Point(21, 12);
            this.rbTcp.Name = "rbTcp";
            this.rbTcp.Size = new System.Drawing.Size(61, 17);
            this.rbTcp.TabIndex = 9;
            this.rbTcp.TabStop = true;
            this.rbTcp.Text = "TCP/IP";
            this.rbTcp.UseVisualStyleBackColor = true;
            // 
            // rbSerial
            // 
            this.rbSerial.AutoSize = true;
            this.rbSerial.Location = new System.Drawing.Point(165, 12);
            this.rbSerial.Name = "rbSerial";
            this.rbSerial.Size = new System.Drawing.Size(51, 17);
            this.rbSerial.TabIndex = 10;
            this.rbSerial.Text = "Serial";
            this.rbSerial.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 76);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Com Port";
            // 
            // cmbSerialPort
            // 
            this.cmbSerialPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSerialPort.FormattingEnabled = true;
            this.cmbSerialPort.Location = new System.Drawing.Point(94, 73);
            this.cmbSerialPort.Name = "cmbSerialPort";
            this.cmbSerialPort.Size = new System.Drawing.Size(242, 21);
            this.cmbSerialPort.TabIndex = 12;
            this.cmbSerialPort.DropDown += new System.EventHandler(this.cmbSerialPort_DropDown);
            // 
            // AirServSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(357, 289);
            this.Controls.Add(this.cmbSerialPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.rbSerial);
            this.Controls.Add(this.rbTcp);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nudPort);
            this.Controls.Add(this.txtHost);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "AirServSettingsForm";
            this.ShowIcon = false;
            this.Text = "AirServ-NG - Settings";
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.NumericUpDown nudPort;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbTcp;
        private System.Windows.Forms.RadioButton rbSerial;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cmbSerialPort;
    }
}