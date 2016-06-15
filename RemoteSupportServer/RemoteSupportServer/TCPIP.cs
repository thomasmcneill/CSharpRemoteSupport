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
        public Socket _Socket;
        public BufferedStream _ReadStream;
        public BufferedStream _WriteStream;
        public Thread thread_Listener;

        ConcurrentQueue<byte[]> Sender_Queue;

        const bool bDEBUG_TCPIP_SEND = true;
        const bool bDEBUG_TCPIP_READ = false;

        Thread thread_Sender;


        void SendArray(ref byte[] buffer)
        {
            try
            {
                Sender_Queue.Enqueue(buffer);

            }
            catch (Exception e)
            {


            }

        }
        private void SendACK()
        {
            Byte[] ACK = new byte[2];
            ACK[0] = 0;
            ACK[1] = FRAME_ACK;
            SendArray(ref ACK);

        }
        void SendData(ref List<Byte> BufferList)
        {
            Byte[] Buffer = BufferList.ToArray();
            SendArray(ref Buffer);

        }
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
            } while (!StopThread);

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
                        count += _Socket.Send(buffer, count, buffer.Length - count, SocketFlags.None);
                    }
                    lock (TransferRate.TXBytes_Lock)
                    {
                        TransferRate.TXBytes += (UInt32)count;
                    }
                    if (bDEBUG_TCPIP_SEND)
                    {
                        if(count != 2 && buffer[1] != FRAME_ACK)
                            myLogView.Append(String.Format("SendData Message[{0}][{1}]: {2}",buffer[0],buffer[1],count));

                    }

                }
            }
            catch (Exception e)
            {
                myLogView.Append("SendData:" + e.Message);
            }
        }

        private void ReadData(ref byte[] buffer)
        {
            try
            {
                Int32 count = 0;
                while (count < buffer.Length)
                {
                    count += _Socket.Receive(buffer, count, buffer.Length - count, SocketFlags.None);
                    //                    if(count < buffer.Length)
                    //                        Thread.Sleep(5);

                }
                lock (TransferRate.RXBytes_Lock)
                {
                    TransferRate.RXBytes += (UInt32)count;
                }
                if (bDEBUG_TCPIP_READ) myLogView.Append("ReadData:" + count.ToString());

            }
            catch (Exception e)
            {
                myLogView.Append("ReadData:" + e.Message);

            }
        }

    }
}
