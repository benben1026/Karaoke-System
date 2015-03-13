using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P_Karaoke_System.p2p
{
    class Receiver
    {
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
            if (method.Equals("search", StringComparison.InvariantCultureIgnoreCase))
            {
                string keyword = parameter[1];
                Console.WriteLine("keyword = {0}", keyword);
                output = "200 SEARCH\nsearch_result<END>";
            }
            else if (method.Equals("get", StringComparison.InvariantCultureIgnoreCase))
            {
                string filename = parameter[1];
                string md5 = parameter[2];
                Console.WriteLine("filename = {0} md5 = {1}", filename, md5);
                output = "200 GET\r\nhello.mp3&ENF84JGHD84JDJT874J&19\r\nfile_data<END>";
            }
            else
            {
                output = "500 \nINVALID REQUEST<END>";
            }
            return Encoding.UTF8.GetBytes(output);
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
