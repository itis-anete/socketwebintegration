using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SocketServer;

namespace WebHost
{
    public class Program
    {
        public const string ip = "10.17.0.121";
        public const int port = 80;

        public static void Main(string[] args)
        {
            Listener.Start(ip, port);
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            Microsoft.AspNetCore.WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
