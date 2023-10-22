using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignalSocket
{
    internal class PlainTCP
    {
        public static void StartServerNow()
        {
            int websocket_port = 8996;
            TcpListener websocket = new TcpListener(IPAddress.Any, websocket_port);
            websocket.Start();
            bool websocket_isRunning = true;
            while (websocket_isRunning)
            {
                Console.WriteLine("--- Waiting for WebSocket ----");
                TcpClient websocket_client = websocket.AcceptTcpClient();
                NetworkStream sockStream = websocket_client.GetStream();
                byte[] bRead = new byte[1024];
                sockStream.Read(bRead, 0, bRead.Length);
                Console.WriteLine("---- Reading WebSocket ----");
                string websocket_header = Encoding.UTF8.GetString(bRead);

                string matchs = new System.Text.RegularExpressions.Regex("Sec-WebSocket-Key: (.*)").Match(websocket_header).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                string finalAcceptedKey = Convert.ToBase64String(System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(matchs)));

                // Write the WebSocket handshake response
                string responseHeader = "";
                responseHeader += "HTTP/1.1 101 Switching Protocols\r\n";
                responseHeader += "Connection: Upgrade\r\n";
                responseHeader += "Upgrade: websocket\r\n";
                responseHeader += "Sec-WebSocket-Accept: " + finalAcceptedKey + "\r\n";
                responseHeader += "\r\n";
                byte[] sendResponse = Encoding.UTF8.GetBytes(responseHeader);
                sockStream.Write(sendResponse, 0, sendResponse.Length);

                while (true)
                {
                    try
                    {
                        Send(sockStream);
                        Receive(sockStream);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
            }
        }
        public static Task Send(NetworkStream sockStream)
        {
            return Task.Run(() =>
            {
                // Prepare a WebSocket frame for sending a text message
                byte[] msg = Encoding.UTF8.GetBytes("Your message");
                byte[] frame = new byte[2 + msg.Length];
                frame[0] = 0x81;  // Text frame opcode
                frame[1] = (byte)msg.Length;  // Payload length
                Array.Copy(msg, 0, frame, 2, msg.Length);

                // Send the WebSocket frame
                sockStream.Write(frame, 0, frame.Length);
                Console.WriteLine("--- Message Sent: " + Encoding.UTF8.GetString(msg) + " ---");
                Thread.Sleep(2000);
                Send(sockStream);
            });
        }
        public static Task Receive(NetworkStream sockStream)
        {
            return Task.Run(() =>
            {

                // Receive and process WebSocket message
                byte[] frameHeader = new byte[2];
                sockStream.Read(frameHeader, 0, frameHeader.Length);
                byte opcode = (byte)(frameHeader[0] & 0x0F);
                byte payloadLength = (byte)(frameHeader[1] & 0x7F);

                if (opcode == 0x8)
                {
                    // Handle WebSocket connection close frame
                    Console.WriteLine("--- Connection closed by client ---");
                }

                if (payloadLength > 0)
                {
                    byte[] msg2 = new byte[payloadLength];
                    sockStream.Read(msg2, 0, msg2.Length);

                    // Handle the received message (msg) here
                    string message = Encoding.UTF8.GetString(msg2);
                    Console.WriteLine("--- Received Message: " + message + " ---");
                }
                Thread.Sleep(2000);
                Receive(sockStream);
            });
        }
    }
}
