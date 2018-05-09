using Microsoft.Extensions.DependencyInjection;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Streams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.Providers.RabbitMQ.Streams
{
    public class SiloRabbitMQStreamConfigurator<TDataAdapter> : SiloPersistentStreamConfigurator
    where TDataAdapter : IRabbitMQMapper
    {
        public SiloRabbitMQStreamConfigurator(string name, ISiloHostBuilder builder)
            : base(name, builder, RabbitMQAdapterFactory<TDataAdapter>.Create)
        {
            this.siloBuilder
                .ConfigureApplicationParts(parts => parts.AddFrameworkPart(typeof(RabbitMQAdapterFactory<>).Assembly))
                .ConfigureServices(services =>
                {
                    services.ConfigureNamedOptionForLogging<RabbitMQStreamProviderOptions>(name)
                            .AddTransient<IConfigurationValidator>(sp => new RabbitMQStreamProviderOptionsValidator(sp.GetOptionsByName<RabbitMQStreamProviderOptions>(name), name))
                        .ConfigureNamedOptionForLogging<SimpleQueueCacheOptions>(name)
                        .AddTransient<IConfigurationValidator>(sp => new SimpleQueueCacheOptionsValidator(sp.GetOptionsByName<SimpleQueueCacheOptions>(name), name))
                        .ConfigureNamedOptionForLogging<HashRingStreamQueueMapperOptions>(name);
                });
        }

        public SiloRabbitMQStreamConfigurator<TDataAdapter> ConfigureRabbitMQ(Action<OptionsBuilder<RabbitMQStreamProviderOptions>> configureOptions)
        {
            this.Configure<RabbitMQStreamProviderOptions>(configureOptions);
            return this;
        }
        public SiloRabbitMQStreamConfigurator<TDataAdapter> ConfigureCache(int cacheSize = SimpleQueueCacheOptions.DEFAULT_CACHE_SIZE)
        {
            this.Configure<SimpleQueueCacheOptions>(ob => ob.Configure(options => options.CacheSize = cacheSize));
            return this;
        }

        public SiloRabbitMQStreamConfigurator<TDataAdapter> ConfigurePartitioning(int numOfPartition = HashRingStreamQueueMapperOptions.DEFAULT_NUM_QUEUES)
        {
            this.Configure<HashRingStreamQueueMapperOptions>(ob => ob.Configure(options => options.TotalQueueCount = numOfPartition));
            return this;
        }
    }
}
