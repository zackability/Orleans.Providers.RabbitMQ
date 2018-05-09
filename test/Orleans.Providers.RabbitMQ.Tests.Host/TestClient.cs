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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Orleans.Providers.RabbitMQ.Tests.Host
{
    public static class TestClient
    {
        private static string rabbitmqName = "Default";
        private static IClusterClient clusterClient;
        private static IList<StreamSubscriptionHandle<int>> subscriptionHandle;

        public async static Task StartClient()
        {
            try
            {
                var configRoot = TestSilo.GetConfiguration();
                var builder = new ClientBuilder()
                    .WithClusterConfig(configRoot)
                    .WithParts()
                    .WithRabbitMQ(configRoot)
                    .ConfigureLogging(logging =>
                    {
                        logging.AddConfiguration(configRoot.GetSection("Logging")).AddConsole();
                        //logging.AddFile("./debug.txt");
                    });

                clusterClient = builder.Build();
                await clusterClient.Connect();

                subscriptionHandle = await SubscribeStream(clusterClient);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                Console.WriteLine("Failure Starting client.");
                throw exc;
            }
        }

        public static async Task<int> CloseClient()
        {
            try
            {
                await Task.WhenAll(subscriptionHandle.Select(h => h.UnsubscribeAsync()));
                await clusterClient.Close();
                return 0;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc);
                Console.WriteLine("Failure close client.");
                return 1;
            }
        }

        private static IClientBuilder WithClusterConfig(this IClientBuilder builder, IConfiguration configRoot)
        {
            return builder
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(configRoot.GetSection("ClusterOptions"));
        }

        private static IClientBuilder WithParts(this IClientBuilder builder)
        {
            return builder
                .ConfigureApplicationParts(parts => parts
                    .AddFromAppDomain()
                    .WithReferences());
        }

        private static IClientBuilder WithRabbitMQ(this IClientBuilder builder, IConfiguration configRoot)
        {
            return builder
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
                .ConfigureServices(s => s.Configure<RabbitMQStreamProviderOptions>(rabbitmqName, configRoot.GetSection(RabbitMQStreamProviderOptions.SECTION_NAME)));
        }

        private static async Task<IList<StreamSubscriptionHandle<int>>> SubscribeStream(IClusterClient client)
        {
            var streamProvider = client.GetStreamProvider(rabbitmqName);
            var stream = streamProvider.GetStream<int>(Guid.Empty, "TestNamespace");
            var subscriptionHandle = await stream.SubscribeAsync(
                async (data, token) =>
                {
                    await Task.Run(() => Console.WriteLine($"client<<<:{data}"));
                });

            var shs = await stream.GetAllSubscriptionHandles();
            // foreach (var sh in shs)
            // {
            //     sh.ResumeAsync(async (data, token) => await Task.Run(() => Console.WriteLine($"stream:{data}")));
            // }
            return shs;
        }
    }
}
