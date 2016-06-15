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

        Thread thread_Sender;
        private volatile bool thread_Stop;
        ConcurrentQueue<byte[]> Sender_Queue;

        Thread thread_Reader;



        TcpClient tcpConnection;
        Socket tcpSocket;



        private void Sender_Thread()
        {
            do
            {
                try
                {

                    if (Sender_Queue.Count > 0)          // Is there any in the queue?
                        Sender_SendData();
                    //Thread.Sleep(5);
                }
                catch (Exception ex)
                {

                }
            } while (!thread_Stop);

        }

        void Sender_SendData()
        {
            byte[] buffer;
            try
            {
                if (Sender_Queue.TryDequeue(out buffer))
                {
                    Int32 count = 0;
                    while (count < buffer.Length)
                    {
                        count += tcpSocket.Send(buffer, count, buffer.Length - count, SocketFlags.None);
                    }
                }
            }
            catch (Exception e)
            {
            }
        }




        private void ReadData(ref byte[] buffer)
        {
            try
            {
                Int32 count = 0;
                while (count < buffer.Length)
                {
                    count += tcpSocket.Receive(buffer, count, buffer.Length - count, SocketFlags.None);
                    //                    if (count < buffer.Length)
                    //                        Thread.Sleep(5);
                }

            }
            catch (Exception e)
            {


            }
        }

        void Connect()
        {

            try
            {
                tcpConnection = new TcpClient();




                tcpConnection.Connect("10.10.0.99", 8001);
                //tcpConnection.Connect("192.168.1.123", 8001);
                tcpSocket = tcpConnection.Client;


                // Disable the Nagle Algorithm for this tcp socket.
                tcpSocket.NoDelay = true;

                // Set the receive buffer size to 8k
                tcpSocket.ReceiveBufferSize = 65536;

                // Set the timeout for synchronous receive methods to 
                // 1 second (1000 milliseconds.)
                tcpSocket.ReceiveTimeout = 2000;

                // Set the send buffer size to 8k.
                tcpSocket.SendBufferSize = 65536;

                // Set the timeout for synchronous send methods
                // to 1 second (1000 milliseconds.)			
                tcpSocket.SendTimeout = 2000;


                SendFullScreen();

                thread_Reader = new Thread(new ThreadStart(Thread_MessageProcessor));
                thread_Stop = false;
                thread_Reader.Start();

                thread_Sender = new Thread(new ThreadStart(Sender_Thread));
                thread_Stop = false;
                thread_Sender.Start();


            }
            catch (Exception e)
            {
                MessageBox.Show("Connect:  " + e.Message);
                button_Start.Enabled = true;

            }
        }



    }
}
