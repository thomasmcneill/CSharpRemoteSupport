using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        public Bitmap screenshot;

        private void ReceiveFullScreen()
        {
            Byte[] b_int32 = new byte[4];
            ReadData(ref b_int32);
            Int32 size = BitConverter.ToInt32(b_int32, 0);
            if (size > 0)
            {
                Byte[] pbuff = new byte[size];
                ReadData(ref pbuff);
                if (screenshot == null)
                    screenshot = new Bitmap(new MemoryStream(pbuff));
                OverLayPicture(pbuff, 0, 0);
                UpdatePictureBox();
                //_Picture.Invoke(new Action(() => _Picture.Image = Image.FromStream(new MemoryStream(pbuff))));
                //_Panel.Invoke(new Action(() => _Panel.Width = _Picture.Image.Width));
                //_Panel.Invoke(new Action(() => _Panel.Height = _Picture.Image.Height));
                SendACK();
                lSquareCounter = 0;
                GC.Collect();


            }

        }
        public void UpdatePicture(Image image)
        {
            if (ui._Picture.InvokeRequired)
            {
                ui._Picture.Invoke(new MethodInvoker(delegate { UpdatePicture(image); }));
            }
            else
            {
                ui._Picture.Image = image;
            }

        }

        public void OverLayPicture(byte[] pbuff, int x, int y)
        {
            if (ui._TabPage.InvokeRequired)
            {
                ui._TabPage.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        Do_OverLayPicture(pbuff, x, y);
                    }));

            }
            else
            {
                Do_OverLayPicture(pbuff, x, y);
            }

        }

        private void ReceiveDiff()
        {
            Byte[] b_int32 = new byte[4];

            ReadData(ref b_int32);
            Int32 X = BitConverter.ToInt16(b_int32, 0);
            ReadData(ref b_int32);
            Int32 Y = BitConverter.ToInt16(b_int32, 0);

            ReadData(ref b_int32);
            Int32 size = BitConverter.ToInt32(b_int32, 0);


            if (size > 0)
            {
                Byte[] pbuff = new byte[size];
                ReadData(ref pbuff);
                OverLayPicture(pbuff, X, Y);
                lSquareCounter++;
                UpdateFrameCounter();
            }



        }

        public void UpdatePictureBox()
        {
            if (ui._Picture.InvokeRequired)
            {
                ui._Picture.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        try
                        {
                            ui._Picture.Image = screenshot;
                        }
                        catch (Exception e)
                        {
                            myLogView.Append("UpdatePictureBox: " + e.Message);
                        }
                    }));

            }
            else
            {
                try
                {
                    ui._Picture.Image = screenshot;
                }
                catch (Exception e)
                {
                    myLogView.Append("UpdatePictureBox: " + e.Message);

                }

            }
        }

        public void Do_OverLayPicture(byte[] pbuff, int x, int y)
        {
            Graphics g;
            Bitmap IncomingImage;

            try
            {
                lock (screenshot)
                {
                    g = Graphics.FromImage(screenshot);
                }
            }
            catch (Exception e)
            {
                myLogView.Append("Do_OverLayPicture Graphics.FromImage(screenshot): " + e.Message);

                return;
            }


            try
            {
                IncomingImage = new Bitmap(new MemoryStream(pbuff));
                //g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                //g.DrawImage(Original, 0, 0);
            }
            catch (Exception e)
            {
                myLogView.Append("Do_OverLayPicture new Bitmap: " + e.Message);
                return;
            }

            try
            {
                g.DrawImage(IncomingImage, x, y);
            }
            catch (Exception e)
            {

                myLogView.Append("Do_OverLayPicture g.DrawImagep: " + e.Message);

                return;
            }
            GC.Collect();
        }

    }
}
