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
        private string keyword;

        private int sizePP;
        private bool ifError = false;
        private byte[] fileData = null;
        private MusicStream dataStream = null;
        private int segmentSize = 4096;
        private int[] flag = null;
        //private int[][] dataReceived = null;
        private bool ifGettingData = false;
        private MusicCopy musicDownload = null;

        public DataReceiver[] dataReceiverList;

        public Local(string[] ipListInput, string searchKeyword, int inputIPNumber)
        {
            this.ipList = ipListInput;
            this.peerNum = inputIPNumber;
            this.searchResult = new List<MusicCopy>[peerNum];
            this.keyword = searchKeyword;
        }

        public Local(string[] ipList, MusicCopy music)
        {
            this.ipList = ipList;
            this.peerNum = ipList.Count();
            this.searchResult = new List<MusicCopy>[peerNum];
            this.musicDownload = music;
            this.dataStream = new MusicStream((int)this.musicDownload.AudioData.Size);
        }

        public MusicStream GetMusicStream()
        {
            return this.dataStream;
        }

        public void UpdateIpList(string[] newIpList)
        {
            this.ipList = newIpList;
        }

        public Stream GetDataStream()
        {
            return this.dataStream;
        }

        private Socket ConnectSocket(string serverIP)
        {
            Console.WriteLine("serverIP is :" + serverIP);
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

        private byte[] ConstructSearchRequest()
        {
            //string request = "SEARCH&" + keyword + "&<EOR>";
            //return Encoding.UTF8.GetBytes(request);

            SearchRequest sres = new SearchRequest(this.keyword);
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

        public List<MusicCopy> StartSearch()
        {
            //string request = "SEARCH&" + keyword + "&<EOR>";
            //byte[] byteRequest = Encoding.UTF8.GetBytes(request);
            Array.Clear(searchResult,0,peerNum);
            Thread[] threadList = new Thread[peerNum];

            //Console.WriteLine("length = {0}", ipList.Count());
            //Console.WriteLine("length = {0}", threadList.Count());
            int count = 0;
            int userIndex = 0;
            int threadIndex = 0;
            for (int i = 0; i < this.ipList.Count(); i++)
            {
                if (String.IsNullOrEmpty(ipList[i]))
                {
                    continue;
                }

                userIndex = i;
                threadIndex = count;
                Console.WriteLine("abc: " + i + "  ipaddress: " + this.ipList[i]);
                threadList[count] = new Thread(() => this.SearchThread(userIndex, threadIndex));
                threadList[count].Start();
                count++;

                Console.WriteLine("aaaaaa: {0}, {1}", i, count);

                Thread.Sleep(1);
            }
            for (int i = 0; i < peerNum; i++)
            {
                threadList[i].Join();
            }
            return MergeMusicList(searchResult);
        }

        private void SearchThread(int userIndex, int threadIndex)
        {
            Console.WriteLine("Connecting to {0}", this.ipList[userIndex]);
            Socket s = this.ConnectSocket(ipList[userIndex]);
            if (s == null)
            {
                Console.WriteLine("{0}:Connection Failed", ipList[userIndex]);
                return;
            }
            Console.WriteLine("{0}:Connection success", ipList[userIndex]);

            byte[] request = this.ConstructSearchRequest();
            s.Send(request, request.Length, 0);

            try
            {
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

                    if (type == 0x11)
                    {
                        searchResult[threadIndex] = ProcessSearchResponse(byteReceived, userIndex);
                        if (searchResult[threadIndex] == null)
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
            catch (Exception e)
            {
                this.ifError = true;
                Console.WriteLine(e.Message);
                return;
            }
        }

        public void StartGetMusic()
        {
            if (ifGettingData)
            {
                return;
            }
            if (this.musicDownload.AudioData.Size >= Int32.MaxValue)
            {
                Console.WriteLine("File Too Large");
                return;
            }
            ifGettingData = true;
            int numOfPeerAvailable = this.musicDownload.CopyInfo.Count();
            Thread[] threadList = new Thread[numOfPeerAvailable];
            this.dataReceiverList = new DataReceiver[numOfPeerAvailable];
            this.fileData = new byte[(int)this.musicDownload.AudioData.Size];
            this.sizePP = ((int)this.musicDownload.AudioData.Size - 1) / numOfPeerAvailable + 1; // celling
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
                    dataReceiverList[i] = new DataReceiver(this.ipList[this.musicDownload.CopyInfo[i].UserIndex], port, i * sizePP, (int)this.musicDownload.AudioData.Size - 1, this.musicDownload.CopyInfo[i].FileName, this.musicDownload.AudioData.HashValue, (int)this.musicDownload.AudioData.Size, this.fileData, this.dataStream);

                    //threadList[i] = new Thread(() => this.GetMusicThread(music.CopyInfo[i].UserIndex, i, i * sizePP, (int)music.AudioData.Size));
                }
                else
                {
                    dataReceiverList[i] = new DataReceiver(this.ipList[this.musicDownload.CopyInfo[i].UserIndex], port, i * sizePP, (i + 1) * sizePP - 1, this.musicDownload.CopyInfo[i].FileName, this.musicDownload.AudioData.HashValue, (int)this.musicDownload.AudioData.Size, this.fileData, this.dataStream);
                    //threadList[i] = new Thread(() => this.GetMusicThread(music.CopyInfo[i].UserIndex, i, i * sizePP, (i + 1) * sizePP - 1));

                }
                threadList[i] = new Thread(() => dataReceiverList[i].start());
                threadList[i].Start();
                //threadList[i].Start();
                Thread.Sleep(1);
            }

            for (int i = 0; i < numOfPeerAvailable; i++)
            {
                threadList[i].Join();
            }
            bool ok = false;
            while (!ok)
            {
                int j = 0;
                for (; j < numOfPeerAvailable && dataReceiverList[j].status != 4; j++);
                if (j == numOfPeerAvailable) {
                    this.ifError = true;
                    Console.WriteLine("Peer Not Avaliable");
                    break;
                }
                for (int i = 0; i < numOfPeerAvailable; i++)
                {
                    ok = true;
                    if (dataReceiverList[i].status != 4 && dataReceiverList[i].status != 5)
                    {
                        dataReceiverList[i].status = 5;
                        DataReceiver dr = new DataReceiver(this.ipList[this.musicDownload.CopyInfo[j].UserIndex], port, dataReceiverList[i].currentByte, dataReceiverList[i].toByte, this.musicDownload.CopyInfo[j].FileName, this.musicDownload.AudioData.HashValue, (int)this.musicDownload.AudioData.Size, this.fileData, this.dataStream);
                        Thread t = new Thread(() => dr.start());
                        t.Start();
                        Thread.Sleep(1);
                        t.Join();
                        if (dr.status == 4)
                        {
                            ok = true;
                            continue;
                        }
                        else
                        {
                            ok = false;
                            dataReceiverList[i].status = -4;
                        }
                    }
                }
            }
            if (!ifError)
            {
                FileStream fs = new FileStream(this.musicDownload.AudioData.MediaPath, FileMode.Create);
                fs.Write(fileData, 0, (int)this.musicDownload.AudioData.Size);
                fs.Close();
                Console.WriteLine("Download file succeeded.");

                //var fileStream = File.Create("123.wma");
                //this.dataStream.Seek(0, SeekOrigin.Begin);
                //this.dataStream.CopyTo(fileStream);
                //fileStream.Close();
                //Console.WriteLine("File written.");
            }
            else
            {
                Console.WriteLine("Fail to download file.");
            }
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
            if (gres.CopyData(this.fileData, this.dataStream))
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

        public List<MusicCopy> MergeMusicList(List<MusicCopy>[] musicList)
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
