using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Timers;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Concurrent;

namespace RemoteSupportClient
{

    public partial class Form1 : Form
    {

        //[DllImport("shcore.dll")]
        //private static extern int SetProcessDpiAwareness(ProcessDPIAwareness value);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetProcessDPIAware();

        private enum ProcessDPIAwareness
        {
            ProcessDPIUnaware = 0,
            ProcessSystemDPIAware = 1,
            ProcessPerMonitorDPIAware = 2
        }

        // RX Event Messages
        const int FRAME_ACK = 0x1;
        const int REFRESH_REQUEST = 0x2;
        const int KEYBOARD_INPUT = 0x3;
        const int START_FILE = 0x4;
        const int RECEIVE_FILE_BLOCK = 0x5;
        const int MOUSE_INPUT = 0x10;
        const int CHANGE_SQUARE_SIZE = 0x11;
        const int CHANGE_JPEG_QUALITY = 0x12;
        const int LAST_FILE_BLOCK_SENT = 0x13;


        // TX Event Messages
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



        PictureBox ScreenOverlay;
        Form ScreenOverlayForm;
        Bitmap ScreenOverlayBitmap;




        bool RemoteMouse_Left_Button_Down = false;
        bool RemoteMouse_Right_Button_Down = false;
        Point RemoteMouse_Location = new Point(0,0);

        public const Int16 Mouse_Event_Left_Button_Down = 1;
        public const Int16 Mouse_Event_Right_Button_Down = 2;
        public const Int16 Mouse_Event_Left_Button_Up = 3;
        public const Int16 Mouse_Event_Right_Button_Up = 4;
        public const Int16 Mouse_Event_Move = 5;



        public Form1()
        {
            InitializeComponent();

            bFile_Transfer_In_Progress = false;

            RemoteMouse_Left_Button_Down = false;
            RemoteMouse_Right_Button_Down = false;
            RemoteMouse_Location.X = 0;
            RemoteMouse_Location.Y = 0;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    //SetProcessDpiAwareness(ProcessDPIAwareness.ProcessPerMonitorDPIAware);
                    SetProcessDPIAware();

                }
            }
            catch (EntryPointNotFoundException)//this exception occures if OS does not implement this API, just ignore it.
            {

            }

            // Initialize Bitmap;
            InitializeBMP();

            Sender_Queue = new ConcurrentQueue<byte[]>();

