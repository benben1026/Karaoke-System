using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Security.Cryptography;

namespace P2P_Karaoke_System
{
    public class Peer
    {
        private static int segmentSize = 204800;
        // chunk size in bytes
        private FileStream fs;

        public Peer()
        {

        }

        public static string ConvertHashValue(byte[] hashvalue)
        {
            var sb = new StringBuilder("");
            foreach (var b in hashvalue)
            {
                sb.Append(b.ToString("x2"));
                // we can change this format accordingly
            }
            return sb.ToString();
        }

        public void ProcessGetRequest(byte[] obj, Socket s)
        {
            GetRequest greq = (GetRequest)GetRequest.toObject(obj);
            string filename = greq.GetFilename();
            string md5 = greq.GetMd5();
            int startByte = greq.GetStartByte();
            int endByte = greq.GetEndByte();

            //FileStream fs;
            string hash = "";
            try {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                MD5 myMD5 = MD5.Create();
                byte[] hashvalue = myMD5.ComputeHash(fs);
                hash = Peer.ConvertHashValue(hashvalue);
            }
            catch (FileNotFoundException ex)
            {
                GetResponse gres = new GetResponse(filename, hash);
                gres.SetStatus(2);
                gres.SetMsg("File Modified");
                byte[] serialize = gres.ToByte();
                byte[] type = { 0x12 };
                byte[] size = BitConverter.GetBytes(serialize.Length);
                byte[] response = new byte[5 + serialize.Length];
                Buffer.BlockCopy(type, 0, response, 0, 1);
                Buffer.BlockCopy(size, 0, response, 1, 4);
                Buffer.BlockCopy(serialize, 0, response, 5, serialize.Length);
                s.Send(response);
                return;
            }
            fs.Close();
            fs.Dispose();

            if ((String.Compare(hash, greq.GetMd5(), true) != 0) && (filename.IndexOf(".ppm", StringComparison.OrdinalIgnoreCase) <= -1))
            {
                GetResponse gres = new GetResponse(filename, hash);
                gres.SetStatus(2);
                gres.SetMsg("File Modified");
                byte[] serialize = gres.ToByte();
                byte[] type = { 0x12 };
                byte[] size = BitConverter.GetBytes(serialize.Length);
                byte[] response = new byte[5 + serialize.Length];
                Buffer.BlockCopy(type, 0, response, 0, 1);
                Buffer.BlockCopy(size, 0, response, 1, 4);
                Buffer.BlockCopy(serialize, 0, response, 5, serialize.Length);
                s.Send(response);
                return;
            }

            FileInfo f = new FileInfo(filename);
            int filesize = (int)f.Length;

             
            if (filename.IndexOf(".ppm", StringComparison.OrdinalIgnoreCase) <= -1) {
                NAudio.Wave.AudioFileReader audioStream = new NAudio.Wave.AudioFileReader(filename);
                for (int i = startByte; i < endByte; i += segmentSize)
                {
                    Console.WriteLine("Transmit no{0} packet", i);
                    GetResponse gres = new GetResponse(filename, hash);
                    if (i + segmentSize >= endByte)
                    {
                        gres.GetData(audioStream, md5, i, endByte);
                    }
                    else
                    {
                        gres.GetData(audioStream, md5, i, i + segmentSize - 1);
                    }
                    byte[] serialize = gres.ToByte();
                    byte[] type = { 0x12 };
                    byte[] size = BitConverter.GetBytes(serialize.Length);
                    byte[] response = new byte[5 + serialize.Length];
                    Buffer.BlockCopy(type, 0, response, 0, 1);
                    Buffer.BlockCopy(size, 0, response, 1, 4);
                    Buffer.BlockCopy(serialize, 0, response, 5, serialize.Length);
                    s.Send(response);
                }
                audioStream.Close();
            }
            else
            {
                FileStream audioStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                for (int i = startByte; i < endByte; i += segmentSize)
                {
                    Console.WriteLine("Transmit no{0} packet", i);
                    GetResponse gres = new GetResponse(filename, hash);
                    if (i + segmentSize >= endByte)
                    {
                        gres.GetData(audioStream, md5, i, endByte);
                    }
                    else
                    {
                        gres.GetData(audioStream, md5, i, i + segmentSize - 1);
                    }
                    byte[] serialize = gres.ToByte();
                    byte[] type = { 0x12 };
                    byte[] size = BitConverter.GetBytes(serialize.Length);
                    byte[] response = new byte[5 + serialize.Length];
                    Buffer.BlockCopy(type, 0, response, 0, 1);
                    Buffer.BlockCopy(size, 0, response, 1, 4);
                    Buffer.BlockCopy(serialize, 0, response, 5, serialize.Length);
                    s.Send(response);
                }
                audioStream.Close();
            }
            
        }

        public void ProcessSearchRequest(byte[] obj, Socket s, List<MusicCopy> musicDataList)
        {
            SearchRequest sreq = (SearchRequest)SearchRequest.ToObject(obj);
            string keyword = sreq.GetKeyword();
            List<MusicCopy> searchResult = MusicSearchUtil.SearchedMusicList(keyword, musicDataList);
            
            // construct search response
            SearchResponse sres = new SearchResponse(searchResult);
            byte[] serialize = sres.ToByte();
            byte[] type = { 0x11 };
            byte[] size = BitConverter.GetBytes(serialize.Length);
            byte[] response = new byte[5 + serialize.Length];
            Buffer.BlockCopy(type, 0, response, 0, 1);
            Buffer.BlockCopy(size, 0, response, 1, 4);
            Buffer.BlockCopy(serialize, 0, response, 5, serialize.Length);
            s.Send(response);
        }

        public void StartListening()
        {
            //byte[] bytes = new Byte[1024];

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 3280);

            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            
            listener.Bind(localEndPoint);
            listener.Listen(10);

            while (true)
            {
                try { 
                    Console.WriteLine("waiting for connection...");
                    //program is suspended
                    Socket handler = listener.Accept();
                    Console.WriteLine("Connected.");

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
                    Console.WriteLine("type = {0}, size = {1}, realSize = {2}", type, payloadSize, byteReceived.Length);
                    if (type == 0x01)
                    {
                        ProcessSearchRequest(byteReceived, handler, MainWindow.musicDataList);
                    }
                    else if (type == 0x02)
                    {
                        ProcessGetRequest(byteReceived, handler);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Socket Error:{0}", e.ToString());
                    try
                    {
                        fs.Close();
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
            
        }

        /*
        public static int Main(String[] args){
            Thread t = new Thread(new ThreadStart(StartListening));
            t.Start();
            
            
            Thread.Sleep(1);
            //for (int i = 0; i < 100000000; i++)
            //{
            //    if (i % 10000000 == 0)
            //    {
            //        Console.WriteLine(i);
            //    }
            //}
                //startListening();
            Console.Read();
            return 0;
        }
        */
    }
}
