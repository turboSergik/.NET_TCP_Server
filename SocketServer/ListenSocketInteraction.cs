using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SocketServer
{
    public class ListenSocketInteraction
    {
        public Socket listenSocket;
        public IPEndPoint ipPoint;

        public ListenSocketInteraction()
        {
            ipPoint = new IPEndPoint(IPAddress.Parse(Settings.address), Settings.port);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // associate the socket with the local point at which we will receive data
                listenSocket.Bind(ipPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // maximum number of connections available
            listenSocket.Listen(Settings.connectionsAbailable);
        }


        public void requestPending()
        {
            Console.WriteLine("Сервер запущен. Ожидание подключений...");
            Socket handler = listenSocket.Accept();

            Console.WriteLine("Успешное подключение!");
            Thread manager = new Thread(new ThreadStart(() => requestManager(handler)));
            manager.Start();
        }

        public void requestManager(Socket handler)
        {
            while (true)
            {
                string answer = "";
                answer = waitingAnswer(handler);

                byte[] data = new byte[256];

                if (answer == "end")
                {
                    // send answer
                    string message = "Connection closed by user!";
                    data = Encoding.Unicode.GetBytes(message);
                    handler.Send(data);

                    // закрываем сокет
                    closeConnection(handler);
                    return;
                }

                sendAcceptMessage(handler);
            }
        }
    

        public void sendAcceptMessage(Socket handler)
        {

            string message = "Message delivered!";
            byte[] data = Encoding.Unicode.GetBytes(message);

            data = Encoding.Unicode.GetBytes(message);
            handler.Send(data);

        }

        public string waitingAnswer(Socket handler)
        {
            // получаем сообщение
            StringBuilder builder = new StringBuilder();
            int bytes = 0; // количество полученных байтов

            try
            {
                byte[] metaData = new byte[256];
                bytes = handler.Receive(metaData);

                Dictionary<string, string> meta = Protocol.ParseMeta(Encoding.UTF8.GetString(metaData, 0, bytes));

                switch (Enum.Parse(typeof(Command), meta["Command"]))
                {
                    case Command.LOGIN:

                        break;
                    case Command.TEXT:
                        break;
                    case Command.BIN:
                        break;
                }

                List<byte[]> list = new List<byte[]>();
                do
                {
                    byte[] data = new byte[256];
                    bytes = handler.Receive(metaData);

                    list.Add(data);
                }
                while (handler.Available > 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }

            Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());

            return builder.ToString();
        }

        public void closeConnection(Socket handler)
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
        }
    }
}
