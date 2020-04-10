using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ComponentModel.Design;
using System.Text.RegularExpressions;

namespace SocketServer
{
    // Supported commands
    enum Command { LOGIN, TEXT, BIN, UTILS };

    struct Packet
    {
        public Dictionary<string, string> Meta;
        public byte[] Response;
        public override string ToString()
        {
            string result = Encoding.UTF8.GetString(this.MetaBytes());
            result += Encoding.UTF8.GetString(this.Response);
            return result;
        }

        public byte[] MetaBytes()
        {
            string result = "";
            foreach (KeyValuePair<string, string> pair in this.Meta)
            {
                result += pair.Key;
                result += ": ";
                result += pair.Value;
                result += "\n";
            }
            result += "\n";
            return Encoding.UTF8.GetBytes(result);
        }
    };

    /**
     * Format:
     * 
     * Command: login
     * Compression: gzip
     * 
     * edelwud
     * 
     */

    class Protocol
    {
        // Allowed headers
        string[] AllowedHeaders = { "Command", "User", "Utils" };

        // Configuring packet
        public static Packet ConfigurePacket(Command command, string username, byte[] message)
        {
            Dictionary<string, string> format = new Dictionary<string, string>();
            format.Add("Command", command.ToString());
            format.Add("User", username);

            Packet packet = new Packet();
            packet.Meta = format;
            packet.Response = message;
            return packet;
        }

        // Configuring packet
        public static Packet ConfigurePacket(Command command, string username, string utils, byte[] message)
        {
            Dictionary<string, string> format = new Dictionary<string, string>();
            format.Add("Command", command.ToString());
            format.Add("User", username);
            format.Add("Utils", utils);

            Packet packet = new Packet();
            packet.Meta = format;
            packet.Response = message;
            return packet;
        }

        // Parsing meta string
        public static Dictionary<string, string> ParseMeta(string buffer)
        {
            // Regular expression (2 groups: 1 - header, 2 - value)
            Regex metaParser = new Regex("([A-Za-z 0-9]+): +(.+)");
            Match match = metaParser.Match(buffer); // Match string

            Dictionary<string, string> meta = new Dictionary<string, string>();

            while (match.Success) // While has match
            {
                string header = match.Groups[1].Value; // Getting header
                string value = match.Groups[2].Value; // Getting header value

                meta.Add(header, value);
                match = match.NextMatch(); // Next iteration
            }
            return meta;
        }
    }
}
