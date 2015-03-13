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

namespace P2P_Karaoke_System.p2p
{
    class Receiver
    {
        private static int segmentSize = 2048;
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
               
                Console.WriteLine("keyword = {0}", keyword);
                output = "200 SEARCH\nsearch_result<END>";
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
                    byte[] byteData = new byte[segmentSize];
                    fs.Seek(4, SeekOrigin.Begin);
                    fs.Read(byteData, (Convert.ToInt32(segID)*segmentSize), segmentSize);

                    // construct the output message
                    string header = "200 GET\r\n" + filename + "&" + md5 + "&" + segID + "\r\n"; //file_data<END>
                    string tail = "\r\n<END>";
                    byte[] byteHead = Encoding.UTF8.GetBytes(header);
                    byte[] byteTail = Encoding.UTF8.GetBytes(tail);

                    // merge output and file data
                    int offset = 0;
                    byteOut = new byte[byteHead.Length + byteData.Length + byteTail.Length];
                    Buffer.BlockCopy(byteHead, 0, byteOut, offset, byteHead.Length);
                    offset += byteHead.Length;
                    Buffer.BlockCopy(byteData, 0, byteOut, offset, byteData.Length);
                    offset += byteData.Length;
                    Buffer.BlockCopy(byteData, 0, byteOut, offset, byteTail.Length);

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
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
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
