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
        bool bFile_Transfer_In_Progress;
        Int32 File_Transfer_Size;
        String File_Transfer_Name;
        BinaryWriter File_Transfer_Writer;
        String Downloads_Folder;
        bool[] File_Transfer_Blocks;

        const bool bFILE_TRANSFER_LOGGING = true;


        private void ReceiveFileInfo()
        {
            byte[] b_int16 = new byte[2];
            byte[] b_int32 = new byte[4];
            ReadData(ref b_int16);
            if (b_int16[0] == 0)
            {
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("ReceiveFileInfo()");

                int FileNameLength = b_int16[1];            // Set Length (up to 255)
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("File Name Length: " + FileNameLength.ToString());

                byte[] bFileName = new byte[FileNameLength];    // create array for file name
                ReadData(ref bFileName);                        // read file name
                String FileName = Encoding.ASCII.GetString(bFileName);  // Convery byte array to string
                if(bFILE_TRANSFER_LOGGING) textBox_Log_Update("Filename: " + FileName);
                ReadData(ref b_int32);                          // read file length
                int FileLength = BitConverter.ToInt32(b_int32, 0);
                if(bFILE_TRANSFER_LOGGING) textBox_Log_Update("File Length: " + FileLength.ToString());

                try
                {
                    File_Transfer_Writer = new BinaryWriter(File.Open(Downloads_Folder + "\\" + FileName, FileMode.Create));
                }
                catch (Exception ex)
                {
                    bFile_Transfer_In_Progress = false;
                    EnqueueMessage(0, CANT_CREATE_FILE);
                    MessageBox.Show(ex.Message, "Cean't start file transfer");
                    return;

                }

                File_Transfer_Size = FileLength;
                File_Transfer_Name = FileName;

                if ((FileLength % 1024) > 0)
                {
                    File_Transfer_Blocks = new bool[(FileLength / 1024) + 1];
                }
                else {
                    File_Transfer_Blocks = new bool[(FileLength / 1024)];
                }
                for (int t = 0; t < File_Transfer_Blocks.Length; t++)
                    File_Transfer_Blocks[t] = false;

                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update(String.Format("File_Transfer_Blocks={0}", File_Transfer_Blocks));
                EnqueueMessage(0, START_FILE_TRANSFER);


                bFile_Transfer_In_Progress = true;
                if(bFILE_TRANSFER_LOGGING) textBox_Log_Update("File Opened");
            }
        }

        void LastFileBlockSent()
        {
            if (bFile_Transfer_In_Progress)
            {
                for (Int32 counter = 0; counter < File_Transfer_Blocks.Length; counter++)
                {
                    if (File_Transfer_Blocks[counter] == false)
                    {
                        EnqueueMessage(0, FILE_MISSING_BLOCK);
                        if (bFILE_TRANSFER_LOGGING) textBox_Log_Update(String.Format("Resend={0}", counter));

                        byte[] b_int32 = BitConverter.GetBytes(counter);
                        Sender_Queue.Enqueue(b_int32);
                        return;

                    }

                }
                EnqueueMessage(0, FILE_TRANSFER_DONE);
                File_Transfer_Writer.Close();
                bFile_Transfer_In_Progress = false;
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("File transfer completed");

            }
        }
        private void ReceiveFileBlock()
        {
            byte[] b_int32 = new byte[4];
            byte[] b_int16 = new byte[2];

            if (bFile_Transfer_In_Progress)
            {
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("ReceiveFileBlock()");

                ReadData(ref b_int32);
                Int32 block = BitConverter.ToInt32(b_int32, 0);
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update(String.Format("block={0}", block));

                ReadData(ref b_int32);
                Int16 blocksize = BitConverter.ToInt16(b_int32, 0);
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update(String.Format("blocksize={0}", blocksize));


                byte[] buffer = new byte[blocksize];
                ReadData(ref buffer);



                try
                {
                    File_Transfer_Writer.Seek(block * 1024, SeekOrigin.Begin);
                    File_Transfer_Writer.Write(buffer);
                    File_Transfer_Blocks[block] = true;
                }
                catch (Exception ex)
                {
                    EnqueueMessage(0, STOP_FILE_TRANSFER);
                    MessageBox.Show(ex.Message, "Error writing file");
                    return;
                }

                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("Sent Ack Block: "+ block.ToString());


                List<Byte> BufferList = new List<Byte>();
                Byte[] message = new byte[2];
                message[0] = 0;
                message[1] = FILE_TRANSFER_ACK_BLOCK;
                BufferList.AddRange(message);
                b_int32 = BitConverter.GetBytes(block);
                BufferList.AddRange(b_int32);
                Sender_Queue.Enqueue(BufferList.ToArray());
                BufferList.Clear();




                //EnqueueMessage(0, FILE_TRANSFER_ACK_BLOCK);
            }
            else
            {
                if (bFILE_TRANSFER_LOGGING) textBox_Log_Update("No File transfer in progress, sending stop");
                EnqueueMessage(0, STOP_FILE_TRANSFER);
            }
        }


    }
}