            CreateOverLay();
            textBox_TempPath.Text = Path.GetTempPath();
            Downloads_Folder = System.Convert.ToString(Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty));

            JPG_codecInfo = ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);       // Set the JPEG encoder
            JPG_parameters = new EncoderParameters(1);
            JPG_parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JPEG_Square_Quality);        // Set Quality


        }

        void CreateOverLay()
        {
            ScreenOverlayForm = new Form();
            ScreenOverlay = new PictureBox();
            ScreenOverlayBitmap = new Bitmap(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);

            ScreenOverlayForm.BackColor = Color.White;
            ScreenOverlayForm.FormBorderStyle = FormBorderStyle.None;
            ScreenOverlayForm.Bounds = Screen.PrimaryScreen.Bounds;
            ScreenOverlayForm.ShowInTaskbar = false;
            ScreenOverlayForm.TopMost = true;
            ScreenOverlayForm.Size = new Size(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            ScreenOverlayForm.StartPosition = FormStartPosition.Manual;
            ScreenOverlayForm.SetDesktopLocation(0, 0);
            Application.EnableVisualStyles();

            ScreenOverlay.Location = new System.Drawing.Point(0, 0);
            ScreenOverlay.Size = new Size(SystemInformation.VirtualScreen.Width, SystemInformation.VirtualScreen.Height);
            ScreenOverlay.SizeMode = PictureBoxSizeMode.Normal;
            ScreenOverlay.Image = ScreenOverlayBitmap;

            ScreenOverlayForm.Controls.Add(ScreenOverlay);

            ScreenOverlayForm.AllowTransparency = true;

            SetWindowLong(ScreenOverlayForm.Handle, GWL_EXSTYLE, (IntPtr)(GetWindowLong(ScreenOverlayForm.Handle, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT));
            // set transparency to 50% (128)
            SetLayeredWindowAttributes(ScreenOverlayForm.Handle, 0, 192, LWA_ALPHA);

            Color BackColor = Color.White;
            ScreenOverlayForm.TransparencyKey = BackColor;
            ScreenOverlayForm.Opacity = 192 / 255f;

            ScreenOverlayForm.Show();

        }
        
        void MessageHandler(byte Message)
        {
            switch (Message)
            {
                case FRAME_ACK:         // Ack send another set or full screen



                    break;
                case REFRESH_REQUEST:     // Request new full screen.
                    iFrameCounter = iFrameCounter_Max;          // Set the frame counter so the next one sends full screen
                    break;

                case KEYBOARD_INPUT:     // Keyboard
                    if (tcpSocket.Available > 0)
                    {
                        ReceiveKeyboard();
                    }
                    break;
                case START_FILE:     // Start File Transfer;
                    textBox_Log_Update("File Transfer Start");
                    if (tcpSocket.Available > 0)
                    {

                        ReceiveFileInfo();
                    }
                    break;
                case RECEIVE_FILE_BLOCK:     // Start File Transfer;
                    if (tcpSocket.Available > 0)
                    {

                        ReceiveFileBlock();
                    }
                    break;
                case LAST_FILE_BLOCK_SENT:
                    textBox_Log_Update("Last Block Sent");
                    LastFileBlockSent();
                    break;

                case CHANGE_SQUARE_SIZE:
                    ChangeBlockSize();
                    break;
                case CHANGE_JPEG_QUALITY:
                    ChangeJPEGQuality();
                    break;
                case MOUSE_INPUT: // Mouse
                    if (tcpSocket.Available > 0)
                    {
                        ReceiveMouse();
                    }
                    break;
            }       // switch


        }


        private void Thread_MessageProcessor()
        {
            byte[] b_int16 = new byte[2];

            do
            {
                try
                {
                    if (tcpSocket.Available > 0)    
                    {
                        ReadData(ref b_int16);
                        if (b_int16[0] == 0)
                        {
                            MessageHandler(b_int16[1]);
                        }       // byte 0 == 0 check
                    }       // If block for data available

                    if(tcpSocket.Available == 0 && Sender_Queue.Count == 0 && Screen_Get_Squares_working == false)
                    {
                        if ((iFrameCounter % iFrameCounter_Max) == 0)      // We need to update the Keyframe
                        {
                                SendFullScreen();
                        }
                        else
                        {
                                Screen_Get_Squares_working = true;
                                new Task(Screen_Get_Squares_new).Start();
                        }
                        iFrameCounter++;

                    }
                }
                catch (Exception e)
                {
                    textBox_Log_Update("ReadInput: " + e.Message);
                }
            } while (!thread_Stop);
        }

        private void ReceiveKeyboard()
        {
            byte[] b_uint = new byte[sizeof(uint)];
            byte[] b_flags = new byte[2];
            ReadData(ref b_uint);
            int uMsg = BitConverter.ToInt32(b_uint, 0);

            ReadData(ref b_uint);
            uint lParam = BitConverter.ToUInt32(b_uint, 0);

            ReadData(ref b_uint);
            uint wParam = BitConverter.ToUInt32(b_uint, 0);

            ReadData(ref b_flags);

            INPUT input = new INPUT();
            input.type = (int)InputType.INPUT_KEYBOARD;
            input.ki.wVk = (short)wParam;
            input.ki.wScan = (short)((lParam >> 16) & 0xff);

            if(b_flags[0] == 0)
                input.ki.dwFlags = (int)KEYEVENTF.KEYUP;
            else
                input.ki.dwFlags = (int)KEYEVENTF.KEYDOWN;

            if( ((lParam >> 24) & 0x1) == 1)
                input.ki.dwFlags = input.ki.dwFlags | (int)KEYEVENTF.EXTENDEDKEY;

            input.ki.dwExtraInfo = (IntPtr)0;

            INPUT[] pInputs = new INPUT[] { input };

            SendInput(1, pInputs, Marshal.SizeOf(input));

            textBox_Log_Update(String.Format("Keyboard: ScanCode({0}) VirtualKey({1})", input.ki.wScan, input.ki.wVk));

        }


        private void EnqueueMessage(Byte a, Byte b)
        {
            byte[] b_int16 = new byte[2];
            b_int16[0] = a;
            b_int16[1] = b;
            Sender_Queue.Enqueue(b_int16);

        }


        private void ChangeBlockSize()
        {
            byte[] b_int16 = new byte[2];
            ReadData(ref b_int16);
            SquareSize = BitConverter.ToInt16(b_int16, 0);
            bmp_Square = new Bitmap(SquareSize, SquareSize);
            textBox_Log_Update(String.Format("SquareSize({0}", SquareSize));
            //bmp_8bit_Square = new Bitmap(SquareSize, SquareSize, PixelFormat.Format8bppIndexed);
        }



        private void ReceiveMouse()
        {
            byte[] b_int32 = new byte[sizeof(Int32)];
            ReadData(ref b_int32);
            int m_event = BitConverter.ToInt32(b_int32, 0);

            ReadData(ref b_int32);
            Int32 x = BitConverter.ToInt32(b_int32, 0);

            ReadData(ref b_int32);
            Int32 y = BitConverter.ToInt32(b_int32, 0);

            //textBox_Log_Update(String.Format("Mouse Event: {0} {1} {2}", m_event, x, y));


            // Set the mouse properties
            uint Flags = MOUSEEVENTF_ABSOLUTE;
            if (m_event == Mouse_Event_Left_Button_Down)
            {
                Flags = Flags | MOUSEEVENTF_LEFTDOWN;
                RemoteMouse_Left_Button_Down = true;
                textBox_Log_Update(String.Format("Mouse: Left Down"));
            }
            if (m_event == Mouse_Event_Left_Button_Up)
            {
                Flags = Flags | MOUSEEVENTF_LEFTUP;
                RemoteMouse_Left_Button_Down = false;
                textBox_Log_Update(String.Format("Mouse: Left Up"));
            }
            if (m_event == Mouse_Event_Right_Button_Down)
            {
                Flags = Flags | MOUSEEVENTF_RIGHTDOWN;
                RemoteMouse_Right_Button_Down = true;
                textBox_Log_Update(String.Format("Mouse: Right Down"));
            }
            if (m_event == Mouse_Event_Right_Button_Up)
            {
                Flags = Flags | MOUSEEVENTF_RIGHTUP;
                RemoteMouse_Right_Button_Down = false;
                textBox_Log_Update(String.Format("Mouse: Right Up"));
            }

            RemoteMouse_Location.X = x;
            RemoteMouse_Location.Y = y;

            Cursor.Position = RemoteMouse_Location;

            mouse_event(Flags, 0, 0, 0, 0);
            //textBox_Log_Update(String.Format("Mouse Simulate Click: Left {0} Right {1} ({2},{3}) Flags({4})", RemoteMouse_Left_Button_Down.ToString(), RemoteMouse_Right_Button_Down.ToString(), x.ToString(), y.ToString(), Flags.ToString()));
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            button_Start.Enabled = false;
            Connect();

        }






        
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                thread_Stop = true;

            }
            catch (Exception ex)
            {

            }
        }

        private void textBox_TempPath_TextChanged(object sender, EventArgs e)
        {

        }
        public void textBox_Ticks_Update(String text)
        {
            try {
                if (textBox_Ticks.InvokeRequired)
                {
                    textBox_Ticks.Invoke(new MethodInvoker(delegate { textBox_Ticks_Update(text); }));
                }
                else
                {
                    textBox_Ticks.Text = text;
                }
            }
            catch (Exception e)
            {

            }

        }
        public void textBox_Log_Update(String text)
        {
            try
            {
                if (textBox_Ticks.InvokeRequired)
                {
                    textBox_Ticks.Invoke(new MethodInvoker(delegate { textBox_Log_Update(text); }));
                }
                else
                {
                    textBox_Log.AppendText(text + "\r\n");
                }
            }
            catch (Exception e)
            {


            }

        }


    }

}
