using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Configuration;
using PipelinesServer.Server;

namespace PipelinesServer
{
    class Program
    {
        static void Main()
        {
            _configuration = ReadConfig();
            var address = _configuration.GetSection("tcp:address").Value;
            var port = _configuration.GetSection("tcp:port").Value;
            var file = _configuration.GetSection("filePath").Value;
            var server = new TcpServer(address, int.Parse(port));
            var pipeline = new PipelineImplementation(server, file);

            var cancellation = new CancellationTokenRegistration();
            try
            {
                pipeline.Start(cancellation.Token);
            }
            catch (Exception)
            {
                Console.WriteLine("Error occured");
                Console.ReadKey();
            }
        }

        private static IConfiguration ReadConfig() =>
            new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

        private static IConfiguration _configuration;
    }
}
