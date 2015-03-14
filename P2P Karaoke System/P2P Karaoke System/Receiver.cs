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
    class Receiver
    {
        private static double segmentSize = 2048.0;
        // chunk size in bytes

        private static string data = null;
        /*
        private static void ChildThreadListening()
        {
            byte[] bytes = new Byte[1024];
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 3280);

            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                Console.WriteLine("waiting for connection...");
                //program is suspended
                Socket handler = listener.Accept();

                //new a thread
                //Thread childListener = new Thread(new ThreadStart(ChildThreadListening));
                //childListener.Start();


                data = null;
                Console.WriteLine("Connected");

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    data += Encoding.UTF8.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOR>") > -1)
                    {
                        break;
                    }
                }

                Console.WriteLine("Text received : {0}", data);
                byte[] msg = processRequest(data);
                //Echo the data back to the client
                //byte[] msg = Encoding.UTF8.GetBytes(data);

                handler.Send(msg);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        */

        public static byte[] processRequest(string request)
        {
            string output = null;
            byte[] msg = new byte[1024];
            string[] parameter = request.Split('&');
            string method = parameter[0];
            byte[] byteOut = null;
            if (method.Equals("search", StringComparison.InvariantCultureIgnoreCase))
            {
                string keyword = parameter[1];
                List<MusicData> musicList = new List<MusicData>();
                // get the music list from UI(?)

                List<MusicData> searchResult = MusicSearchUtil.SearchedMusicList(keyword, musicList);

                Console.WriteLine("keyword = {0}", keyword);

                // construct the output message
                string header = "200 SEARCH\r\n";
                string tail = "<END>";
                int items = searchResult.Count();
                output = header;
                // search result data: properties delimited by &, file delimited by newline
                for (int i = 0; i < items; i++)
                {
                    output += searchResult[i].Filename + "&" + searchResult[i].Title + "&"
                               + searchResult[i].Singer + "&" + searchResult[i].Album + "&"
                               + searchResult[i].Hashvalue + "&" + Convert.ToString(searchResult[i].Size) + "&"
                               + Convert.ToString(searchResult[i].Relevancy) + "\r\n";
                }
                output += tail;
                byteOut = Encoding.UTF8.GetBytes(output);
            }
            else if (method.Equals("get", StringComparison.InvariantCultureIgnoreCase))
            {
                // parameters in the request
                string filename = parameter[1];
                string md5 = parameter[2];
                string segID = parameter[3];

                Console.WriteLine("filename = {0} md5 = {1}", filename, md5);

                // check whether the file is modified
                FileStream fs = new FileStream(filename, FileMode.Open);
                RIPEMD160 myRIPEMD160 = RIPEMD160Managed.Create();
                byte[] hashvalue = myRIPEMD160.ComputeHash(fs);
                string hash = convertHashValue(hashvalue);

                if (String.Compare(md5, hash, true) == 0)
                {
                    // read the segment we need from the file
                    int segSize = Convert.ToInt32(segmentSize);
                    byte[] byteData = new byte[segSize];
                    fs.Seek(4, SeekOrigin.Begin);
                    fs.Read(byteData, (Convert.ToInt32(segID) * segSize), segSize);

                    // construct the output message
                    string header = "200 GET " ;
                    // header + ushort1 + info + ushort2 + data
                    
                    string info = filename + "&" + md5 + "&" + segID + " ";
                    byte[] byteHead = Encoding.UTF8.GetBytes(header);
                    byte[] byteInfo = Encoding.UTF8.GetBytes(info);
                    ushort size1 = (ushort) byteInfo.Length;
                    //size of file information
                    ushort size2 = (ushort) byteData.Length;
                    //size of data segment
                    byte[] info1 = BitConverter.GetBytes(size1);
                    byte[] info2 = BitConverter.GetBytes(size2);

                    // merge output and file data
                    int offset = 0;
                    byteOut = new byte[byteHead.Length + byteData.Length + byteInfo.Length + 4];
                    Buffer.BlockCopy(byteHead, 0, byteOut, offset, byteHead.Length);
                    offset += byteHead.Length;
                    Buffer.BlockCopy(info1, 0, byteOut, offset, 2);
                    offset += 2;
                    Buffer.BlockCopy(byteInfo, 0, byteOut, offset, byteInfo.Length);
                    offset += byteInfo.Length;
                    Buffer.BlockCopy(info2, 0, byteOut, offset, 2);
                    offset += 2;
                    Buffer.BlockCopy(byteData, 0, byteOut, offset, byteData.Length);

                }
                else 
                {
                    output = "300 GET SHORT ERROR-MEESAGE";
                    byteOut = Encoding.UTF8.GetBytes(output);
                }
            }
            else
            {
                output = "500 \nINVALID REQUEST<END>";
                byteOut = Encoding.UTF8.GetBytes(output);
            }
            return byteOut;
        }

        public static string convertHashValue(byte[] hashvalue)
        {
            var sb = new StringBuilder("");
            foreach (var b in hashvalue)
            {
                sb.Append(b);
                // we can change this format accordingly
            }
            return sb.ToString();
        }

        public static void StartListening()
        {
            byte[] bytes = new Byte[1024];

            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 3280);

            Socket listener = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine("waiting for connection...");
                    //program is suspended
                    Socket handler = listener.Accept();
                    data = null;
                    Console.WriteLine("Connected.");

                    while (true)
                    {
                        bytes = new byte[1024];
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<EOR>") > -1)
                        {
                            break;
                        }
                    }

                    Console.WriteLine("Text received : {0}", data);
                    //Echo the data back to the client
                    byte[] msg = processRequest(data);

                    handler.Send(msg);
                    //handler.Shutdown(SocketShutdown.Both);
                    //handler.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();
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
