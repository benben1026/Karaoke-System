using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P_Karaoke_System
{
    class Sender
    {
        private static int port = 3280;
        private static int peerNum = 10;
        private static string[] ipList = null;

        private static List<MusicCopy>[] searchResult = new List<MusicCopy>[peerNum];

        private static byte[] fileData = null;
        private static double segmentSize = 2048.0;
        private static int[] flag = null;
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
            string request = "SEARCH&" + keyword + "<EOR>";
            return Encoding.UTF8.GetBytes(request);
        }

        private static byte[] ConstructGetRequest(string filename, string md5, int segmentId)
        {
            string request = "GET&" + filename + "&" + md5 + "&" + segmentId + "<EOR>";
            return Encoding.UTF8.GetBytes(request);
        }

        public static void StartSearch(string keyword) 
        {
            string request = "SEARCH&" + keyword + "<EOR>";
            byte[] byteRequest = Encoding.UTF8.GetBytes(request);
            Thread[] threadList = new Thread[ipList.Length];
            for (int i = 0; i < ipList.Length; i++)
            {
                if (String.IsNullOrEmpty(ipList[i]))
                {
                    continue;
                }
                threadList[i] = new Thread(() => SearchThread(ipList[i], i, byteRequest));
                threadList[i].Start();
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
            int numOfSeg = Convert.ToInt32(Math.Ceiling(music.Size / segmentSize));
            ifGettingData = true;
            flag = new int[numOfSeg];
            for (int i = 0; i < numOfSeg; i++)
            {
                flag[i] = 0;
            }
            fileData = new byte[music.Size];
            for (int i = 0; i < music.CopyInfo.Count; i++)
            {

            }
        }

        private static void GetMusicThread(int index)
        {
            
        }

        private static List<MusicCopy> DecodeSearchResult(byte[] byteIn)
        {
            List<MusicCopy> outputMusicList = new List<MusicCopy>();
            string result = System.Text.Encoding.UTF8.GetString(byteIn);
            string[] stringSeparators = new string[] { "\r\n" };
            string[] resultSeg = result.Split( stringSeparators, StringSplitOptions.RemoveEmptyEntries );
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
                else {
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
            ipList[0] = "192.168.173.1";
            ipList[1] = "192.168.173.1";
            ipList[2] = "192.168.173.1";
            ipList[3] = "192.168.173.1";
            ipList[4] = "192.168.173.1";
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
