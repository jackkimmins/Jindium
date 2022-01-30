using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace Jindium
{
    public static class XLExtensions
    {
        public static IEnumerable<string> SplitInGroups(this string original, int size)
        {
            var p = 0;
            var l = original.Length;
            while (l - p > size)
            {
                yield return original.Substring(p, size);
                p += size;
            }
            yield return original.Substring(p);
        }
    }

    //A class for real-time bidirectional communication between the AppServer and connected clients.
    public abstract class RTC
    {
        //List of all connected clients.
        public List<TcpClient> clients = new List<TcpClient>();

        private void HandleClient(int index)
        {
            NetworkStream stream = clients[index].GetStream();

            while (true)
            {
                try
                {
                    while (!stream.DataAvailable);
                }
                catch
                {
                    break;
                }

                //Check if the index is valid.
                if (index < 0 || index >= clients.Count)
                {
                    break;
                }

                //Checks for GET request.
                while (clients[index].Available < 3) ;

                byte[] bytes = new byte[clients[index].Available];
                stream.Read(bytes, 0, clients[index].Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\n" + "Connection: Upgrade\r\n" + "Upgrade: websocket\r\n" + "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);

                    OnConnect(index);
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0;

                    //Ensures that it is a text message from the client.
                    int opcode = bytes[0] & 0b00001111,
                        msglen = bytes[1] - 128,
                        offset = 2;

                    if (msglen == 126)
                    {
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    }

                    if (msglen == 0)
                        cText.WriteLine("Message lenght was 0 chars.", "RTC", ConsoleColor.Red);
                    else if (mask)
                    {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (int i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                        if (decoded.Length == 2)
                        {
                            if (decoded[0] == 3 && decoded[1] == 233)
                            {
                                OnDisconnect();
                                return;
                            }
                        }

                        string text = Encoding.UTF8.GetString(decoded);

                        //throw new Exception(text);

                        if (text == "")
                            return;

                        cText.WriteLine(text, "RTC-MSG", ConsoleColor.Magenta);

                        OnRecieve(index, text);
                    }
                    else
                        cText.WriteLine("No MASK bit was set!", "RTC", ConsoleColor.Red);
                }
            }
        }

        //To disconnect a client.
        public void Disconnect(int index)
        {
            clients[index].Close();
            //clients.RemoveAt(index);
        }

        public abstract void OnRecieve(int index, string text);
        public abstract void OnConnect(int index);
        public abstract void OnDisconnect();

        //Reply to specific client.
        public bool Reply(int index, string msg)
        {
            if (!clients[index].Connected)
            {
                Disconnect(index);
                return false;
            }

            try
            {

                NetworkStream stream = clients[index].GetStream();
                Queue<string> que = new Queue<string>(msg.SplitInGroups(125));
                int len = que.Count;

                while (que.Count > 0)
                {
                    var header = GetHeader(
                        que.Count > 1 ? false : true,
                        que.Count == len ? false : true
                    );

                    byte[] list = Encoding.UTF8.GetBytes(que.Dequeue());
                    header = (header << 7) + list.Length;
                
                
                    stream.Write(IntToByteArray((ushort)header), 0, 2);
                    stream.Write(list, 0, list.Length);    
                }

            }
            catch (Exception e)
            {
                Disconnect(index);
                return false;
            }

            return true;       
        }

        //Gets the header for the message
        protected int GetHeader(bool finalFrame, bool contFrame)
        {
            int header = finalFrame ? 1 : 0;                //fin: 0 = more frames, 1 = final frame
            header = (header << 1) + 0;                     //rsv1
            header = (header << 1) + 0;                     //rsv2
            header = (header << 1) + 0;                     //rsv3
            header = (header << 4) + (contFrame ? 0 : 1);   //opcode : 0 = continuation frame, 1 = text
            header = (header << 1) + 0;                     //mask: server -> client = no mask

            return header;
        }

        //Converts an integer to a byte array
        protected byte[] IntToByteArray(ushort value)
        {
            var ary = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ary);
            return ary;
        }

        private void Run()
        {
            string ip = "51.89.252.169";
            int port = 8080;
            TcpListener server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();
            cText.WriteLine($"Server has started on {ip}:{port}. Waiting for a connection...", "RTC", ConsoleColor.Magenta);

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();

                //Add client to clients and get index
                int index = clients.Count;
                clients.Add(client);

                cText.WriteLine("Client Connected", "RTC", ConsoleColor.Magenta);

                Thread thread = new Thread(() => HandleClient(index));
                thread.Start();
            }
        }

        //Broadcast message to all connected clients.
        public void Broadcast(string msg = "Hello, World!", int ignoreIndex = -1)
        {
            for (int i = 0; i < clients.Count; ++i)
            {
                if (i == ignoreIndex)
                    continue;

                Reply(i, msg);
            }
        }

        //Starts the RTC server on a new thread.
        public void Start()
        {
            cText.WriteLine("Starting RTC server...");
            Thread thread = new Thread(Run);
            thread.Start();
        }
    }
}