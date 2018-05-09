using Microsoft.Extensions.Logging;
using Orleans.Configuration;
using Orleans.Providers.Streams.Common;
using Orleans.Serialization;

namespace Orleans.Providers.RabbitMQ.Streams
{
    public abstract class RabbitMQBaseStreamProvider<TMapper> : PersistentStreamProvider where TMapper : IRabbitMQMapper
    {
        public RabbitMQBaseStreamProvider(string name, StreamPubSubOptions pubsubOptions, StreamLifecycleOptions lifeCycleOptions, IProviderRuntime runtime, SerializationManager serializationManager, ILogger<PersistentStreamProvider> logger) : base(name, pubsubOptions, lifeCycleOptions, runtime, serializationManager, logger)
        {
        }
    }
}
