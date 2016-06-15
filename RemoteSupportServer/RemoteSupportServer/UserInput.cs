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
        public const Int16 Mouse_Event_Left_Button_Down = 1;
        public const Int16 Mouse_Event_Right_Button_Down = 2;
        public const Int16 Mouse_Event_Left_Button_Up = 3;
        public const Int16 Mouse_Event_Right_Button_Up = 4;
        public const Int16 Mouse_Event_Move = 5;


        public const Int16 Mouse_Event_MinUpdateInterval = 2000;     //milliseconds
        public const Int16 Mouse_Event_MinUpdateMove = 16;  //Pixels
        public Point Mouse_LastSentPosition = new Point(0, 0);
        public Int32 Mouse_LastSentMove_Ticks = Environment.TickCount;

        public MessageFilter _MessageFilter = null;


        public void SendKey(_KeyStrokes ks)
        {
            List<Byte> BufferList = new List<Byte>();

            byte[] b_uint;
            Byte[] KEYSTROKE = new byte[2];

            KEYSTROKE[0] = 0;
            KEYSTROKE[1] = KEYBOARD_INPUT;
            BufferList.AddRange(KEYSTROKE);

            b_uint = BitConverter.GetBytes(ks.msg);
            BufferList.AddRange(b_uint);

            b_uint = BitConverter.GetBytes(ks.lParam); ;
            BufferList.AddRange(b_uint);

            b_uint = BitConverter.GetBytes(ks.wParam);
            BufferList.AddRange(b_uint);

            KEYSTROKE[0] = (byte)(ks.KeyDown ? 1 : 0);
            KEYSTROKE[1] = (byte)(ks.SysKeyDown ? 1 : 0);
            BufferList.AddRange(KEYSTROKE);

            SendData(ref BufferList);



        }


        void HookKeyboard()
        {
            if (this._parent.InvokeRequired)
            {
                this._parent.Invoke(new MethodInvoker(delegate { HookKeyboard(); }));
            }
            else
            {
                this._parent.mobjMessageFilter.AddHWND(ui._TabPage.Handle);
            }




        }


        void HookMouse()
        {
            ui._Picture.MouseDown += MouseDown_handler;
            ui._Picture.MouseUp += MouseUp_handler;
            ui._Picture.MouseMove += MouseMove_handler;

        }
        public void OffsetMouse(ref Point p)
        {

            Rectangle work = ui._Picture.Bounds;
            work.Intersect(ui._Panel.ClientRectangle);
            p.X += work.X;
            p.Y += work.Y;
        }
        public void MouseDown_handler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Point p = new Point(e.X, e.Y);
            OffsetMouse(ref p);
            ClickCounter++;
            if (e.Button == MouseButtons.Left)
                SendMouseEvent(p, Mouse_Event_Left_Button_Down);
            if (e.Button == MouseButtons.Right)
                SendMouseEvent(p, Mouse_Event_Right_Button_Down);

            GetFocusBack();
        }
        public void MouseUp_handler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Point p = new Point(e.X, e.Y);
            OffsetMouse(ref p);

            if (e.Button == MouseButtons.Left)
                SendMouseEvent(p, Mouse_Event_Left_Button_Up);
            if (e.Button == MouseButtons.Right)
                SendMouseEvent(p, Mouse_Event_Right_Button_Up);

        }
        public void MouseMove_handler(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Point p = new Point(e.X, e.Y);
            OffsetMouse(ref p);

            //MessageBox.Show(p.X.ToString());
            int dx = Math.Abs(p.X - Mouse_LastSentPosition.X);
            int dy = Math.Abs(p.Y - Mouse_LastSentPosition.Y);
            Int32 dt = Environment.TickCount - Mouse_LastSentMove_Ticks;

            // If the mouse moved for than 10 pixels or the mouse hasn't moved in the last 500ms or a button is down
            if (dy > Mouse_Event_MinUpdateMove || dx > Mouse_Event_MinUpdateMove || dt > Mouse_Event_MinUpdateInterval || e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                SendMouseEvent(p, Mouse_Event_Move);
                Mouse_LastSentPosition = p;
                Mouse_LastSentMove_Ticks = Environment.TickCount;

            }

        }
        private void SendMouseEvent(Point p, Int32 mouse_event)
        {
            if (_Socket.Connected)
            {
                List<Byte> BufferList = new List<Byte>();

                byte[] buffer;


                Byte[] MouseEvent = new byte[2];
                MouseEvent[0] = 0x0;
                MouseEvent[1] = MOUSE_INPUT;
                BufferList.AddRange(MouseEvent);


                buffer = BitConverter.GetBytes((Int32)mouse_event);
                BufferList.AddRange(buffer);

                buffer = BitConverter.GetBytes((Int32)p.X);
                BufferList.AddRange(buffer);

                buffer = BitConverter.GetBytes((Int32)p.Y);
                BufferList.AddRange(buffer);
                SendData(ref BufferList);
            }
        }



    }
}
