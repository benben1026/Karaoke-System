using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace P2P_Karaoke_System
{
    class Local
    {
        private static int port = 3280;
        private int peerNum;
        private string[] ipList = null;

        private List<MusicCopy>[] searchResult;

        private int sizePP;
        private bool ifError = false;
        private byte[] fileData = null;
        private int segmentSize = 4096;
        private int[] flag = null;
        //private int[][] dataReceived = null;
        private bool ifGettingData = false;
        private MusicCopy musicDownload = null;

        public Local(string[] ipList)
        {
            this.ipList = ipList;
            this.peerNum = ipList.Count();
            this.searchResult = new List<MusicCopy>[peerNum];
        }

        public void UpdateIpList(string[] newIpList)
        {
            this.ipList = newIpList;
        }

        private Socket ConnectSocket(string serverIP)
        {
            IPAddress ip = IPAddress.Parse(serverIP);
            IPEndPoint ipe = new IPEndPoint(ip, port);
            Socket s = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            Console.WriteLine("4");
            s.Connect(ipe);
            Console.WriteLine("Socket connected");
            if (s.Connected)
            {
                return s;
            }
            else
            {
                return null;
            }
        }
        /*
        private void SendRequest(string ip, byte[] bytesSent)
        {
            //string request = "TEST REQUEST FROM BENJAMIN<EOR>";
            //Byte[] bytesSent = Encoding.UTF8.GetBytes(request);
            Byte[] bytesReceived = new Byte[256];

            Console.WriteLine("Connecting to {0}", ip);
            Socket s = ConnectSocket(ip);

            if (s == null)
            {
                Console.WriteLine("Connection Failed");
                return;
            }

            Console.WriteLine("Connection success");
            s.Send(bytesSent, bytesSent.Length, 0);

            int bytes = 0;

            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                Console.WriteLine(Encoding.UTF8.GetString(bytesReceived, 0, bytes));
                if (Encoding.UTF8.GetString(bytesReceived).IndexOf("<END>") > -1)
                {
                    break;
                }
            } while (bytes > 0);
            Console.Read();
        }
        */

        private byte[] ConstructSearchRequest(string keyword)
        {
            //string request = "SEARCH&" + keyword + "&<EOR>";
            //return Encoding.UTF8.GetBytes(request);

            SearchRequest sres = new SearchRequest(keyword);
            byte[] obj = sres.ToByte();
            byte[] type = { 0x01 };
            byte[] size = BitConverter.GetBytes(obj.Length);
            byte[] request = new byte[5 + obj.Length];
            Buffer.BlockCopy(type, 0, request, 0, 1);
            Buffer.BlockCopy(size, 0, request, 1, 4);
            Buffer.BlockCopy(obj, 0, request, 5, obj.Length);
            return request;
        }

        private byte[] ConstructGetRequest(int copyInfoIndex, int startByte, int endByte)
        {
            //string request = "GET&" + filename + "&" + md5 + "&" + segmentId + "&<EOR>";
            //return Encoding.UTF8.GetBytes(request);
            
            GetRequest gres = new GetRequest(musicDownload.CopyInfo[copyInfoIndex].FileName, musicDownload.AudioData.HashValue, startByte, endByte);
            byte[] obj = gres.ToByte();
            byte[] type = {0x02};
            byte[] size = BitConverter.GetBytes(obj.Length);
            byte[] request = new byte[5 + obj.Length];
            Buffer.BlockCopy(type, 0, request, 0, 1);
            Buffer.BlockCopy(size, 0, request, 1, 4);
            Buffer.BlockCopy(obj, 0, request, 5, obj.Length);
            return request;

        }

        public List<MusicCopy> StartSearch(string keyword)
        {
            string request = "SEARCH&" + keyword + "&<EOR>";
            byte[] byteRequest = Encoding.UTF8.GetBytes(request);
            Thread[] threadList = new Thread[ipList.Count()];
            Console.WriteLine("length = {0}", ipList.Count());
            Console.WriteLine("length = {0}", threadList.Count());
            for (int i = 0; i < ipList.Count(); i++)
            {
                if (String.IsNullOrEmpty(ipList[i]))
                {
                    continue;
                }
                Console.WriteLine(i);
                threadList[i] = new Thread(() => this.SearchThread(i, byteRequest));
                threadList[i].Start();
                Thread.Sleep(1);
            }
            for (int i = 0; i < ipList.Count(); i++)
            {
                threadList[i].Join();
            }
            return MergeMusicList(searchResult);
        }

        private void SearchThread(int userIndex, byte[] bytesSent)
        {
            Console.WriteLine("Connecting to {0}", ipList[userIndex]);
            Socket s = this.ConnectSocket(ipList[userIndex]);
            if (s == null)
            {
                Console.WriteLine("{0}:Connection Failed", ipList[userIndex]);
                return;
            }
            Console.WriteLine("{0}:Connection success", ipList[userIndex]);
            s.Send(bytesSent, bytesSent.Length, 0);

            
            while(true)
            {
                int bytes = 0;
                byte[] byteReceived = new byte[5];
                for (int remain = 5; remain > 0; remain -= bytes)
                {
                    bytes = s.Receive(byteReceived, 5 - remain, remain, 0);
                }
                int payloadSize = BitConverter.ToInt32(byteReceived, 1);
                byte type = byteReceived[0];
                byteReceived = new byte[payloadSize];
                for (int remain = payloadSize; remain > 0; remain -= bytes)
                {
                    bytes = s.Receive(byteReceived, payloadSize - remain, remain, 0);
                }

                if (type == 0x11)
                {
                    searchResult[userIndex] = ProcessSearchResponse(byteReceived, userIndex);
                    if (searchResult[userIndex] == null)
                    {
                        ifError = true; 
                    }
                    break;
                }
                else if (type == 0x12)
                {
                    
                }
            }

            s.Shutdown(SocketShutdown.Both);
            s.Close();
        }

        public void StartGetMusic(MusicCopy music)
        {
            if (ifGettingData)
            {
                return;
            }
            if (music.AudioData.Size >= Int32.MaxValue)
            {
                Console.WriteLine("File Too Large");
                return;
            }
            ifGettingData = true;
            this.musicDownload = music;
            int numOfPeerAvailable = music.CopyInfo.Count();
            Thread[] threadList = new Thread[numOfPeerAvailable];
            this.fileData = new byte[(int)music.AudioData.Size];
            this.sizePP = ((int)music.AudioData.Size - 1) / numOfPeerAvailable + 1; // celling
            flag = new int[numOfPeerAvailable];
            //dataReceived = new int[numOfPeerAvailable][];
            for (int i = 0; i < numOfPeerAvailable; i++)
            {
                flag[i] = i * sizePP;
            }
            for (int i = 0; i < numOfPeerAvailable; i++)
            {
                if (i == numOfPeerAvailable - 1)
                {
                    threadList[i] = new Thread(() => this.GetMusicThread(music.CopyInfo[i].UserIndex, i, i * sizePP, (int)music.AudioData.Size));

                }
                else
                {
                    threadList[i] = new Thread(() => this.GetMusicThread(music.CopyInfo[i].UserIndex, i, i * sizePP, (i + 1) * sizePP - 1));

                }
                threadList[i].Start();
                Thread.Sleep(1);
            }

            for (int i = 0; i < numOfPeerAvailable; i++)
            {
                threadList[i].Join(10000);
            }
            while (this.ifError)
            {

            }
            FileStream fs = new FileStream(music.AudioData.MediaPath, FileMode.Create);
            fs.Write(fileData, 0, (int)music.AudioData.Size);
            fs.Close();
            Console.WriteLine("succeed");
            ifGettingData = false;
        }

        private void GetMusicThread(int index, int threadIndex, int startByte, int endByte)
        {
            int numOfSeg = (endByte - startByte) / segmentSize + 1; //celling
            //dataReceived[threadIndex] = new int[numOfSeg];
            //for (int i = 0; i < numOfSeg; i++)
            //{
            //    dataReceived[threadIndex][i] = 0;
            //}
            Console.WriteLine("Connecting to {0}", this.ipList[index]);
            Socket s = this.ConnectSocket(ipList[index]);
            if (s == null)
            {
                Console.WriteLine("{0}:Connection Failed", this.ipList[index]);
                return;
            }
            Console.WriteLine("{0}:Connection success", this.ipList[index]);
            //Console.WriteLine("segmentSize = {0}, filename = {1}, segNum = {2}, packetLeft = {3}", segmentSize, musicDownload.Filename, flag.Count(), packetLeft);
            
            byte[] request = this.ConstructGetRequest(threadIndex, startByte, endByte);
            s.Send(request, request.Length, 0);

            try { 
                while (true)
                {
                    int bytes = 0;
                    byte[] byteReceived = new byte[5];
                    for (int remain = 5; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(byteReceived, 5 - remain, remain, 0);
                    }
                    int payloadSize = BitConverter.ToInt32(byteReceived, 1);
                    byte type = byteReceived[0];
                    byteReceived = new byte[payloadSize];
                    for (int remain = payloadSize; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(byteReceived, payloadSize - remain, remain, 0);
                    }
                    Console.WriteLine("type = {0}, size = {1}, realSize = {2}", type, payloadSize, byteReceived.Length);
                    if (type == 0x11)
                    {

                    }
                    else if (type == 0x12)
                    {
                        int t = this.ProcessGetResponse(byteReceived, threadIndex);
                        Console.WriteLine("return value = {0}", t);
                        if (t == 1)
                        {
                            break;
                        }
                        else if (t == -1)
                        {
                            this.ifError = true;
                            break;
                        }
                    }
                }
                Console.WriteLine("try to close socket");
                s.Shutdown(SocketShutdown.Both);
                s.Close();
                Console.WriteLine("socket closed");
            }catch(Exception e){
                this.ifError = true;
                return;
            }
        }

        private int ProcessGetResponse(byte[] obj, int threadIndex)
        {
            GetResponse gres = (GetResponse)GetResponse.toObject(obj);
            if (gres.GetStatus() != 1)
            {
                Console.WriteLine("Error occured when getting file data: {0}", gres.GetMsg());
                return -1;
            }
            else if (String.Compare(musicDownload.AudioData.HashValue, gres.GetMd5(), true) != 0)
            {
                Console.WriteLine("File Modified");
                return -1;
            }
            if (gres.CopyData(this.fileData))
            {
                flag[threadIndex] = gres.GetEndByte();
                Console.WriteLine("Copy from {0} to {1}", gres.GetStartByte(), gres.GetEndByte());
                Console.WriteLine("flag = {0}, sizePP = {1}, datasize = {2}", flag[threadIndex], sizePP, musicDownload.AudioData.Size);
                if ((flag[threadIndex] + 1) % sizePP == 0 || flag[threadIndex] + 1 == musicDownload.AudioData.Size)
                {
                    return 1;
                }
                return 0;
            }
            else
            {
                Console.WriteLine("Error occured when copying from {0} to {1}", gres.GetStartByte(), gres.GetEndByte());
                return -1;
            }

        }

        private List<MusicCopy> ProcessSearchResponse(byte[] obj, int userIndex)
        {
            SearchResponse sres = (SearchResponse)SearchResponse.ToObject(obj);
            if (sres.GetStatus() != 1)
            {
                Console.WriteLine("Error occured when getting search results: {0}", sres.GetMsg());
                return null;
            }
            else
            {
                List<MusicCopy> searchResult =  sres.GetResult();
                int count = searchResult.Count();
                for (int i = 0; i < count; i++)
                {
                    CopyIndex adding = new CopyIndex(userIndex, searchResult[i].AudioData.MediaPath, ipList[userIndex]);
                    searchResult[i].CopyInfo.Add(adding);
                }
                return searchResult;
            }
        }

        private List<MusicCopy> MergeMusicList(List<MusicCopy>[] musicList)
        {
            int listItems = musicList.Length;
            List<MusicCopy> oldList = musicList[0];
            int oldItems = oldList.Count();

            for (int k = 1; k < listItems; k++)
            {
                List<MusicCopy> newList = musicList[k];

                int newItems = newList.Count();
                bool duplicate;

                for (int i = 0; i < newItems; i++)
                {
                    duplicate = false;
                    for (int j = 0; j < oldItems; j++)
                    {
                        if (newList[i].AudioData.HashValue == oldList[j].AudioData.HashValue && (String.Compare(newList[i].AudioData.Title, oldList[j].AudioData.Title, false) == 0))
                        {
                            duplicate = true;
                            oldList[j].CopyNumber++;
                            oldList[j].CopyInfo.Add(new CopyIndex(newList[i].CopyInfo[0].UserIndex, newList[i].AudioData.MediaPath, newList[i].CopyInfo[0].IPAddress));
                        }
                    }
                    if (!duplicate)
                    {
                        oldList.Add(newList[i]);
                    }
                }
            }

            return oldList;
        }

        public static void InitialIpList()
        {
            //ipList = new string[peerNum];
            //ipList[0] = "192.168.213.200";
            //ipList[1] = "192.168.211.197";
            //ipList[2] = "";
            //ipList[3] = "";
            //ipList[4] = "";
            //ipList[5] = "";
            //ipList[6] = "";
            //ipList[7] = "";
            //ipList[8] = "";
            //ipList[9] = "";

        }

        /*
        public static int Main(String[] args)
        {
            Thread sender1 = new Thread(() => SendRequest("192.168.173.1", ConstructSearchRequest("hello world")));
            Thread sender2 = new Thread(() => SendRequest("192.168.173.1", ConstructGetRequest("hello.mp3", "wiehfsiahfsuiaf", 19)));
            //Thread sender3 = new Thread(() => SendRequest("192.168.173.1"));

            sender1.Start();
            sender2.Start();
            //sender3.Start();
            Thread.Sleep(1);

            //for (int i = 0; i < 100000000; i++)
            //{
            //    if (i % 10000000 == 0)
            //    {
            //        Console.WriteLine(i);
            //    }
            //}

            //sendData();
            return 0;
        }
        */
    }
}
