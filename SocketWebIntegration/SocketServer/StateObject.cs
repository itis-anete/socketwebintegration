using System.Net.Sockets;
using System.Text;

namespace SocketServer
{
    public class StateObject
    {
        public const int BufferSize = 1024;

        public StateObject(Socket workSocket)
        {
            WorkSocket = workSocket;
            Buffer = new byte[BufferSize];
            Builder = new StringBuilder();
        }

        public Socket WorkSocket { get; }

        public byte[] Buffer { get; }

        public StringBuilder Builder { get; }
    }
}
