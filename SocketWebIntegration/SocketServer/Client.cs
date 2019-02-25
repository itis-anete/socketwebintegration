using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer
{
    public class Client
    {
        private const string EndOfLine = "<EOF>";
        private static readonly ManualResetEvent connectDone;
        private static readonly ManualResetEvent sendDone;
        private static readonly ManualResetEvent receiveDone;

        private static string response;

        static Client()
        {
            connectDone = new ManualResetEvent(false);
            sendDone = new ManualResetEvent(false);
            receiveDone = new ManualResetEvent(false);

            response = string.Empty;
        }

        public static void Start(string ipString, int port,string msg)
        {
            Task.Run(() =>
            {
                try
                {
                    var ipAddress = IPAddress.Parse(ipString);
                    var remoteEndPoint = new IPEndPoint(ipAddress, port);

                    var client = new Socket(ipAddress.AddressFamily,
                        SocketType.Stream, ProtocolType.Tcp);

                    client.BeginConnect(remoteEndPoint, new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();

                    Send(client, msg + EndOfLine);
                    sendDone.WaitOne();

                    Receive(client);
                    receiveDone.WaitOne();

                    Console.WriteLine($"Response received: {response}");

                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{e.ToString()} : {e.Message}");
                }
            });
        }
        
        private static void ConnectCallback(IAsyncResult asyncResult)
        {
            try
            {
                var client = asyncResult.AsyncState as Socket;
                client.EndConnect(asyncResult);

                Console.WriteLine($"Socket connected to: {client.RemoteEndPoint.ToString()}");
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()} : {e.Message}");
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                var state = new StateObject(client);
                client.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()} : {e.Message}");
            }
        }

        private static void ReceiveCallback(IAsyncResult asyncResult)
        {
            try
            {
                var state = asyncResult.AsyncState as StateObject;
                var client = state.WorkSocket;

                var bytesRead = client.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    var str = Encoding.ASCII.GetString(state.Buffer, 0, bytesRead);
                    state.Builder.Append(str);
                }
                else
                {
                    if (state.Builder.Length > 1)
                    {
                        response = state.Builder.ToString();
                    }
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()} : {e.Message}");
            }
        }

        private static void Send(Socket client, string data)
        {
            var byteData = Encoding.ASCII.GetBytes(data);

            client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult asyncResult)
        {
            try
            {
                var client = asyncResult.AsyncState as Socket;

                var bytesSent = client.EndSend(asyncResult);
                Console.WriteLine($"Sent {bytesSent} bytes to server.");

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.ToString()} : {e.Message}");
            }
        }
    }
}
