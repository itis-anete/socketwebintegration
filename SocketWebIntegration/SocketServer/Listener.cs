using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer
{
    public class Listener
    {
        public static List<string> messages = new List<string>();

        public static readonly ManualResetEvent allDone;

        static Listener()
        {
            allDone = new ManualResetEvent(false);
        }

        public static void Start(string ipString, int port)
        {
            Task.Run(() =>
            {
                var ipAddress = IPAddress.Parse(ipString);
                var localEndPoint = new IPEndPoint(ipAddress, port);

                var listener = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    listener.Bind(localEndPoint);
                    listener.Listen(128);

                    while (true)
                    {
                        allDone.Reset();
                        Console.WriteLine("Waiting for a connection...");
                        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                        allDone.WaitOne();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.ToString()} : {e.Message}");
                }
            });
        }
        
        public static void AcceptCallback(IAsyncResult asyncResult)
        {
            allDone.Set();
            var listener = asyncResult.AsyncState as Socket;
            var handler = listener.EndAccept(asyncResult);

            var state = new StateObject(handler);
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult asyncResult)
        {
            var state = asyncResult.AsyncState as StateObject;
            var handler = state.WorkSocket;

            var bytesRead = handler.EndReceive(asyncResult);

            if (bytesRead > 0)
            {
                var str = Encoding.ASCII.GetString(state.Buffer, 0, bytesRead);
                state.Builder.Append(str);
                var content = state.Builder.ToString();

                if (content.IndexOf("<EOF>") > -1)
                {
                    messages.Add(content.Replace("<EOF>", string.Empty));

                    Console.WriteLine(
                        $"Read {content.Length} bytes from buffer.\n" +
                        $"Data : {content}");

                    Send(handler, content);
                }
                else
                {
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private static void Send(Socket handler, string data)
        {
            var byteData = Encoding.ASCII.GetBytes(data);

            handler.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallBack), handler);
        }

        private static void SendCallBack(IAsyncResult asyncResult)
        {
            try
            {
                var handler = asyncResult.AsyncState as Socket;

                int bytesSent = handler.EndSend(asyncResult);
                Console.WriteLine($"Sent {bytesSent} bytes to client.");

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()} : {e.Message}");
            }
        }
    }
}
