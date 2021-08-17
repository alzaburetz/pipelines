using System;
using System.Net.Sockets;

namespace PipelinesServer.Server
{
    public class TcpClientEventArgs
    {
        public TcpClient Client { get; }

        public TcpClientEventArgs(TcpClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }
    }
}
