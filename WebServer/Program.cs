using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebServer
{
    class MyWebServer
    {
        public static void Main(string[] args)
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            int port = 5050;
            NetworkFactory.GetNetworkConnection(ipAddress, port);
        }
    }
}