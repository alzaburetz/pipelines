using Microsoft.Extensions.Configuration;
using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tcp.Client
{
    static class Program
    {
        static void Main()
        {
            _configuration = ReadConfig();
            var host = _configuration.GetSection("tcp:address").Value;
            var port = _configuration.GetSection("tcp:port").Value;

            var pipe = new Pipe();
            Task.WaitAll(
                TcpReaderAsync(pipe.Writer, host, int.Parse(port)),
                FileWriteAsync(pipe.Reader)
            );
            Console.ReadLine();
        }


        private static async Task TcpReaderAsync(PipeWriter writer, string host, int port)
        {
            _minimumBufferSize = int.Parse(_configuration.GetSection("bufferSize").Value);

            using var client = new TcpClient();
            Console.WriteLine("Connecting to server.");
            await client.ConnectAsync(host, port);
            Console.WriteLine("Connected.");

            using var stream = client.GetStream();
            while (true)
            {
                try
                {
                    var memory = writer.GetMemory(_minimumBufferSize);
                    var read = await stream.ReadAsync(memory);

                    if (read == 0)
                    {
                        break;
                    }

                    writer.Advance(read);

                    Console.WriteLine($"Read from stream {read} bytes");

                }
                catch
                {
                    break;
                }

                var flushResult = await writer.FlushAsync();
                if (!flushResult.IsCompleted)
                {
                    break;
                }
            }

            Console.WriteLine("Message was read");
            
            writer.Complete();
        }

        static async Task FileWriteAsync(PipeReader reader)
        {
            var file = _configuration.GetSection("filePath").Value;

            using var fileStream = new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            while (true)
            {
                var readResult = await reader.ReadAsync();

                var buffer = readResult.Buffer;

                if (buffer.IsEmpty && readResult.IsCompleted)
                {
                    break;
                }

                foreach (ReadOnlyMemory<byte> segment in buffer)
                {
                    await fileStream.WriteAsync(segment);
                }

                reader.AdvanceTo(buffer.End);

                Console.WriteLine($"Append to file {buffer.Length} bytes");
            }

            reader.Complete();
        }

        private static IConfiguration ReadConfig() =>
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

        private static IConfiguration _configuration;
        private static int _minimumBufferSize;
    }
}