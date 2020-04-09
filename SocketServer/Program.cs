using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketTcpServer
{
    class Program
    {
        static int port = 8005; // порт для приема входящих запросов
        static void Main(string[] args)
        {
            // получаем адреса для запуска сокета
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            // создаем сокет
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // связываем сокет с локальной точкой, по которой будем принимать данные
                listenSocket.Bind(ipPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // начинаем прослушивание
            listenSocket.Listen(10);

            while (true)
            {
                
                Console.WriteLine("Сервер запущен. Ожидание подключений...");
                Socket handler = listenSocket.Accept();

                Thread manager = new Thread(new ThreadStart(() => requestManager(handler)));
                manager.Start();

            }
        }


        public static void requestManager(Socket handler)
        {
            Console.WriteLine("Успешное подключение!");

            while (true)
            {

                // получаем сообщение
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байтов
                byte[] data = new byte[256]; // буфер для получаемых данных

                try
                {
                    do
                    {
                        bytes = handler.Receive(data);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (handler.Available > 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                    break;
                }

                Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + builder.ToString());

                string message = "";
                if (builder.ToString().ToLower() == "end") {


                    // отправляем ответ
                    message = "Подключение закрыто!";
                    data = Encoding.Unicode.GetBytes(message);
                    handler.Send(data);

                    // закрываем сокет
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                    break;
                }


                message = "Сообщение досталвено!";
                data = Encoding.Unicode.GetBytes(message);
                handler.Send(data);

            }
        }
    }
}