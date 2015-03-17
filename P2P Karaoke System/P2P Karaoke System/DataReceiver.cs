using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace P2P_Karaoke_System
{
    class DataReceiver
    {
        public string rawIP;
        public int port;

        private Socket handler;

        //0 =>start 1=>socket connected 2=>request sent 3=>startr eciving 4=>succeed 5=>error fixed
        public int status;
        public int fromByte;
        public int toByte;
        public int currentByte;
        public byte[] fileData;

        public string filepath;
        public string hashValue;
        public int filesize;

        private MusicStream ms;

        public DataReceiver(string ip, int port, int fromByte, int toByte, string filepath, string hashValue, int filesize, byte[] fileData, MusicStream ms)
        {
            this.rawIP = ip;
            this.port = port;

            this.fromByte = fromByte;
            this.toByte = toByte;
            this.filepath = filepath;
            this.hashValue = hashValue;
            this.filesize = filesize;
            this.fileData = fileData;

            this.status = 0;
            this.currentByte = fromByte;
            this.ms = ms;
        }

        private void ConnectSocket()
        {
            Console.WriteLine("Connecting to {0}", this.rawIP);
            IPAddress ip = IPAddress.Parse(this.rawIP);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            handler = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //Console.WriteLine("4");
            handler.Connect(ipe);
            Console.WriteLine("Socket connected to {0}", this.rawIP);
            if (handler.Connected)
            {
                status = 1;
            }
            else
            {
                status = -1;
            }
        }

        private byte[] ConstructGetRequest()
        {
            GetRequest gres = new GetRequest(this.filepath, this.hashValue, this.fromByte, this.toByte);
            byte[] obj = gres.ToByte();
            byte[] type = { 0x02 };
            byte[] size = BitConverter.GetBytes(obj.Length);
            byte[] request = new byte[5 + obj.Length];
            Buffer.BlockCopy(type, 0, request, 0, 1);
            Buffer.BlockCopy(size, 0, request, 1, 4);
            Buffer.BlockCopy(obj, 0, request, 5, obj.Length);
            return request;
        }

        private int ProcessGetResponse(byte[] obj)
        {
            GetResponse gres = (GetResponse)GetResponse.toObject(obj);
            if (gres.GetStatus() != 1)
            {
                Console.WriteLine("Error occured when getting file data: {0}", gres.GetMsg());
                this.status = -3;
                return -1;
            }
            else if (String.Compare(this.hashValue, gres.GetMd5(), true) != 0)
            {
                Console.WriteLine("File Modified");
                this.status = -3;
                return -1;
            }
            if (gres.CopyData(this.fileData, this.ms))
            {
                this.currentByte = gres.GetEndByte();
                Console.WriteLine("{0}:Copy from {1} to {2}", this.rawIP, gres.GetStartByte(), gres.GetEndByte());
                Console.WriteLine("current = {0}, toByte = {1}", this.currentByte, this.toByte);
                if (this.currentByte == this.toByte)
                {
                    return 1;
                }
                return 0;
            }
            else
            {
                Console.WriteLine("Error occured when copying from {0} to {1}", gres.GetStartByte(), gres.GetEndByte());
                this.status = -3;
                return -1;
            }

        }

        public void start()
        {
            try
            {
                this.ConnectSocket();
            }
            catch (Exception e)
            {
                Console.WriteLine("Fail to connect to {0}", this.rawIP);
                this.status = -1;
                return;
            }
            this.status = 1;
            byte[] request = this.ConstructGetRequest();
            try {
                handler.Send(request, request.Length, 0);
            }
            catch (Exception e)
            {
                this.status = -2;
                return;
            }
            this.status = 2;
            try
            {
                this.status = 3;
                while (true)
                {
                    int bytes = 0;
                    byte[] byteReceived = new byte[5];
                    for (int remain = 5; remain > 0; remain -= bytes)
                    {
                        bytes = handler.Receive(byteReceived, 5 - remain, remain, 0);
                    }
                    int payloadSize = BitConverter.ToInt32(byteReceived, 1);
                    byte type = byteReceived[0];
                    byteReceived = new byte[payloadSize];
                    for (int remain = payloadSize; remain > 0; remain -= bytes)
                    {
                        bytes = handler.Receive(byteReceived, payloadSize - remain, remain, 0);
                    }
                    //Console.WriteLine("type = {0}, size = {1}, realSize = {2}", type, payloadSize, byteReceived.Length);
                    if (type == 0x11)
                    {

                    }
                    else if (type == 0x12)
                    {
                        int t = this.ProcessGetResponse(byteReceived);
                        Console.WriteLine("return value = {0}", t);
                        if (t == 1)
                        {
                            break;
                        }
                        else if (t == -1)
                        {
                            this.status = -3;
                            break;
                        }
                    }
                }
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                this.status = -3;
                Console.WriteLine(e.ToString());
                return;
            }
            this.status = 4;
        }
    }
}
