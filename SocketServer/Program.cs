using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.IO;

using SocketServer;

namespace SocketTcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            
            ListenSocketInteraction listenSocketInteraction = new ListenSocketInteraction();


            // start listening
            while (true)
            {
                listenSocketInteraction.requestPending();
            }
        }
    }
}