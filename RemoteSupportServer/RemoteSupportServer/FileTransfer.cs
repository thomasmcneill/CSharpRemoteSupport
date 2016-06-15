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
        struct _FileTransfer
        {
            public bool bTransfer_In_Progress;
            public Int32 Size;
            public BinaryReader Reader;
            public String FileName;
            public Int32 Blocks;
            public bool[] ACKList;
            public Int32 LastBlockSent;
            public Int32 Block_Size;
        }
        _FileTransfer FileTransfer;

        const bool bFILE_TRANSFER_LOGGING = true;

        const Int32 Default_Block_Size = 4096;


        void Toolbar_SendFileButton_Click(object sender, System.EventArgs e)
        {
            List<Byte> BufferList = new List<Byte>();

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = false;

            DialogResult result = ofd.ShowDialog();
            if (result == DialogResult.OK)
            {
                //FileTransfer.bTransfer_In_Progress = true;
                FileTransfer.FileName = Path.GetFileName(ofd.FileName);
                FileInfo fi = new FileInfo(ofd.FileName);

                FileTransfer.Block_Size = Default_Block_Size;
                FileTransfer.Size = (Int32)fi.Length;

                if ((FileTransfer.Size % FileTransfer.Block_Size) > 0)
                {
                    FileTransfer.Blocks = (FileTransfer.Size / FileTransfer.Block_Size) + 1;
                }
                else {
                    FileTransfer.Blocks = (FileTransfer.Size / FileTransfer.Block_Size);
                }
                FileTransfer.ACKList = new bool[FileTransfer.Blocks];
                for (int t = 0; t < FileTransfer.ACKList.Length; t++)
                    FileTransfer.ACKList[t] = false;

                FileTransfer.LastBlockSent = -1;

                FileTransfer.Reader = new BinaryReader(File.Open(ofd.FileName, FileMode.Open));

                byte[] b_int32 = new byte[4];
                byte[] b_int16 = new byte[2];
                byte[] command = new byte[2];

                command[0] = 0;
                command[1] = START_FILE;
                BufferList.AddRange(command);

                byte[] b_filename = ASCIIEncoding.ASCII.GetBytes(FileTransfer.FileName);

                b_int16[0] = 0;
                b_int16[1] = (byte)b_filename.Length;
                BufferList.AddRange(b_int16);
                BufferList.AddRange(b_filename);

                b_int32 = BitConverter.GetBytes(FileTransfer.Size);
                BufferList.AddRange(b_int32);

                SendData(ref BufferList);
                if(bFILE_TRANSFER_LOGGING)
                {
                    myLogView.Append("FileName: " + FileTransfer.FileName);
                    myLogView.Append("File name Length: " + FileTransfer.FileName.Length.ToString());
                    myLogView.Append("File Length: " + FileTransfer.Size.ToString());
                    myLogView.Append("File Blocks: " + FileTransfer.Blocks);

                }

            }
        }


        void FileTransferAckBlock()
        {
            byte[] b_int32 = new byte[4];
            ReadData(ref b_int32);
            int block = BitConverter.ToInt32(b_int32, 0);
            if (block < FileTransfer.ACKList.Length)
                FileTransfer.ACKList[block] = true;


            SendNextFileBlock();

            if (bFILE_TRANSFER_LOGGING)
            {
                myLogView.Append("FileTransferAckBlock: " + block.ToString());
            }
        }

        void FileTransferDone()
        {
            FileTransfer.bTransfer_In_Progress = false;
            FileTransfer.Reader.Close();
            if (bFILE_TRANSFER_LOGGING)
            {
                myLogView.Append("FileTransferDone()");
            }

        }
        void FileMissingBlock()
        {
            byte[] b_int32 = new byte[4];
            ReadData(ref b_int32);
            Int32 BlockToSend = BitConverter.ToInt32(b_int32, 0);
            if (bFILE_TRANSFER_LOGGING)
            {
                myLogView.Append("FileMissingBlock: " + BlockToSend.ToString());
            }

            if (BlockToSend < FileTransfer.Blocks)
            {
                int TotalBytesAfterTransfer = BlockToSend * FileTransfer.Block_Size;
                int BlockSize = FileTransfer.Block_Size;
                if (TotalBytesAfterTransfer > FileTransfer.Size)
                {
                    BlockSize = FileTransfer.Size - (FileTransfer.LastBlockSent * FileTransfer.Block_Size);
                }

                List<Byte> BufferList = new List<Byte>();

                byte[] b_int16 = new byte[2];
                byte[] command = new byte[2];
                command[0] = 0;
                command[1] = RECEIVE_FILE_BLOCK;
                BufferList.AddRange(command);

                b_int32 = BitConverter.GetBytes(BlockToSend);
                BufferList.AddRange(b_int32);

                b_int16 = BitConverter.GetBytes(BlockSize);
                BufferList.AddRange(b_int16);

                byte[] buffer = new byte[BlockSize];
                FileTransfer.Reader.BaseStream.Seek(BlockToSend * FileTransfer.Block_Size, SeekOrigin.Begin);
                buffer = FileTransfer.Reader.ReadBytes(BlockSize);
                BufferList.AddRange(buffer);

                command[0] = 0;
                command[1] = LAST_FILE_BLOCK_SENT;
                BufferList.AddRange(command);
                SendData(ref BufferList);

            }



        }
        void SendNextFileBlock()
        {
            Int32 BlockToSend = FileTransfer.LastBlockSent + 1;
            if (BlockToSend <= FileTransfer.Blocks)
            {
                Int32 TotalBytesAfterTransfer = BlockToSend * FileTransfer.Block_Size;
                if (bFILE_TRANSFER_LOGGING)
                {
                    myLogView.Append("TotalBytesAfterTransfer="+ TotalBytesAfterTransfer.ToString());
                }

                Int32 BlockSize = FileTransfer.Block_Size;
                bool LastBlock = false;
                if (TotalBytesAfterTransfer > FileTransfer.Size)
                {
                    BlockSize = FileTransfer.Size - (FileTransfer.LastBlockSent * FileTransfer.Block_Size);
                    LastBlock = true;

                    if (bFILE_TRANSFER_LOGGING)
                    {
                        myLogView.Append("TotalBytesAfterTransfer > Size, BlockSize=" + BlockSize.ToString());
                    }

                }
                if (TotalBytesAfterTransfer == FileTransfer.Size)
                {
                    LastBlock = true;
                    if (bFILE_TRANSFER_LOGGING)
                    {
                        myLogView.Append("TotalBytesAfterTransfer == Size");
                    }

                }

                List<Byte> BufferList = new List<Byte>();

                byte[] b_int32 = new byte[4];
                //byte[] b_int16 = new byte[2];
                byte[] command = new byte[2];
                command[0] = 0;
                command[1] = RECEIVE_FILE_BLOCK;
                BufferList.AddRange(command);

                b_int32 = BitConverter.GetBytes(BlockToSend);
                BufferList.AddRange(b_int32);

                b_int32 = BitConverter.GetBytes(BlockSize);
                BufferList.AddRange(b_int32);

                byte[] buffer = new byte[BlockSize];
                buffer = FileTransfer.Reader.ReadBytes(BlockSize);
                BufferList.AddRange(buffer);

                if (bFILE_TRANSFER_LOGGING)
                {
                    myLogView.Append("Sent Block: " + BlockToSend.ToString());
                }

                FileTransfer.LastBlockSent++;


                if (LastBlock)
                {
                    command[0] = 0;
                    command[1] = LAST_FILE_BLOCK_SENT;
                    BufferList.AddRange(command);

                    if (bFILE_TRANSFER_LOGGING)
                    {
                        myLogView.Append("SendNextFileBlock: LastBlock");
                    }

                }
                SendData(ref BufferList);

            }
        }

    }
}
