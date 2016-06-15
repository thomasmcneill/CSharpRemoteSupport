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

    public partial class  SupportConnection : Form
    {




        //bool[] File_Transfer_Blocks;




        long lSquareCounter = 0;

        public volatile bool StopThread;
        object _StopThread = new object();



        // TX Event Messages
        const int FRAME_ACK = 0x1;
        const int REFRESH_REQUEST = 0x2;
        const int KEYBOARD_INPUT = 0x3;
        const int START_FILE = 0x4;
        const int RECEIVE_FILE_BLOCK = 0x5;
        const int MOUSE_INPUT = 0x10;
        const int CHANGE_SQUARE_SIZE = 0x11;
        const int CHANGE_JPEG_QUALITY = 0x12;
        const int LAST_FILE_BLOCK_SENT = 0x13;

        // RX Event Messages
        const int FULL_SCREEN_JPG = 0x1;
        const int JPG_SQUARE = 0x2;
        const int SQUARES_DONE = 0x3;
        const int CANT_CREATE_FILE = 0x4;
        const int START_FILE_TRANSFER = 0x5;
        const int STOP_FILE_TRANSFER = 0x6;
        const int FILE_TRANSFER_ACK_BLOCK = 0x7;
        const int FILE_MISSING_BLOCK = 0x8;
        const int FILE_TRANSFER_DONE = 0x9;
        const int PNG_SQUARE = 0xa;



        int ClickCounter = 0;



        public void Start()
        {
            StopThread = false;
            FileTransfer.bTransfer_In_Progress = false;

            Sender_Queue = new ConcurrentQueue<byte[]>();


            HookMouse();
            HookKeyboard();

            AddToolButtons();

            TransferRate.TXBytes_Lock = new object();
            TransferRate.RXBytes_Lock = new object();
            TransferRate.stopwatch = new Stopwatch();
            TransferRate.stopwatch.Start();

            thread_Listener = new Thread(new ThreadStart(Listener_Thread));
            thread_Listener.Start();

            thread_Sender = new Thread(new ThreadStart(Sender_Thread));
            thread_Sender.Start();


        }

        public void Stop()
        {
            lock (_StopThread)
                StopThread = true;


        }


        public void Listener_Thread()
        {
            _KeyStrokes ks;
            do
            {
                try
                {
                    if (StopThread)
                        return;
                    if(TransferRate.stopwatch.ElapsedMilliseconds > 2000)
                    {
                        try {
                            double ms = TransferRate.stopwatch.ElapsedMilliseconds;
                            double TXBytes = 0;
                            double RXBytes = 0;
                            TransferRate.stopwatch.Stop();


                            lock (TransferRate.TXBytes_Lock)
                            {
                                TXBytes = TransferRate.TXBytes;
                                TransferRate.TXBytes = 0;

                            }
                            lock (TransferRate.RXBytes_Lock)
                            {
                                RXBytes = TransferRate.RXBytes;
                                TransferRate.RXBytes = 0;
                            }


                            TXBytes = (TXBytes / (ms / 1000)) / 1000;
                            RXBytes = (RXBytes / (ms / 1000)) / 1000;
                            UpdateTransferRate(String.Format("TX: {0:0.##}KB/s  RX: {1:0.##}KB/s", TXBytes, RXBytes));
                            TransferRate.stopwatch.Restart();

                        }
                        catch (Exception e)
                        {
                            myLogView.Append("TransferRate: " + e.Message);
                        }

                    }
                    if(_MessageFilter.KeyStrokes.Count > 0)
                    {
                        _MessageFilter.KeyStrokes.TryDequeue(out ks);
                        SendKey(ks);

                    }
                    if (_Socket.Available > 0)  // see if data is available
                    {
                        Byte[] packet_type = new byte[2];    // read packet type
                        ReadData(ref packet_type);

                        if (packet_type[0] == 0)         // first byte is 0
                        {

                            if (packet_type[1] == FULL_SCREEN_JPG)             // We are expecting full screen JPG image
                            {
                                ReceiveFullScreen();
                            }



                            if (packet_type[1] == JPG_SQUARE)             // We are expecting a diff square JPG image
                            {
                                ReceiveDiff();
                            }
                            if (packet_type[1] == PNG_SQUARE)             // We are expecting a diff square JPG image
                            {
                                ReceiveDiff();
                            }


                            if (packet_type[1] == SQUARES_DONE)             // Square are done so we update the picture box and ACK
                            {
                                UpdatePictureBox();
                                SendACK();
                                lSquareCounter = 0;
                            }
                            if (packet_type[1] == START_FILE_TRANSFER)             // Square are done so we update the picture box and ACK
                            {
                                FileTransfer.bTransfer_In_Progress = true;
                                SendNextFileBlock();
                                SendNextFileBlock();
                                SendNextFileBlock();
                                SendNextFileBlock();
                                SendNextFileBlock();
                            }
                            if (packet_type[1] == FILE_TRANSFER_ACK_BLOCK)             // Square are done so we update the picture box and ACK
                            {
                                if(FileTransfer.bTransfer_In_Progress)
                                {
                                    FileTransferAckBlock();

                                }
                            }
                            if (packet_type[1] == FILE_MISSING_BLOCK)             // Square are done so we update the picture box and ACK
                            {
                                if (FileTransfer.bTransfer_In_Progress)
                                {
                                    FileMissingBlock();

                                }
                            }
                            if (packet_type[1] == FILE_TRANSFER_DONE)             // Square are done so we update the picture box and ACK
                            {
                                if (FileTransfer.bTransfer_In_Progress)
                                {
                                    FileTransferDone();

                                }
                            }

                        }

                    }
                }
                catch (Exception e)
                {
                    myLogView.Append("Listener_Thread: " + e.Message);


                }
            } while (1 == 1);
        }











        //private ToolStrip toolStrip1;
        //private TabControl tabControl1;


        private void SupportConnection_Load(object sender, EventArgs e)
        {

        }
    }
}
