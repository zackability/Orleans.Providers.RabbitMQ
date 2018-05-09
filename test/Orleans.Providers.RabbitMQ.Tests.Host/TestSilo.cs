using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.RabbitMQ.Hosting;
using Orleans.Providers.RabbitMQ.Streams;
using Orleans.Providers.RabbitMQ.Tests.Host.Grains;
using Orleans.Providers.RabbitMQ.Tests.Host.StartupTasks;
using Orleans.Streams;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Orleans.Providers.RabbitMQ.Tests.Host
{
    public static class TestSilo
    {
        private static IPAddress _siloIP;
        private static ISiloHost _siloHost;
        private static string _hostName;
        private const string ADO_INVARIANT = "System.Data.SqlClient";

        public static async Task<int> StopSilo()
        {
            try
            {
                await _siloHost.StopAsync();
                return 0;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                Console.WriteLine("Failure stopping silo.");
                return 1;
            }
        }

        public static async Task StartSilo()
        {
            try
            {
                _hostName = Dns.GetHostName();
                var ips = await Dns.GetHostAddressesAsync(_hostName);
                _siloIP = ips.FirstOrDefault();

                var configRoot = GetConfiguration();
                var builder = new SiloHostBuilder()
                    .WithClusterConfig(configRoot)
                    .WithParts()
                    .WithMembership(configRoot)
                    .ConfigureLogging(logging =>
                    {
                        logging.AddConfiguration(configRoot.GetSection("Logging")).AddConsole();
                        //logging.AddFile("./debug.txt");
                    });

                _siloHost = builder.Build();
                await _siloHost.StartAsync();
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                Console.WriteLine("Failure Starting silo.");
                throw exc;
            }
        }

        private static ISiloHostBuilder WithMembership(this ISiloHostBuilder builder, IConfigurationRoot configRoot)
        {
            return builder;
            //return builder.UseDevelopmentMembership(opt => opt.PrimarySiloEndpoint = new IPEndPoint(_siloIP, 10001));
        }

        private static ISiloHostBuilder WithClusterConfig(this ISiloHostBuilder builder, IConfigurationRoot configRoot)
        {
            string rabbitmqName = "Default";

            builder.AddStartupTask<RabbitMQTestStartupTask>()
                .UseLocalhostClustering()
                .AddMemoryGrainStorage("PubSubStore")
                .AddRabbitMQStreams<RabbitMQDefaultMapper>(name: rabbitmqName, configure: null)
                //.AddRabbitMQStreams<RabbitMQDefaultMapper>(rabbitmqName, ob =>
                //    ob.Configure(op =>
                //     {
                //         op.Mode = StreamProviderDirection.ReadWrite;
                //         op.NumberOfQueues = 8;
                //         op.HostName = "localhost";
                //         op.Port = 5671;
                //         op.VirtualHost = "/";
                //         op.Exchange = "exchange";
                //         op.ExchangeType = "direct";
                //         op.ExchangeDurable = false;
                //         op.AutoDelete = true;
                //         op.Queue = "queue";
                //         op.QueueDurable = false;
                //         op.Namespace = "TestNamespace";
                //         op.RoutingKey = "#";
                //         op.Username = "guest";
                //         op.Password = "guest";
                //     }))
                .Configure<ClusterOptions>(configRoot.GetSection("ClusterOptions"))
                .ConfigureServices(s => s.Configure<RabbitMQStreamProviderOptions>(rabbitmqName, configRoot.GetSection(RabbitMQStreamProviderOptions.SECTION_NAME)));

            return builder;
        }

        private static ISiloHostBuilder WithParts(this ISiloHostBuilder builder)
        {
            return builder
                .ConfigureApplicationParts(mgr => mgr.AddApplicationPart(typeof(ImplicitGrain).Assembly));
        }

        private static IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
#if DEBUG
                .AddJsonFile($"appsettings.Development.json", optional: true)
#endif
                ;

            return builder.Build();
        }
    }
}