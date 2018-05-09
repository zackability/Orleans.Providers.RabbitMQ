using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Configuration.Overrides;
using Orleans.Providers.Streams.Common;
using Orleans.Serialization;
using Orleans.Streams;
using System;
using System.Threading.Tasks;

namespace Orleans.Providers.RabbitMQ.Streams
{
    public class RabbitMQAdapterFactory<TMapper> : IQueueAdapterFactory where TMapper : IRabbitMQMapper
    {
        private readonly string providerName;
        private readonly RabbitMQStreamProviderOptions options;
        private readonly ClusterOptions clusterOptions;
        private readonly IRabbitMQMapper mapper;
        private readonly ILoggerFactory loggerFactory;
        private HashRingBasedStreamQueueMapper streamQueueMapper;
        private IQueueAdapterCache adapterCache;

        protected Func<QueueId, Task<IStreamFailureHandler>> StreamFailureHandlerFactory { private get; set; }

        public RabbitMQAdapterFactory(
            string name,
            RabbitMQStreamProviderOptions options,
            HashRingStreamQueueMapperOptions queueMapperOptions,
            SimpleQueueCacheOptions cacheOptions,
            IServiceProvider serviceProvider,
            IOptions<ClusterOptions> clusterOptions,
            SerializationManager serializationManager,
            ILoggerFactory loggerFactory)
        {
            providerName = name;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.clusterOptions = clusterOptions.Value;
            //this.SerializationManager = serializationManager ?? throw new ArgumentNullException(nameof(serializationManager));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            streamQueueMapper = new HashRingBasedStreamQueueMapper(queueMapperOptions, providerName);
            adapterCache = new SimpleQueueAdapterCache(cacheOptions, providerName, this.loggerFactory);
            mapper= ActivatorUtilities.GetServiceOrCreateInstance<TMapper>(serviceProvider);
        }

        public void Init()
        {
            if (StreamFailureHandlerFactory == null)
            {
                StreamFailureHandlerFactory =
                    qid => Task.FromResult<IStreamFailureHandler>(new NoOpStreamDeliveryFailureHandler(false));
            }
        }

        public Task<IQueueAdapter> CreateAdapter()
        {
            IQueueAdapter adapter = new RabbitMQAdapter(options, loggerFactory, providerName, streamQueueMapper, mapper);
            return Task.FromResult(adapter);
        }

        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId)
        {
            return StreamFailureHandlerFactory(queueId);
        }

        public IQueueAdapterCache GetQueueAdapterCache()
        {
            return adapterCache;
        }

        public IStreamQueueMapper GetStreamQueueMapper()
        {
            return streamQueueMapper;
        }

        internal static IQueueAdapterFactory Create(IServiceProvider services, string name)
        {
            var azureQueueOptions = services.GetOptionsByName<RabbitMQStreamProviderOptions>(name);
            var queueMapperOptions = services.GetOptionsByName<HashRingStreamQueueMapperOptions>(name);
            var cacheOptions = services.GetOptionsByName<SimpleQueueCacheOptions>(name);
            IOptions<ClusterOptions> clusterOptions = services.GetProviderClusterOptions(name);
            var factory = ActivatorUtilities.CreateInstance<RabbitMQAdapterFactory<TMapper>>(services, name, azureQueueOptions, queueMapperOptions, cacheOptions, clusterOptions);
            factory.Init();
            return factory;
        }
    }
}
