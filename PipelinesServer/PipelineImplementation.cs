using System;
using System.Collections.Generic;
using System.IO;

using PipelinesServer.Server;
using PipelinesServer.Abstractions;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;

namespace PipelinesServer
{
    public class PipelineImplementation
    {
        public PipelineImplementation(TcpServer server, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _server = server;
            _path = path;
        }

        public void Start(CancellationToken token)
        {
            _server.Start(token);
            _server.ClientConnected += Listen;
            Console.ReadKey();
            Console.WriteLine("Server stopped");
        }

        private async void Listen(object sender, TcpClientEventArgs args)
        {
            var pipe = new Pipe(new PipeOptions());
            await FillPipeline(pipe.Writer)
                .ContinueWith(_ => SendData(pipe.Reader, args.Client).ConfigureAwait(false));
        }
        private async Task FillPipeline(PipeWriter writer)
        {
            using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 10);
            while (true)
            {
                try
                {
                    var memory = writer.GetMemory(512);

                    var bytesRead = await stream.ReadAsync(memory);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    writer.Advance(bytesRead);

                    var flushResult = await writer.FlushAsync();

                    if (flushResult.IsCanceled || !flushResult.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception)
                {
                    await writer.CompleteAsync();
                    break;
                }
            }
        }

        private async Task SendData(PipeReader reader, TcpClient client)
        {
            var tcpStream = client.GetStream();

            while (true)
            {
                var readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;

                if (readResult.IsCompleted || buffer.IsEmpty)
                {
                    break;
                }

                foreach (var segment in buffer)
                {
                    await tcpStream.WriteAsync(segment);
                }

                reader.AdvanceTo(buffer.End);

                if (readResult.IsCompleted)
                {
                    break;
                }
            }

            await tcpStream.FlushAsync();
            reader.Complete();
            tcpStream.Dispose();
        }

        private readonly string _path;
        private readonly TcpServer _server;
    }
}
