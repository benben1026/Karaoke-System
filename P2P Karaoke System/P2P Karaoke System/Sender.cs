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
    class Sender
    {
        private static int port = 3280;
        private static int peerNum = 10;
        private static string[] ipList = null;

        private static List<MusicCopy>[] searchResult = new List<MusicCopy>[peerNum];

        private static byte[] fileData = null;
        private static int segmentSize = 4096;
        private static int[] flag = null;
        private static int packetLeft;
        private static bool flagLock = false;
        private static bool ifGettingData = false;
        private static MusicCopy musicDownload = null;

        private static Socket ConnectSocket(string serverIP, int port)
        {
            //Console.WriteLine("1");
            IPAddress ip = IPAddress.Parse(serverIP);
            //Console.WriteLine("2");
            IPEndPoint ipe = new IPEndPoint(ip, port);
            //Console.WriteLine("3");
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

        private static void SendRequest(string ip, byte[] bytesSent)
        {
            //string request = "TEST REQUEST FROM BENJAMIN<EOR>";
            //Byte[] bytesSent = Encoding.UTF8.GetBytes(request);
            Byte[] bytesReceived = new Byte[256];

            Console.WriteLine("Connecting to {0}", ip);
            Socket s = ConnectSocket(ip, port);

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

        private static byte[] ConstructSearchRequest(string keyword)
        {
            string request = "SEARCH&" + keyword + "&<EOR>";
            return Encoding.UTF8.GetBytes(request);
        }

        private static byte[] ConstructGetRequest(string filename, string md5, int segmentId)
        {
            string request = "GET&" + filename + "&" + md5 + "&" + segmentId + "&<EOR>";
            return Encoding.UTF8.GetBytes(request);
        }

        public static void StartSearch(string keyword)
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
                int temp = i;
                threadList[i] = new Thread(() => SearchThread(ipList[i], temp, byteRequest));
                threadList[i].Start();
                Thread.Sleep(1);
            }
            Thread.Sleep(2000);
            MergeMusicList(searchResult);
        }

        private static void SearchThread(string ip, int index, byte[] bytesSent)
        {
            Console.WriteLine("Connecting to {0}", ipList[index]);
            Socket s = ConnectSocket(ipList[index], port);
            if (s == null)
            {
                Console.WriteLine("{0}:Connection Failed", ipList[index]);
                return;
            }
            Console.WriteLine("{0}:Connection success", ipList[index]);
            s.Send(bytesSent, bytesSent.Length, 0);
            int bytes = 0;
            byte[] bytesReceived = new byte[256];
            do
            {
                bytes = s.Receive(bytesReceived, bytesReceived.Length, 0);
                //Console.WriteLine(Encoding.UTF8.GetString(bytesReceived, 0, bytes));
                if (Encoding.UTF8.GetString(bytesReceived).IndexOf("<END>") > -1)
                {
                    break;
                }
            } while (bytes > 0);
            searchResult[index] = DecodeSearchResult(bytesReceived);
        }

        public static void StartGetMusic(MusicCopy music)
        {
            if (ifGettingData)
            {
                return;
            }
            musicDownload = music;
            int numOfSeg = ((music.Size - 1) / segmentSize) + 1;
            packetLeft = numOfSeg;
            ifGettingData = true;
            flag = new int[numOfSeg];
            for (int i = 0; i < numOfSeg; i++)
            {
                flag[i] = 0;
            }
            fileData = new byte[music.Size];
            Console.WriteLine("filedata = {0}",fileData);
            Thread[] threadList = new Thread[music.CopyInfo.Count()];
            for (int i = 0; i < music.CopyInfo.Count(); i++)
            {
                if (String.IsNullOrEmpty(music.CopyInfo[i].FileName) || music.CopyInfo[i].UserIndex < 0 || music.CopyInfo[i].UserIndex >= peerNum)
                {
                    continue;
                }
                threadList[i] = new Thread(() => GetMusicThread(music.CopyInfo[i].UserIndex));
                threadList[i].Start();
                Thread.Sleep(1);
            }
            while (packetLeft != 0);
            //for (int i = 0; i < music.CopyInfo.Count(); i++)
            //{
            //    threadList[i].Join(20000);
            //}
            FileStream fs = new FileStream(music.Filename, FileMode.Create);
            fs.Write(fileData, 0, music.Size);
            fs.Close();
        }

        private static void GetMusicThread(int index)
        {
            Console.WriteLine("Connecting to {0}", ipList[index]);
            Socket s = ConnectSocket(ipList[index], port);
            if (s == null)
            {
                Console.WriteLine("{0}:Connection Failed", ipList[index]);
                return;
            }
            Console.WriteLine("{0}:Connection success", ipList[index]);
            Console.WriteLine("segmentSize = {0}, filename = {1}, segNum = {2}, packetLeft = {3}", segmentSize, musicDownload.Filename, flag.Count(), packetLeft);
            while (true)
            {
                int i = 0;
                for (i = 0; i < flag.Count(); i++)
                {
                    if (AccessFlag(0, i, 0) == 0)
                    {
                        byte[] request = ConstructGetRequest(musicDownload.Filename, musicDownload.Hashvalue, i);
                        s.Send(request, request.Length, 0);
                        AccessFlag(1, i, 1);
                        break;
                    }
                }
                if (i == flag.Count())
                {
                    break;
                }
                int bytes = 0;

                byte[] bytesReceived = new byte[8];
                for (int remain = 8; remain > 0; remain -= bytes)
                {
                    bytes = s.Receive(bytesReceived, 8 - remain, remain, 0);
                }
                //byte[] bytesReceived = new byte[8];
                //bytes = s.Receive(bytesReceived, 8, 0);
                string status = Encoding.UTF8.GetString(bytesReceived, 0, 3);
                Console.WriteLine("status = {0}", status);
                if (String.Compare(status, "200") == 0)
                {
                    bytesReceived = new byte[2];
                    for (int remain = 2; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(bytesReceived, 2 - remain, remain, 0);
                    }
                    //bytesReceived = new byte[2];
                    //bytes = s.Receive(bytesReceived, 2, 0);
                    ushort parameterLength = BitConverter.ToUInt16(bytesReceived, 0);
                    //Console.WriteLine("parameterLength = {0}", parameterLength);

                    bytesReceived = new byte[parameterLength];
                    for (int remain = parameterLength; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(bytesReceived, parameterLength - remain, remain, 0);
                    }
                    //bytesReceived = new byte[parameterLength];
                    //bytes = s.Receive(bytesReceived, parameterLength, 0);
                    string[] parameter = Encoding.UTF8.GetString(bytesReceived).Split('&');
                    int segmentId = Convert.ToInt32(parameter[2]);
                    Console.WriteLine("segmentId = {0}", segmentId);

                    bytesReceived = new byte[2];
                    for (int remain = 2; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(bytesReceived, 2 - remain, remain, 0);
                    }
                    //bytesReceived = new byte[2];
                    //bytes = s.Receive(bytesReceived, 2, 0);
                    ushort payloadLength = BitConverter.ToUInt16(bytesReceived, 0);
                    //Console.WriteLine("payloadLength = {0}", payloadLength);

                    bytesReceived = new byte[payloadLength];
                    for (int remain = payloadLength; remain > 0; remain -= bytes)
                    {
                        bytes = s.Receive(bytesReceived, payloadLength - remain, remain, 0);
                    }
                    Array.Copy(bytesReceived, 0, fileData, (int)(segmentId * segmentSize), payloadLength);
                    //bytesReceived.CopyTo(fileData, Convert.ToInt64(segmentId * segmentSize));
                    //Console.WriteLine("data = {0}", BitConverter.ToString(bytesReceived));
                    AccessFlag(1, i, 2);
                    //while (true)
                    //{
                    //    bytes = s.Receive(bytesReceived, 1, 0);
                    //    if (bytes == 0)
                    //    {
                    //        break;
                    //    }
                    //}

                    Thread.Sleep(100);
                }
                else
                {
                    Console.WriteLine("Response Error: status = {0}", status);
                    AccessFlag(1, i, 0);
                    Thread.Sleep(2000);
                }
            }
            s.Shutdown(SocketShutdown.Both);
            s.Close();
        }

        private static int AccessFlag(int mode, int index, int status)
        {
            while (flagLock) ;
            flagLock = true;
            int temp = -1;
            if (mode == 0)
            {
                temp = flag[index];
            }
            else if (mode == 1)
            {
                flag[index] = status;
                if (status == 2)
                {
                    packetLeft--;
                }
            }
            flagLock = false;
            return temp;
        }

        private static List<MusicCopy> DecodeSearchResult(byte[] byteIn)
        {
            List<MusicCopy> outputMusicList = new List<MusicCopy>();
            string result = System.Text.Encoding.UTF8.GetString(byteIn);
            string[] stringSeparators = new string[] { "\r\n" };
            string[] resultSeg = result.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            int i = 0;
            foreach (string s in resultSeg)
            {
                if (i == 0)
                {
                    if (String.Compare(s, "200 SEARCH", false) != 0)
                    {
                        // error
                        return null;
                    }
                    else i++;
                }
                else if (String.Compare(s, "<END>", false) == 0)
                {
                    break;
                }
                else
                {
                    string[] separators = new string[] { "&" };
                    string[] musicProperty = s.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    outputMusicList.Add(new MusicCopy(musicProperty[0], musicProperty[1], musicProperty[2], musicProperty[3], musicProperty[4], Convert.ToInt32(musicProperty[5]), Convert.ToInt32(musicProperty[6])));

                }
            }

            return outputMusicList;
        }


        private static List<MusicCopy> MergeMusicList(List<MusicCopy>[] musicList)
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
                        if (newList[i].Hashvalue == oldList[j].Hashvalue && (String.Compare(newList[i].Title, oldList[j].Title, false) == 0))
                        {
                            duplicate = true;
                            oldList[j].CopyNumber++;
                            oldList[j].CopyInfo.Add(new CopyIndex(k, newList[i].Filename));
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
            ipList = new string[peerNum];
            ipList[0] = "192.168.213.200";
            ipList[1] = "";
            ipList[2] = "";
            ipList[3] = "";
            ipList[4] = "";
            ipList[5] = "";
            ipList[6] = "";
            ipList[7] = "";
            ipList[8] = "";
            ipList[9] = "";

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
