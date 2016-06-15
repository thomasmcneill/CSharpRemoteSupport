using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Windows;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Diagnostics;


namespace RemoteSupportServer
{

    public partial class SupportConnection : Form
    {
        struct _TransferRate
        {
            public Stopwatch stopwatch;
            public UInt32 RXBytes;
            public UInt32 TXBytes;
            public object RXBytes_Lock;
            public object TXBytes_Lock;
        }
        _TransferRate TransferRate;

        public _SupportUI ui;

        public Form1 _parent;
        public LogView myLogView;


        public void CreateUI()
        {
            ui = _parent.AddTab();

        }

        void AddToolButtons()
        {
            if (ui._ToolStrip.InvokeRequired)
            {
                ui._ToolStrip.Invoke(new MethodInvoker(delegate { AddToolButtons(); }));
            }
            else
            {
                ui.ToolBarStuff.Squares = new ToolStripComboBox();
                ui.ToolBarStuff.Squares.DropDownStyle = ComboBoxStyle.DropDownList;
                ui.ToolBarStuff.Squares.Items.Add("32");
                ui.ToolBarStuff.Squares.Items.Add("64");
                ui.ToolBarStuff.Squares.Items.Add("128");
                ui.ToolBarStuff.Squares.Items.Add("256");
                ui.ToolBarStuff.Squares.SelectedIndex = 2;
                ui.ToolBarStuff.Squares.SelectedIndexChanged += new System.EventHandler(Toolbar_Squares_SelectedIndexChanged);

                ui.ToolBarStuff.JPGQuality = new ToolStripComboBox();
                ui.ToolBarStuff.JPGQuality.DropDownStyle = ComboBoxStyle.DropDownList;
                ui.ToolBarStuff.JPGQuality.Items.Add("10%");
                ui.ToolBarStuff.JPGQuality.Items.Add("20%");
                ui.ToolBarStuff.JPGQuality.Items.Add("30%");
                ui.ToolBarStuff.JPGQuality.Items.Add("40%");
                ui.ToolBarStuff.JPGQuality.Items.Add("50%");
                ui.ToolBarStuff.JPGQuality.Items.Add("60%");
                ui.ToolBarStuff.JPGQuality.Items.Add("70%");
                ui.ToolBarStuff.JPGQuality.Items.Add("80%");
                ui.ToolBarStuff.JPGQuality.Items.Add("90%");
                ui.ToolBarStuff.JPGQuality.Items.Add("100%");
                ui.ToolBarStuff.JPGQuality.SelectedIndex = 7;
                ui.ToolBarStuff.JPGQuality.SelectedIndexChanged += new System.EventHandler(Toolbar_JPGQuality_SelectedIndexChanged);

                ui.ToolBarStuff.Refresh = new ToolStripButton();
                ui.ToolBarStuff.Refresh.Text = "Refresh";
                ui.ToolBarStuff.Refresh.Click += new System.EventHandler(Toolbar_Refresh_Click);

                ui.ToolBarStuff.SendFileButton = new ToolStripButton();
                ui.ToolBarStuff.SendFileButton.Text = "Send File";
                ui.ToolBarStuff.SendFileButton.Click += new System.EventHandler(Toolbar_SendFileButton_Click);

                ui.ToolBarStuff.TRansferRate = new ToolStripStatusLabel();


                ui._ToolStrip.Items.Add(ui.ToolBarStuff.Refresh);
                ui._ToolStrip.Items.Add(ui.ToolBarStuff.Squares);
                ui._ToolStrip.Items.Add(ui.ToolBarStuff.JPGQuality);
                ui._ToolStrip.Items.Add(ui.ToolBarStuff.SendFileButton);
                ui._ToolStrip.Items.Add(ui.ToolBarStuff.TRansferRate);

                ui.ToolBarStuff.SendFileProgress = new ToolStripProgressBar();
                ui._ToolStrip.Items.Add(ui.ToolBarStuff.SendFileProgress);

            }



        }

        void Toolbar_Refresh_Click(object sender, System.EventArgs e)
        {
            Byte[] b_int16 = new byte[2];
            b_int16[0] = 0;
            b_int16[1] = REFRESH_REQUEST;



        }
        void Toolbar_JPGQuality_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ToolStripComboBox cb = (ToolStripComboBox)sender;
            Int16 size = 80;
            switch (cb.SelectedIndex)
            {
                case 0:
                    size = 10;
                    break;
                case 1:
                    size = 20;
                    break;
                case 2:
                    size = 30;
                    break;
                case 3:
                    size = 40;
                    break;
                case 4:
                    size = 50;
                    break;
                case 5:
                    size = 60;
                    break;
                case 6:
                    size = 70;
                    break;
                case 7:
                    size = 80;
                    break;
                case 8:
                    size = 90;
                    break;
                case 9:
                    size = 100;
                    break;
            }

            List<Byte> BufferList = new List<Byte>();

            Byte[] b_int16 = new byte[2];
            b_int16[0] = 0;
            b_int16[1] = CHANGE_JPEG_QUALITY;
            BufferList.AddRange(b_int16);

            b_int16 = BitConverter.GetBytes(size);
            BufferList.AddRange(b_int16);
            SendData(ref BufferList);


        }
        void Toolbar_Squares_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ToolStripComboBox cb = (ToolStripComboBox)sender;
            Int16 size = 64;
            switch (cb.SelectedIndex)
            {
                case 0:
                    size = 32;
                    break;
                case 1:
                    size = 64;
                    break;
                case 2:
                    size = 128;
                    break;
                case 3:
                    size = 256;
                    break;
            }

            List<Byte> BufferList = new List<Byte>();

            Byte[] b_int16 = new byte[2];
            b_int16[0] = 0;
            b_int16[1] = CHANGE_SQUARE_SIZE;
            BufferList.AddRange(b_int16);

            b_int16 = BitConverter.GetBytes(size);
            BufferList.AddRange(b_int16);
            SendData(ref BufferList);




        }

        public void GetFocusBack()
        {
            if (ui._TabPage.InvokeRequired)
            {
                ui._TabPage.Invoke(new MethodInvoker(delegate { GetFocusBack(); }));
            }
            else
            {
                ui._TabPage.Focus();
            }

        }
        public void UpdateTransferRate(String text)
        {
            if (ui._ToolStrip.InvokeRequired)
            {
                ui._ToolStrip.Invoke(new MethodInvoker(delegate { UpdateTransferRate(text); }));
            }
            else
            {
                ui.ToolBarStuff.TRansferRate.Text = text;
            }

        }


        public void UpdateTitle(String text)
        {
            if (_parent.InvokeRequired)
            {
                _parent.Invoke(new MethodInvoker(delegate { UpdateTitle(text); }));
            }
            else
            {
                _parent.Text = text;
            }

        }




        public void UpdateFrameCounter()
        {
            if (ui._TabPage.InvokeRequired)
            {
                ui._TabPage.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        ui._TabPage.Text = lSquareCounter.ToString();
                    }));

            }
            else
            {
                ui._TabPage.Text = lSquareCounter.ToString();
            }

        }


    }
}
