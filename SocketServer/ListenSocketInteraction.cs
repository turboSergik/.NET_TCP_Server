using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public Dictionary<string, Socket> connectedUsers;
        public Dictionary<string, Socket> usersSockets;
        public Dictionary<Socket, string> userLogin;
        public List<string> loginList;

        public string login = "";

        public ListenSocketInteraction()
        {
            ipPoint = new IPEndPoint(IPAddress.Parse(Settings.address), Settings.port);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            connectedUsers = new Dictionary<string, Socket>();
            usersSockets = new Dictionary<string, Socket>();
            userLogin = new Dictionary<Socket, string>();
            loginList = new List<string>(); 

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
            // Accepting user request
            Socket handler = listenSocket.Accept();

            Console.WriteLine("User successfully connected!");

            // Creating thread to communicate with client
            Thread manager = new Thread(new ThreadStart(() => requestManager(handler)));
            manager.Start();
        }

        public void requestManager(Socket handler)
        {
            try
            {
                while (true)
                {
                    waitingAnswer(handler);
                }
            } catch(Exception)
            {
                closeConnection(handler);
                return;
            }
        }
    

        public void sendMessage(Socket handler, string message)
        {
            Packet packet = new Packet();
            packet = Protocol.ConfigurePacket(Command.TEXT, "Server", Encoding.Unicode.GetBytes(message));

            // Sending header + response to client
            handler.Send(packet.MetaBytes());
            handler.Send(packet.Response);
        }

        public void waitingAnswer(Socket handler)
        {
            // receive message
            StringBuilder builder = new StringBuilder();
            int bytes = 0; // bytes readed

            // Waiting for meta bytes
            byte[] metaData = new byte[256];
            bytes = handler.Receive(metaData);

            // Parsing meta
            Dictionary<string, string> meta = Protocol.ParseMeta(Encoding.UTF8.GetString(metaData, 0, bytes));

            // Check user descriptor
            if (userLogin.ContainsKey(handler) is true) login = userLogin[handler];

            // Parsing meta command and managing request
            switch (Enum.Parse(typeof(Command), meta["Command"]))
            {
                case Command.LOGIN:

                    loginList.Add(meta["User"]); // Add username to authorized users list
                    login = meta["User"]; // Current username
                    usersSockets[login] = handler; // Key - username, value - descriptor
                    userLogin[handler] = login; // Key - descriptor, value - username
                    break;

                case Command.TEXT:

                    Packet packet = new Packet();
                    // Validate username
                    if (haveConnect(login) == false)
                    {
                        string message = getTextRequest(handler);
                        sendMessage(handler, "You dont have user to connect!");
                        break;
                    }

                    // Getting client request
                    string text = getTextRequest(handler);

                    // Configure response
                    packet = Protocol.ConfigurePacket(Command.TEXT, login, Encoding.Unicode.GetBytes(text));

                    // Sending meta + response bytes to connected user
                    connectedUsers[login].Send(packet.MetaBytes());
                    connectedUsers[login].Send(packet.Response);

                    break;
                case Command.BIN:
                    // Validate
                    if (haveConnect(login) == false)
                    {
                        string message = getTextRequest(handler);
                        sendMessage(handler, "You dont have user to connect!");
                        break;
                    }
                    // Parsing client binary request
                    parseBinRequest(handler, meta);
                    break;

                case Command.UTILS:
                    // Server utils
                    string addData = getTextRequest(handler);
                 
                    // Parsing client command
                    parseUtilsRequest(handler, meta["Utils"], addData);
                    break;
            }
            return;
        }

   
        static string getTextRequest(Socket handler)
        {
            StringBuilder builder = new StringBuilder();
            do
            {
                // Receiving packets each 256 bytes
                byte[] data = new byte[256];
                int bytes = handler.Receive(data, data.Length, 0);
                // Appending and decoding them to string builder
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (handler.Available > 0);

            return builder.ToString();
        }

        public void parseBinRequest(Socket handler, Dictionary<string, string> meta)
        {
            // Creating byte container
            List<byte[]> list = new List<byte[]>();
            int countOfAllBytes = 0;

            do
            {
                // Receiving packets each 256 bytes
                byte[] data = new byte[256];
                countOfAllBytes += handler.Receive(data, data.Length, 0);

                list.Add(data);
            }
            while (handler.Available > 0);

            // Join bytes packets into byte array
            byte[] allData = list
                            .SelectMany(a => a)
                            .ToArray();

            // Configuring final package
            Packet packet = new Packet();

            if (meta.ContainsKey("Utils") == true) packet = Protocol.ConfigurePacket(Command.BIN, login, meta["Utils"], allData);
            else packet = Protocol.ConfigurePacket(Command.BIN, login, allData);

            // Sending meta + bytes to other client
            connectedUsers[login].Send(packet.MetaBytes());
            connectedUsers[login].Send(packet.Response);

        }

        public void parseUtilsRequest(Socket handler, string utils, string message)
        {
            // Getting authorized users
            if (utils.ToLower() == "get")
            {
                string answer = "";
                answer += $"Count of all users = {loginList.Count}\n";

                foreach (var user_name in loginList)
                {
                    if (user_name == login) answer += $"Login: {user_name} <---- It's you\n";
                    else answer += $"Login: {user_name}\n";
                }

                sendMessage(handler, answer);
            }
            // Connecting to client
            if (utils.ToLower() == "connect")
            {
                string user_name = message;
                if (loginList.Contains(user_name) is false)
                {
                    sendMessage(handler, "Wrong login!");
                }
                else
                {
                    sendMessage(handler, "Successful connection");
                    connectedUsers[login] = usersSockets[user_name];
                }
            }
            // Disconnecting from server
            if (utils.ToLower() == "disconnect")
            {
                closeConnection(handler);
            }
        }

        public bool haveConnect(string login)
        {
            if (connectedUsers.ContainsKey(login) == false) return false;
            return true;
        }

        public void closeConnection(Socket handler)
        {
            // Cleanup
            loginList.Remove(login);
            connectedUsers[login] = null;
            usersSockets.Remove(login);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

            Console.WriteLine("User disconnected");
        }
    }
}
