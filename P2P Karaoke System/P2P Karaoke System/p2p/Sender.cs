﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace P2P_Karaoke_System.p2p
{
    class Sender
    {
        private static int port = 3280;

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