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

    class RTC
    {
        public List<TcpClient> clients = new List<TcpClient>();

        private void HandleClient(int index)
        {
            NetworkStream stream = clients[index].GetStream();

            // enter to an infinite cycle to be able to handle every change in stream
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
                
                while (clients[index].Available < 3); // match against "get"

                byte[] bytes = new byte[clients[index].Available];
                stream.Read(bytes, 0, clients[index].Available);
                string s = Encoding.UTF8.GetString(bytes);

                if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
                {
                    Console.WriteLine("=====Handshaking from client=====\n{0}", s);

                    // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                    // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                    // 3. Compute SHA-1 and Base64 hash of the new value
                    // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                    string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                    string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                    byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                    string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                    // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                    byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols\r\n" + "Connection: Upgrade\r\n" + "Upgrade: websocket\r\n" + "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                    stream.Write(response, 0, response.Length);
                }
                else
                {
                    bool fin = (bytes[0] & 0b10000000) != 0,
                        mask = (bytes[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"

                    int opcode = bytes[0] & 0b00001111, // expecting 1 - text message
                        msglen = bytes[1] - 128, // & 0111 1111
                        offset = 2;

                    if (msglen == 126)
                    {
                        // was ToUInt16(bytes, offset) but the result is incorrect
                        msglen = BitConverter.ToUInt16(new byte[] { bytes[3], bytes[2] }, 0);
                        offset = 4;
                    }
                    else if (msglen == 127)
                    {
                        Console.WriteLine("TODO: msglen == 127, needs qword to store msglen");
                    }

                    if (msglen == 0)
                        Console.WriteLine("msglen == 0");
                    else if (mask)
                    {
                        byte[] decoded = new byte[msglen];
                        byte[] masks = new byte[4] { bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3] };
                        offset += 4;

                        for (int i = 0; i < msglen; ++i)
                            decoded[i] = (byte)(bytes[offset + i] ^ masks[i % 4]);

                        string text = Encoding.UTF8.GetString(decoded);
                        Console.WriteLine("{0}", text);

                        Recieve(index, text);
                    }
                    else
                        Console.WriteLine("mask bit not set");

                    Console.WriteLine();
                }
            }
        }

        private void Disconnect(int index)
        {
            clients[index].Close();
            clients.RemoveAt(index);
        }

        public virtual void Recieve(int index, string text)
        {
            Reply(index, "Hello, client! You said: " + text);
        }

        public bool Reply(int index, string msg)
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
                
                try
                {
                    stream.Write(IntToByteArray((ushort)header), 0, 2);
                    stream.Write(list, 0, list.Length);
                }
                catch (Exception e)
                {
                    Disconnect(index);
                    return false;
                }
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
            string ip = "127.0.0.1";
            int port = 8080;
            var server = new TcpListener(IPAddress.Parse(ip), port);

            server.Start();
            cText.WriteLine($"Server has started on {ip}:{port}. Waiting for a connection...", "RTC");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();

                //Add client to clients and get index
                int index = clients.Count;
                clients.Add(client);

                cText.WriteLine("Client Connected", "RTC");

                Thread thread = new Thread(() => HandleClient(index));
                thread.Start();
            }
        }

        public void Broadcast(string msg = "Hello, World!", int ignoreIndex = -1)
        {
            for (int i = 0; i < clients.Count; ++i)
            {
                if (i == ignoreIndex)
                    continue;

                Reply(i, msg);
            }
        }

        public void Start()
        {
            cText.WriteLine("Starting RTC server...");
            Thread thread = new Thread(Run);
            thread.Start();
        }
    }
}