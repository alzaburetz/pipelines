using System;
using System.Collections.Generic;
using System.Text;

using PipelinesServer.Abstractions;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace PipelinesServer.Server
{
    public class TcpServer : IDisposable, ITcpServer
    {
        public TcpServer(
            string address,
            int port)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new ArgumentException("You cannot pass empty address into constructor", nameof(address));
            }

            if (port <= 0)
            {
                throw new ArgumentException("Port should be more than zero", nameof(port));
            }

            _listener = new TcpListener(IPAddress.Parse(address), port);
            _address = address;
            _port = port;
            _isStarted = false;
        }
        public void Dispose() => Stop();

        public async void Listen(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }

            if (_isStarted || _listener is null)
            {
                return;
            }

            while (!token.IsCancellationRequested)
            {
                Console.WriteLine("Waiting for a client connection...");

                var client = await _listener.AcceptTcpClientAsync();

                Console.WriteLine("Connection established");
                OnClientConnected(client);
                Console.WriteLine("Data passed, closing connection.");
                client.Close();
            }
        }

        public void Start(CancellationToken token)
        {
            if (_isStarted)
            {
                Console.WriteLine("Server has already started!");
                return;
            }

            _listener.Start();
            Listen(token);
            Console.WriteLine($"Server started on {_address}:{_port}.");
            _isStarted = true;
        }

        private void OnClientConnected(TcpClient client)
        {
            var args = new TcpClientEventArgs(client);
            var clientConnectedEvent = ClientConnected;
            clientConnectedEvent?.Invoke(this, args);
        }

        public void Stop()
        {
            if (_isStarted)
            {
                _listener.Stop();
                _isStarted = false;
            }
        }
        public event EventHandler<TcpClientEventArgs> ClientConnected;

        private readonly TcpListener _listener;
        private bool _isStarted;
        private readonly string _address;
        private readonly int _port;
    }
}
