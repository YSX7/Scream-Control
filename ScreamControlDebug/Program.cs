using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MicrophoneTest
{
    class Program
    {

        static void Main(string[] args)
        {

            UdpClient client = new UdpClient();
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 15000);
            Console.WriteLine("Введи ченить че ты");
            string input = Console.ReadLine();
            byte[] bytes = Encoding.ASCII.GetBytes(input);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
        }
    }
}