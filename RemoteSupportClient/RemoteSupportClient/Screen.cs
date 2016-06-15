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


        Rectangle rect_ScreenDimensions;

        Int32 iFrameCounter = 0;
        Int32 iFrameCounter_Max = 10000;
        Int32 JPEG_Square_Quality = 80;
        Int16 SquareSize = 64;
        bool ScreenLine_OddEven = false;

        volatile bool Screen_Get_Squares_working = false;

        System.Drawing.Bitmap bmp_LastScreenShot;
        System.Drawing.Bitmap bmp_CurrentScreenShot;
        Bitmap bmp_Square;

        ImageCodecInfo JPG_codecInfo;
        EncoderParameters JPG_parameters;


        void Screen_Get_Squares_new()
        {
            UInt32 BitsPerPixel = (UInt32)Image.GetPixelFormatSize(bmp_LastScreenShot.PixelFormat) / 8;

            int SquaresUpdated = 0;
            ////////////////////////////////////////////////////////
            // Get new screen shot
            /////////////////////////////////////////////////////////////
            using (Graphics h = Graphics.FromImage(bmp_CurrentScreenShot))
            {
                int cursor_x = 0;
                int cursor_y = 0;

                // Update the screenshot by copying it from the screen to the bmp
                h.CopyFromScreen(rect_ScreenDimensions.X, rect_ScreenDimensions.Y, 0, 0, bmp_CurrentScreenShot.Size);

                // copy the mouse cursor to the screen shot
                try
                {
                    Bitmap cursor = CaptureCursor(ref cursor_x, ref cursor_y);
                    if (cursor != null)
                    {
                        h.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        h.DrawImage(cursor, cursor_x, cursor_y);
                    }
                }
                catch (Exception e)
                {
                    textBox_Log_Update("CaptureCursor: " + e.Message);
                }

            }


            // Get Pointer to data
            BitmapData data_bmp_Square_Old = bmp_LastScreenShot.LockBits(new Rectangle(0, 0, bmp_LastScreenShot.Width, bmp_LastScreenShot.Height), ImageLockMode.ReadWrite, bmp_LastScreenShot.PixelFormat);
            BitmapData data_bmp_Square_New = bmp_CurrentScreenShot.LockBits(new Rectangle(0, 0, bmp_LastScreenShot.Width, bmp_LastScreenShot.Height), ImageLockMode.ReadOnly, bmp_LastScreenShot.PixelFormat);
            IntPtr ptr_bmp_Square_Old = data_bmp_Square_Old.Scan0;
            IntPtr ptr_bmp_Square_New = data_bmp_Square_New.Scan0;

            ////////////////////////////////////////////////
            // Create Square Bit Map
            ////////////////////////////////////////////
            int Width_in_Squares = (data_bmp_Square_Old.Width % SquareSize) > 0 ? (data_bmp_Square_Old.Width / SquareSize) + 1 : (data_bmp_Square_Old.Width / SquareSize);
            int Height_in_Squares = (data_bmp_Square_Old.Height % SquareSize) > 0 ? (data_bmp_Square_Old.Height / SquareSize) + 1 : (data_bmp_Square_Old.Height / SquareSize);
            bool[,] SquareMap_Flags = new bool[Width_in_Squares, Height_in_Squares];

            // Preset the array to false 
            for (int y = 0; y < Height_in_Squares; y++)
                for (int x = 0; x < Width_in_Squares; x++)
                    SquareMap_Flags[x, y] = false;


            int Y_Counter = 0;

            int Width_Square_Counter = 0;
            int Height_Square_Counter = 0;

            int MemoryStart;
            UIntPtr Ptr_Memcmp_Length = new UIntPtr((uint)SquareSize * BitsPerPixel);  // 3 Bytes Per Pixel (24Bit)
            UIntPtr Ptr_Memcmp_LineLength = new UIntPtr((uint)data_bmp_Square_Old.Width * BitsPerPixel);
            int Memcmp_Length = SquareSize * (short)BitsPerPixel;
            IntPtr Cmp1, Cmp2;

            int Memcmp_Result;
            int Y_Counter_Start = 0;
            if (ScreenLine_OddEven)
            {
                Y_Counter_Start = 0;
                ScreenLine_OddEven = false;
            } else
            {
                Y_Counter_Start = 1;
                ScreenLine_OddEven = true;
            }
                

            for (Y_Counter = Y_Counter_Start; Y_Counter < bmp_CurrentScreenShot.Height; Y_Counter+=2)
            {
                // Calculate where we are starting in memory, current scan line times width in bytes of scanline
                MemoryStart = (data_bmp_Square_Old.Stride * Y_Counter);

                // Get the Y axis in Squares
                Height_Square_Counter = Y_Counter / SquareSize;

                // Check entire line first.  If the line is different then we can break it down in to squares
                Cmp1 = IntPtr.Add(ptr_bmp_Square_Old, MemoryStart);
                Cmp2 = IntPtr.Add(ptr_bmp_Square_New, MemoryStart);
                Memcmp_Result = memcmp(Cmp1, Cmp2, Ptr_Memcmp_LineLength);


                if (Memcmp_Result != 0)
                {
                    for (Width_Square_Counter = 0; Width_Square_Counter < Width_in_Squares; Width_Square_Counter++)
                    {
                        //  See if this square has been flagged already.  If it has we can skip checking it
                        if (SquareMap_Flags[Width_Square_Counter, Height_Square_Counter] == false)
                        {

                            Cmp1 = IntPtr.Add(ptr_bmp_Square_Old, MemoryStart);
                            Cmp2 = IntPtr.Add(ptr_bmp_Square_New, MemoryStart);
                            Memcmp_Result = memcmp(Cmp1, Cmp2, Ptr_Memcmp_Length);

                            // If the memory is not the same then this "Square" needs to be sent.  Flag this square is not the same and break the loop
                            if (Memcmp_Result != 0)
                            {
                                SquaresUpdated++;
                                //textBox_Log_Update(String.Format("Squares Updated: {0}", SquaresUpdated));

                                SquareMap_Flags[Width_Square_Counter, Height_Square_Counter] = true;
                            }
                        }
                        MemoryStart += Memcmp_Length;        // Increment  memory start, 3 Bytes Per Pixel (24Bit)
                    }
                }

            }
            
            memcpy(ptr_bmp_Square_Old, ptr_bmp_Square_New, (UIntPtr)(data_bmp_Square_Old.Stride * data_bmp_Square_Old.Height));
            bmp_LastScreenShot.UnlockBits(data_bmp_Square_Old);
            bmp_CurrentScreenShot.UnlockBits(data_bmp_Square_New);

            int NewSquares = 0;
            for (int y = 0; y < Height_in_Squares; y++)
            {
                for (int x = 0; x < Width_in_Squares; x++)
                {
                    if (SquareMap_Flags[x, y])
                    {
                        NewSquares++;
                        Screen_SendSquare(x, y);
                    }
                }
            }


            //bmp_LastScreenShot.Dispose();
            //bmp_LastScreenShot = new Bitmap(bmp_CurrentScreenShot);
            
            EnqueueMessage(0, SQUARES_DONE);
            //GC.Collect();

            Screen_Get_Squares_working = false;


        }
        void Screen_SendSquare(int SX, int SY)
        {


            Graphics g = Graphics.FromImage(bmp_Square);
            g.DrawImage(bmp_CurrentScreenShot, 0, 0, new Rectangle(SX * SquareSize, SY * SquareSize, SquareSize, SquareSize), GraphicsUnit.Pixel);
            g.Dispose();


            MemoryStream jpg_Stream = new MemoryStream();
            MemoryStream png_Stream = new MemoryStream();


            try
            {
                bmp_Square.Save(jpg_Stream, JPG_codecInfo, JPG_parameters);

            }
            catch (Exception e)
            {
                textBox_Log_Update("Sendsquare JPG Encoder error: " + e.Message);
                return;
            }

            bmp_Square.Save(png_Stream, ImageFormat.Png);


            // Get bitmap data
            Byte[] jpg_array = jpg_Stream.GetBuffer();
            Byte[] png_array = png_Stream.GetBuffer();

            // Send data length
            Int32 jpg_length = jpg_array.Length;
            Int32 png_length = png_array.Length;


            // Send the type for the data
            List<Byte> BufferList = new List<Byte>();

            Byte[] message = new byte[2];
            byte[] length_buffer;

            if (png_length < jpg_length)
            {
                message[0] = 0;
                message[1] = PNG_SQUARE;                    // Type 2 is a JPG square
                length_buffer = BitConverter.GetBytes(png_length);
            }
            else
            {
                message[0] = 0;
                message[1] = JPG_SQUARE;                    // Type 2 is a JPG square
                length_buffer = BitConverter.GetBytes(jpg_length);
            }
            //textBox_Log_Update(String.Format("JPG: {0}  PNG:{1}", jpg_length, png_length));


            BufferList.AddRange(message);

            // Get the X,Y Location
            Byte[] bmp_Int32 = new byte[4];
            Int32 X = SX * SquareSize;
            bmp_Int32 = BitConverter.GetBytes(X);
            BufferList.AddRange(bmp_Int32);

            Int32 Y = SY * SquareSize;
            bmp_Int32 = BitConverter.GetBytes(Y);
            BufferList.AddRange(bmp_Int32);


            // Send length
            BufferList.AddRange(length_buffer);


            // send image data
            if (png_length < jpg_length)
            {

                BufferList.AddRange(png_array);
            }
            else
            {
                BufferList.AddRange(jpg_array);

            }

            // Enqueue data
            byte[] buffer = BufferList.ToArray();
            Sender_Queue.Enqueue(buffer);

            BufferList.Clear();

            jpg_Stream.Dispose();
            png_Stream.Dispose();



        }


        void SendFullScreen()
        {
            //tcpclnt.Connected;        


            CURSORINFO pci;
            pci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            int cursor_x = 0;
            int cursor_y = 0;

            // Validate the screen size.  It can change
            if (rect_ScreenDimensions != GetScreenSize())
            {
                bmp_LastScreenShot.Dispose();
                bmp_CurrentScreenShot.Dispose();
                InitializeBMP();


            }
            try
            {
                // Get screenshow in NewScreenShot
                using (Graphics h = Graphics.FromImage(bmp_LastScreenShot))
                {
                    h.CopyFromScreen(rect_ScreenDimensions.X, rect_ScreenDimensions.Y, 0, 0, bmp_LastScreenShot.Size);        // Update the Diff since we are going to send the entire screen 
                    Bitmap cursor = CaptureCursor(ref cursor_x, ref cursor_y);
                    if (cursor != null)
                    {
                        h.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        h.DrawImage(cursor, cursor_x, cursor_y);
                    }

                }




                // Send the full screen image
                if (tcpConnection.Connected)
                {
                    using (MemoryStream bitmap_Stream = new MemoryStream())
                    {

                        bmp_LastScreenShot.Save(bitmap_Stream, JPG_codecInfo, JPG_parameters);                                                       // Save to memory Stream


                        List<Byte> BufferList = new List<Byte>();

                        // Set message type
                        Byte[] message = new byte[2];
                        message[0] = 0;
                        message[1] = FULL_SCREEN_JPG;                    // Type 1 is JPG full screenshot
                        BufferList.AddRange(message);

                        // send size and data
                        Byte[] bmp_array = bitmap_Stream.GetBuffer();           // Get array from stream
                        Int32 length = bmp_array.Length;                        // get length
                        byte[] length_buffer = BitConverter.GetBytes(length);   // convert length to byte array
                        BufferList.AddRange(length_buffer);
                        BufferList.AddRange(bmp_array);


                        Byte[] Buffer = BufferList.ToArray();
                        Sender_Queue.Enqueue(Buffer);

                    }
                }
            }
            catch (Exception e)
            {
                textBox_Log_Update("SendScreen:  " + e.Message);
            }
        }
        private void ChangeJPEGQuality()
        {
            byte[] b_int16 = new byte[2];
            ReadData(ref b_int16);
            JPEG_Square_Quality = BitConverter.ToInt16(b_int16, 0);
            JPG_parameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, JPEG_Square_Quality);        // Set Quality

            textBox_Log_Update(String.Format("Change Quality to {0}", JPEG_Square_Quality));

        }
        private void InitializeBMP()
        {
            rect_ScreenDimensions = GetScreenSize();
            bmp_LastScreenShot = new Bitmap(rect_ScreenDimensions.Width, rect_ScreenDimensions.Height);
            bmp_CurrentScreenShot = new Bitmap(rect_ScreenDimensions.Width, rect_ScreenDimensions.Height);
            bmp_Square = new Bitmap(SquareSize, SquareSize);
            //bmp_8bit_Square = new Bitmap(SquareSize, SquareSize, PixelFormat.Format8bppIndexed);

        }
        Rectangle GetScreenSize()
        {
            Rectangle r = new Rectangle();

            r.X = SystemInformation.VirtualScreen.Left;
            r.Y = SystemInformation.VirtualScreen.Top;
            r.Width = SystemInformation.VirtualScreen.Width;
            r.Height = SystemInformation.VirtualScreen.Height;
            return r;
        }
        Bitmap CaptureCursor(ref int x, ref int y)
        {
            int TernaryRasterOperations_SRCCOPY = 0x00CC0020;
            int TernaryRasterOperations_SRCPAINT = 0x00EE0086;
            int TernaryRasterOperations_SRCAND = 0x008800C6;
            int TernaryRasterOperations_SRCINVERT = 0x00660046;
            int TernaryRasterOperations_SRCERASE = 0x00440328;
            int TernaryRasterOperations_NOTSRCCOPY = 0x00330008;
            int TernaryRasterOperations_NOTSRCERASE = 0x001100A6;
            int TernaryRasterOperations_MERGECOPY = 0x00C000CA;
            int TernaryRasterOperations_MERGEPAINT = 0x00BB0226;
            int TernaryRasterOperations_PATCOPY = 0x00F00021;
            int TernaryRasterOperations_PATPAINT = 0x00FB0A09;
            int TernaryRasterOperations_PATINVERT = 0x005A0049;
            int TernaryRasterOperations_DSTINVERT = 0x00550009;
            int TernaryRasterOperations_BLACKNESS = 0x00000042;
            int TernaryRasterOperations_WHITENESS = 0x00FF0062;
            int TernaryRasterOperations_CAPTUREBLT = 0x40000000;

            CURSORINFO cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(cursorInfo);
            if (!GetCursorInfo(out cursorInfo))
                return null;

            if (cursorInfo.flags != CURSOR_SHOWING)
                return null;

            IntPtr hicon = CopyIcon(cursorInfo.hCursor);
            if (hicon == IntPtr.Zero)
                return null;

            ICONINFO iconInfo;
            if (!GetIconInfo(hicon, out iconInfo))
                return null;

            x = cursorInfo.ptScreenPos.x - ((int)iconInfo.xHotspot);
            y = cursorInfo.ptScreenPos.y - ((int)iconInfo.yHotspot);

            using (Bitmap maskBitmap = Bitmap.FromHbitmap(iconInfo.MaskBitmap))
            {
                // Is this a monochrome cursor?
                if (maskBitmap.Height == maskBitmap.Width * 2)
                {
                    Bitmap resultBitmap = new Bitmap(maskBitmap.Width, maskBitmap.Width);

                    Graphics desktopGraphics = Graphics.FromHwnd(GetDesktopWindow());
                    IntPtr desktopHdc = desktopGraphics.GetHdc();

                    IntPtr maskHdc = CreateCompatibleDC(desktopHdc);
                    IntPtr oldPtr = SelectObject(maskHdc, maskBitmap.GetHbitmap());

                    using (Graphics resultGraphics = Graphics.FromImage(resultBitmap))
                    {
                        IntPtr resultHdc = resultGraphics.GetHdc();

                        // These two operation will result in a black cursor over a white background.
                        // Later in the code, a call to MakeTransparent() will get rid of the white background.
                        BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 32, TernaryRasterOperations_SRCCOPY);
                        BitBlt(resultHdc, 0, 0, 32, 32, maskHdc, 0, 0, TernaryRasterOperations_SRCINVERT);


                        resultGraphics.ReleaseHdc(resultHdc);
                    }

                    IntPtr newPtr = SelectObject(maskHdc, oldPtr);
                    DeleteObject(newPtr);
                    DeleteDC(maskHdc);
                    desktopGraphics.ReleaseHdc(desktopHdc);

                    // Remove the white background from the BitBlt calls,
                    // resulting in a black cursor over a transparent background.
                    resultBitmap.MakeTransparent(Color.White);
                    return resultBitmap;
                }
            }

            Icon icon = Icon.FromHandle(hicon);
            return icon.ToBitmap();
        }


    }
}
